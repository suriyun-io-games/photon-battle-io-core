using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSignal : MonoBehaviour
{
    public RectTransform[] signalObjects;
    public float sizeMultiplier = 1f;
    private CharacterEntity characterEntity;

    private void Start()
    {
        characterEntity = GetComponentInParent<CharacterEntity>();
    }

    private void Update()
    {
        if (!characterEntity)
            return;

        var spread = characterEntity.attackingSpreadDamages;
        var eulerAngles = Vector3.zero;
        var addRotationZ = 0f;
        var addingRotationZ = 360f / spread;

        if (spread <= 16)
        {
            addRotationZ = (-(spread - 1) * 15f);
            addingRotationZ = 30f;
        }

        for (int i = 0; i < signalObjects.Length; ++i)
        {
            var signalObject = signalObjects[i];
            if (i >= spread)
            {
                signalObject.gameObject.SetActive(false);
                continue;
            }
            signalObject.gameObject.SetActive((characterEntity.isPlayingAttackAnim || characterEntity.isPlayingUseSkillAnim || characterEntity.attackingDamageEntity) && characterEntity == BaseNetworkGameCharacter.Local);
            if (signalObject.gameObject.activeSelf)
                signalObject.sizeDelta = new Vector2(characterEntity.attackingDamageEntity.radius, characterEntity.attackingDamageEntity.GetAttackRange()) * sizeMultiplier;
            eulerAngles.z = addRotationZ;
            signalObject.localEulerAngles = eulerAngles;
            addRotationZ += addingRotationZ;
        }
    }
}
