using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class DamageEntity : MonoBehaviour
{
    public string GetId()
    {
        return name;
    }

    public int GetHashId()
    {
        return GetId().MakeHashId();
    }

    public EffectEntity spawnEffectPrefab;
    public EffectEntity explodeEffectPrefab;
    public EffectEntity hitEffectPrefab;
    [Tooltip("This status will be applied to character whom hitted by this damage entity")]
    public StatusEffectEntity statusEffectPrefab;
    public AudioClip[] hitFx;
    public float radius;
    public float explosionForceRadius;
    public float explosionForce;
    public float lifeTime;
    public float spawnForwardOffset;
    public float speed;
    public bool relateToAttacker;
    private bool isDead;
    private WeaponData weaponData;
    private SkillData skillData;
    private bool isLeftHandWeapon;
    private CharacterEntity attacker;
    private float addRotationX;
    private float addRotationY;
    private int spread;
    private float? colliderExtents;
    private HashSet<int> appliedIDs = new HashSet<int>();

    public Transform CacheTransform { get; private set; }
    public Rigidbody CacheRigidbody { get; private set; }
    public Collider CacheCollider { get; private set; }

    private void Awake()
    {
        gameObject.layer = GenericUtils.IgnoreRaycastLayer;
        CacheTransform = transform;
        CacheRigidbody = GetComponent<Rigidbody>();
        CacheCollider = GetComponent<Collider>();
        CacheCollider.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// Init Attacker, this function must be call at server to init attacker
    /// </summary>
    public void InitAttackData(WeaponData weaponData, SkillData skillData, bool isLeftHandWeapon, CharacterEntity attacker, float addRotationX, float addRotationY, int spread)
    {
        this.weaponData = weaponData;
        this.skillData = skillData;
        this.isLeftHandWeapon = isLeftHandWeapon;
        this.attacker = attacker;
        this.addRotationX = addRotationX;
        this.addRotationY = addRotationY;
        this.spread = spread;
        InitTransform();
    }

    private void InitTransform()
    {
        if (attacker == null)
            return;

        if (relateToAttacker)
        {
            Transform damageLaunchTransform;
            attacker.GetDamageLaunchTransform(isLeftHandWeapon, out damageLaunchTransform);
            CacheTransform.SetParent(damageLaunchTransform);
            var baseAngles = attacker.CacheTransform.eulerAngles;
            CacheTransform.rotation = Quaternion.Euler(baseAngles.x + addRotationX, baseAngles.y + addRotationY, baseAngles.z);
        }
    }

    private void FixedUpdate()
    {
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        if (attacker != null)
        {
            if (relateToAttacker)
            {
                if (CacheTransform.parent == null)
                {
                    Transform damageLaunchTransform;
                    attacker.GetDamageLaunchTransform(isLeftHandWeapon, out damageLaunchTransform);
                    CacheTransform.SetParent(damageLaunchTransform);
                }
                var baseAngles = attacker.CacheTransform.eulerAngles;
                CacheTransform.rotation = Quaternion.Euler(baseAngles.x + addRotationX, baseAngles.y + addRotationY, baseAngles.z);
            }
        }
        CacheRigidbody.velocity = GetForwardVelocity();
    }

    private void OnDestroy()
    {
        if (!isDead)
        {
            Explode(null);
            EffectEntity.PlayEffect(explodeEffectPrefab, CacheTransform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == GenericUtils.IgnoreRaycastLayer)
            return;

        var otherCharacter = other.GetComponent<CharacterEntity>();
        // Damage will not hit attacker, so avoid it
        if (otherCharacter != null && otherCharacter == attacker)
            return;

        var hitSomeAliveCharacter = false;
        if (otherCharacter != null &&
            otherCharacter.Hp > 0 &&
            !otherCharacter.IsInvincible &&
            GameplayManager.Singleton.CanReceiveDamage(otherCharacter, attacker))
        {
            if (!otherCharacter.IsHidding)
                EffectEntity.PlayEffect(hitEffectPrefab, otherCharacter.effectTransform);
            ApplyDamage(otherCharacter);
            hitSomeAliveCharacter = true;
        }

        if (Explode(otherCharacter))
        {
            hitSomeAliveCharacter = true;
        }

        // If hit character (not the wall) but not hit alive character, don't destroy, let's find another target.
        if (otherCharacter != null && !hitSomeAliveCharacter)
            return;

        if (!isDead && hitSomeAliveCharacter)
        {
            // Play hit effect
            if (hitFx != null && hitFx.Length > 0 && AudioManager.Singleton != null)
                AudioSource.PlayClipAtPoint(hitFx[Random.Range(0, hitFx.Length - 1)], CacheTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);
        }

        Destroy(gameObject);
        isDead = true;
    }

    private bool Explode(CharacterEntity otherCharacter)
    {
        var hitSomeAliveCharacter = false;
        Collider[] colliders = Physics.OverlapSphere(CacheTransform.position, radius, 1 << GameInstance.Singleton.characterLayer);
        CharacterEntity hitCharacter;
        for (int i = 0; i < colliders.Length; i++)
        {
            hitCharacter = colliders[i].GetComponent<CharacterEntity>();
            // If not character or character is attacker, skip it.
            if (hitCharacter == null ||
                hitCharacter == otherCharacter ||
                hitCharacter == attacker ||
                hitCharacter.Hp <= 0 ||
                hitCharacter.IsInvincible ||
                !GameplayManager.Singleton.CanReceiveDamage(hitCharacter, attacker))
                continue;
            if (!hitCharacter.IsHidding)
                EffectEntity.PlayEffect(hitEffectPrefab, hitCharacter.effectTransform);
            ApplyDamage(hitCharacter);
            hitSomeAliveCharacter = true;
        }
        return hitSomeAliveCharacter;
    }

    private void ApplyDamage(CharacterEntity target)
    {
        if (appliedIDs.Contains(target.photonView.ViewID))
            return;
        // Damage receiving calculation on server only
        if (attacker != null && attacker.photonView.IsMine)
        {
            appliedIDs.Add(target.photonView.ViewID);
            var statusEffectId = statusEffectPrefab != null ? statusEffectPrefab.GetHashId() : 0;
            if (PhotonNetwork.IsMasterClient)
            {
                // Master client can apply damage immediately
                if (skillData != null)
                    attacker.ApplySkillDamage(target, skillData, spread, statusEffectId);
                else if (weaponData != null)
                    attacker.ApplyWeaponDamage(target, weaponData, spread, statusEffectId);
            }
            else
            {
                // Client tells master client to apply damage
                if (skillData != null)
                    attacker.CmdApplySkillDamage(target.photonView.ViewID, skillData.GetHashId(), spread, statusEffectId);
                else if (weaponData != null)
                    attacker.CmdApplyWeaponDamage(target.photonView.ViewID, weaponData.GetHashId(), spread, statusEffectId);
            }
        }
        target.CacheCharacterMovement.AddExplosionForce(CacheTransform.position, explosionForce, explosionForceRadius);
    }

    private float GetColliderExtents()
    {
        if (colliderExtents.HasValue)
            return colliderExtents.Value;
        var tempObject = Instantiate(gameObject);
        var tempCollider = tempObject.GetComponent<Collider>();
        colliderExtents = Mathf.Min(tempCollider.bounds.extents.x, tempCollider.bounds.extents.z);
        Destroy(tempObject);
        return colliderExtents.Value;
    }

    public float GetAttackRange()
    {
        // s = v * t
        return (speed * lifeTime * GameplayManager.REAL_MOVE_SPEED_RATE) + GetColliderExtents();
    }

    public Vector3 GetForwardVelocity()
    {
        return CacheTransform.forward * speed * GameplayManager.REAL_MOVE_SPEED_RATE;
    }

    public static DamageEntity InstantiateNewEntityByWeapon(
        WeaponData weaponData,
        byte actionId,
        Vector3 targetPosition,
        CharacterEntity attacker,
        float addRotationX,
        float addRotationY,
        int spread)
    {
        if (weaponData == null || weaponData.damagePrefab == null)
            return null;

        if (attacker == null)
            return null;

        var damagePrefab = weaponData.damagePrefab;
        var isLeftHandWeapon = false;
        AttackAnimation attackAnimation;
        if (weaponData.AttackAnimations.TryGetValue(actionId, out attackAnimation))
        {
            if (attackAnimation.damagePrefab != null)
                damagePrefab = attackAnimation.damagePrefab;
            isLeftHandWeapon = attackAnimation.isAnimationForLeftHandWeapon;
        }
        return InstantiateNewEntity(weaponData, null, damagePrefab, isLeftHandWeapon, targetPosition, attacker, addRotationX, addRotationY, spread);
    }

    public static DamageEntity InstantiateNewEntityBySkill(
        SkillData skillData,
        Vector3 targetPosition,
        CharacterEntity attacker,
        float addRotationX,
        float addRotationY,
        int spread)
    {
        if (skillData == null || skillData.damagePrefab == null)
            return null;

        if (attacker == null)
            return null;

        var damagePrefab = skillData.damagePrefab;
        var isLeftHandWeapon = false;
        return InstantiateNewEntity(null, skillData, damagePrefab, isLeftHandWeapon, targetPosition, attacker, addRotationX, addRotationY, spread);
    }

    public static DamageEntity InstantiateNewEntity(
        WeaponData weaponData,
        SkillData skillData,
        DamageEntity damagePrefab,
        bool isLeftHandWeapon,
        Vector3 targetPosition,
        CharacterEntity attacker,
        float addRotationX,
        float addRotationY,
        int spread)
    {
        if (weaponData == null && skillData == null)
            return null;
        Transform launchTransform;
        attacker.GetDamageLaunchTransform(isLeftHandWeapon, out launchTransform);
        Vector3 position = launchTransform.position + attacker.CacheTransform.forward * damagePrefab.spawnForwardOffset;
        Vector3 dir = targetPosition - position;
        Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
        rotation = Quaternion.Euler(rotation.eulerAngles + new Vector3(addRotationX, addRotationY));
        DamageEntity result = Instantiate(damagePrefab, position, rotation);
        result.InitAttackData(weaponData, skillData, isLeftHandWeapon, attacker, addRotationX, addRotationY, spread);
        return result;
    }
}
