using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

public class WeaponData : ItemData
{
    public GameObject rightHandObject;
    public GameObject leftHandObject;
    public GameObject shieldObject;
    public List<AttackAnimation> attackAnimations;
    public DamageEntity damagePrefab;
    [Header("SFX")]
    public AudioClip[] attackFx;
    public int weaponAnimId;
    public readonly Dictionary<short, AttackAnimation> AttackAnimations = new Dictionary<short, AttackAnimation>();

    public void Launch(CharacterEntity attacker, byte actionId)
    {
        if (attacker == null || !attacker.photonView.IsMine)
            return;

        var gameNetworkManager = GameNetworkManager.Singleton;
        var gameplayManager = GameplayManager.Singleton;
        var spread = attacker.TotalSpreadDamages;
        var damage = (float)attacker.TotalAttack;
        damage += Random.Range(gameplayManager.minAttackVaryRate, gameplayManager.maxAttackVaryRate) * damage;
        if (gameplayManager.divideSpreadedDamageAmount)
            damage /= spread;

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

            var damageEntity = DamageEntity.InstantiateNewEntityByWeapon(GetHashId(), actionId, direction, attacker.photonView.ViewID, addRotationX, addRotationY);
            if (damageEntity)
            {
                damageEntity.weaponDamage = Mathf.CeilToInt(damage);
                damageEntity.hitEffectType = CharacterEntity.RPC_EFFECT_DAMAGE_HIT;
                damageEntity.relateDataId = GetHashId();
                damageEntity.actionId = actionId;
            }

            gameNetworkManager.photonView.RPC("RpcCharacterAttack",
                RpcTarget.Others,
                GetHashId(),
                actionId,
                (short)(direction.x * 100f),
                (short)(direction.y * 100f),
                (short)(direction.z * 100f),
                attacker.photonView.ViewID,
                addRotationX,
                addRotationY,
                Mathf.CeilToInt(damage));

            addRotationY += addingRotationY;
        }

        attacker.photonView.RPC("RpcEffect", RpcTarget.All, attacker.photonView.ViewID, CharacterEntity.RPC_EFFECT_DAMAGE_SPAWN, GetHashId(), actionId);
    }

    public void SetupAnimations()
    {
        foreach (var attackAnimation in attackAnimations)
        {
            AttackAnimations[attackAnimation.actionId] = attackAnimation;
        }
    }

    public AttackAnimation GetRandomAttackAnimation()
    {
        var list = AttackAnimations.Values.ToList();
        var randomedIndex = Random.Range(0, list.Count);
        return list[randomedIndex];
    }
}
