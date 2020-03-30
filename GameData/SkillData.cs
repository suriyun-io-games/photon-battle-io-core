using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SkillData : ScriptableObject
{
    public string GetId()
    {
        return name;
    }

    public int GetHashId()
    {
        return GetId().MakeHashId();
    }
    
    [Range(0, 7)]
    public sbyte hotkeyId;
    public Sprite icon;
    public AttackAnimation attackAnimation;
    public DamageEntity damagePrefab;
    [Tooltip("This status will be applied to user when use skill")]
    public StatusEffectEntity statusEffectPrefab;
    [Tooltip("This will increase to weapon damage to calculate skill damage" +
        "Ex. weaponDamage => 10 * this => 1, skill damage = 10 + 1 = 11")]
    public int increaseDamage;
    [Tooltip("This will multiplies to weapon damage then increase to weapon damage to calculate skill damage." +
        "Ex. weaponDamage => 10 * this => 0.1, skill damage = 10 + (10 * 0.1) = 11")]
    public float increaseDamageByRate;
    public int spreadDamages = 0;
    public float coolDown = 3;
    [Header("SFX")]
    public AudioClip[] attackFx;
    public int TotalSpreadDamages { get { return 1 + spreadDamages; } }

    public void Launch(CharacterEntity attacker)
    {
        if (attacker == null || !attacker.photonView.IsMine)
            return;

        attacker.photonView.RPC("RpcEffect", RpcTarget.All, attacker.photonView.ViewID, CharacterEntity.RPC_EFFECT_SKILL_SPAWN, GetHashId(), default(byte));

        if (statusEffectPrefab && GameplayManager.Singleton.CanApplyStatusEffect(attacker, null))
            attacker.photonView.RPC("RpcApplyStatusEffect", RpcTarget.All, statusEffectPrefab.GetHashId());

        if (!damagePrefab)
            return;

        var spread = TotalSpreadDamages;
        var damage = (float)attacker.TotalAttack + increaseDamage + (attacker.TotalAttack * increaseDamageByRate);
        damage += Random.Range(GameplayManager.Singleton.minAttackVaryRate, GameplayManager.Singleton.maxAttackVaryRate) * damage;

        var addRotationX = 0f;
        var addRotationY = 0f;
        var addingRotationY = 360f / spread;

        if (spread <= 16)
        {
            addRotationY = (-(spread - 1) * 15f);
            addingRotationY = 30f;
        }

        for (var i = 0; i < spread; ++i)
        {
            // An transform's rotation, position will be set when set `Attacker`
            // So don't worry about them before damage entity going to spawn
            // Velocity also being set when set `Attacker` too.
            var direction = attacker.CacheTransform.forward;

            var damageEntity = DamageEntity.InstantiateNewEntityBySkill(GetHashId(), direction, attacker.photonView.ViewID, addRotationX, addRotationY);
            if (damageEntity)
            {
                damageEntity.weaponDamage = Mathf.CeilToInt(damage);
                damageEntity.hitEffectType = CharacterEntity.RPC_EFFECT_SKILL_HIT;
                damageEntity.relateDataId = GetHashId();
                damageEntity.actionId = 0;
            }

            GameNetworkManager.Singleton.photonView.RPC("RpcCharacterUseSkill", 
                RpcTarget.Others,
                GetHashId(),
                (short)(direction.x * 100f),
                (short)(direction.y * 100f),
                (short)(direction.z * 100f),
                attacker.photonView.ViewID,
                addRotationX,
                addRotationY,
                Mathf.CeilToInt(damage));

            addRotationY += addingRotationY;
        }
    }
}
