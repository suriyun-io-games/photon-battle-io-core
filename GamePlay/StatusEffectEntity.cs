using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StatusEffectEntity : MonoBehaviour
{
    public int GetHashId()
    {
        return hashId;
    }

    [SerializeField]
    private int hashId;

    [Range(0f, 1f)]
    public float applyRate = 1f;
    public int recoveryHpPerSeconds = 0;
    public CharacterStats addStats;
    public float lifeTime;
    private CharacterEntity receiverCharacterEntity;
    private CharacterEntity applierCharacterEntity;

    private void Start()
    {
        if (lifeTime > 0)
            Destroy(gameObject, lifeTime);
    }

    private void OnDestroy()
    {
        if (receiverCharacterEntity)
            receiverCharacterEntity.RemoveAppliedStatusEffect(GetHashId());
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (hashId != name.MakeHashId())
        {
            hashId = name.MakeHashId();
            EditorUtility.SetDirty(this);
        }
#endif
    }

    public void Recovery()
    {
        if (receiverCharacterEntity && receiverCharacterEntity.Hp > 0)
        {
            if (recoveryHpPerSeconds > 0 || GameplayManager.Singleton.CanReceiveDamage(receiverCharacterEntity, applierCharacterEntity))
                receiverCharacterEntity.Hp += recoveryHpPerSeconds;
            if (receiverCharacterEntity.Hp <= 0 && !BaseNetworkGameManager.Singleton.IsMatchEnded)
            {
                if (applierCharacterEntity)
                    applierCharacterEntity.KilledTarget(receiverCharacterEntity);
                Destroy(gameObject);
            }
        }
    }

    public void Applied(CharacterEntity receiverCharacterEntity, CharacterEntity applierCharacterEntity)
    {
        this.receiverCharacterEntity = receiverCharacterEntity;
        this.applierCharacterEntity = applierCharacterEntity;
        InvokeRepeating("Recovery", 0, 1);
    }
}
