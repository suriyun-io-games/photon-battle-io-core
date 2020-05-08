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
        if (receiverCharacterEntity && GameplayManager.Singleton.CanReceiveDamage(receiverCharacterEntity, applierCharacterEntity))
        {
            receiverCharacterEntity.Hp += recoveryHpPerSeconds;
            if (applierCharacterEntity && receiverCharacterEntity.Hp == 0)
                applierCharacterEntity.KilledTarget(receiverCharacterEntity);
        }
    }

    public void Applied(CharacterEntity receiverCharacterEntity, CharacterEntity applierCharacterEntity)
    {
        this.receiverCharacterEntity = receiverCharacterEntity;
        this.applierCharacterEntity = applierCharacterEntity;
        InvokeRepeating("Recovery", 0, 1);
    }
}
