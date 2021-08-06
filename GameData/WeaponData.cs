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

    public void Launch(CharacterEntity attacker, Vector3 targetPosition, byte actionId)
    {
        if (!attacker)
            return;

        if (!attacker.IsHidding)
            EffectEntity.PlayEffect(damagePrefab.spawnEffectPrefab, attacker.effectTransform);

        var spread = attacker.TotalSpreadDamages;
        var damage = (float)attacker.TotalAttack;
        damage += Random.Range(GameplayManager.Singleton.minAttackVaryRate, GameplayManager.Singleton.maxAttackVaryRate) * damage;
        if (GameplayManager.Singleton.divideSpreadedDamageAmount)
            damage /= spread;
        if (damage <= 0f)
            damage = GameplayManager.Singleton.baseDamage;

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
            var damageEntity = DamageEntity.InstantiateNewEntityByWeapon(GetHashId(), actionId, targetPosition, attacker.photonView.ViewID, addRotationX, addRotationY);
            if (damageEntity)
            {
                damageEntity.weaponDamage = Mathf.CeilToInt(damage);
                damageEntity.actionId = actionId;
            }
            addRotationY += addingRotationY;
        }
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
