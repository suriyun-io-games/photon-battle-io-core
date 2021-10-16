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

    public void Launch(CharacterEntity attacker, Vector3 targetPosition)
    {
        if (!attacker)
            return;

        if (attacker.photonView.IsMine && statusEffectPrefab && Random.value <= statusEffectPrefab.applyRate && GameplayManager.Singleton.CanApplyStatusEffect(attacker, null))
            attacker.photonView.AllRPC(attacker.RpcApplyStatusEffect, statusEffectPrefab.GetHashId(), attacker.photonView.ViewID);

        if (!damagePrefab)
            return;

        if (!attacker.IsHidding)
            EffectEntity.PlayEffect(damagePrefab.spawnEffectPrefab, attacker.effectTransform);
        var spread = TotalSpreadDamages;
        var damage = (float)attacker.TotalAttack + increaseDamage + (attacker.TotalAttack * increaseDamageByRate);
        damage += Random.Range(GameplayManager.Singleton.minAttackVaryRate, GameplayManager.Singleton.maxAttackVaryRate) * damage;
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
            var damageEntity = DamageEntity.InstantiateNewEntityBySkill(GetHashId(), targetPosition, attacker.photonView.ViewID, addRotationX, addRotationY);
            if (damageEntity)
            {
                damageEntity.weaponDamage = Mathf.CeilToInt(damage);
                damageEntity.actionId = 0;
            }

            addRotationY += addingRotationY;
        }
    }
}
