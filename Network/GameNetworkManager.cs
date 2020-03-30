using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class GameNetworkManager : BaseNetworkGameManager
{
    public static new GameNetworkManager Singleton
    {
        get { return SimplePhotonNetworkManager.Singleton as GameNetworkManager; }
    }

    [PunRPC]
    protected void RpcCharacterAttack(
        int weaponId,
        byte actionId,
        short dirX,
        short dirY,
        short dirZ,
        int attackerViewId,
        float addRotationX,
        float addRotationY,
        int damage)
    {
        // Instantiates damage entities on clients only
        var direction = new Vector3((float)dirX * 0.01f, (float)dirY * 0.01f, (float)dirZ * 0.01f);
        var damageEntity = DamageEntity.InstantiateNewEntityByWeapon(weaponId, actionId, direction, attackerViewId, addRotationX, addRotationY);
        if (damageEntity)
        {
            damageEntity.weaponDamage = damage;
            damageEntity.hitEffectType = CharacterEntity.RPC_EFFECT_DAMAGE_HIT;
            damageEntity.relateDataId = weaponId;
            damageEntity.actionId = actionId;
        }
    }

    [PunRPC]
    protected void RpcCharacterUseSkill(
        int skillId,
        short dirX,
        short dirY,
        short dirZ,
        int attackerViewId,
        float addRotationX,
        float addRotationY,
        int damage)
    {
        // Instantiates damage entities on clients only
        var direction = new Vector3((float)dirX * 0.01f, (float)dirY * 0.01f, (float)dirZ * 0.01f);
        var damageEntity = DamageEntity.InstantiateNewEntityBySkill(skillId, direction, attackerViewId, addRotationX, addRotationY);
        if (damageEntity)
        {
            damageEntity.weaponDamage = damage;
            damageEntity.hitEffectType = CharacterEntity.RPC_EFFECT_SKILL_HIT;
            damageEntity.relateDataId = skillId;
            damageEntity.actionId = 0;
        }
    }

    [PunRPC]
    protected override void RpcAddPlayer()
    {
        var position = Vector3.zero;
        var rotation = Quaternion.identity;
        RandomStartPoint(out position, out rotation);

        // Get character prefab
        CharacterEntity characterPrefab = GameInstance.Singleton.characterPrefab;
        if (gameRule != null && gameRule is IONetworkGameRule)
        {
            var ioGameRule = gameRule as IONetworkGameRule;
            if (ioGameRule.overrideCharacterPrefab != null)
                characterPrefab = ioGameRule.overrideCharacterPrefab;
        }
        var characterGo = PhotonNetwork.Instantiate(characterPrefab.name, position, rotation, 0);
        var character = characterGo.GetComponent<CharacterEntity>();
        // Custom Equipments
        var savedCustomEquipments = PlayerSave.GetCustomEquipments();
        var selectCustomEquipments = new List<int>();
        foreach (var savedCustomEquipment in savedCustomEquipments)
        {
            var data = GameInstance.GetAvailableCustomEquipment(savedCustomEquipment.Value);
            if (data != null)
                selectCustomEquipments.Add(data.GetHashId());
        }
        var headData = GameInstance.GetAvailableHead(PlayerSave.GetHead());
        var characterData = GameInstance.GetAvailableCharacter(PlayerSave.GetCharacter());
        var weaponData = GameInstance.GetAvailableWeapon(PlayerSave.GetWeapon());
        character.CmdInit(headData != null ? headData.GetHashId() : 0,
            characterData != null ? characterData.GetHashId() : 0,
            weaponData != null ? weaponData.GetHashId() : 0,
            selectCustomEquipments.ToArray(),
            "");
    }

    protected override void Awake()
    {
        base.Awake();
        PhotonPeer.RegisterType(typeof(CharacterStats), 0, CharacterStats.SerializeMethod, CharacterStats.DeserializeMethod);
    }

    protected override void UpdateScores(NetworkGameScore[] scores)
    {
        var rank = 0;
        foreach (var score in scores)
        {
            ++rank;
            if (BaseNetworkGameCharacter.Local != null && score.viewId == BaseNetworkGameCharacter.Local.photonView.ViewID)
            {
                (BaseNetworkGameCharacter.Local as CharacterEntity).rank = rank;
                break;
            }
        }
        var uiGameplay = FindObjectOfType<UIGameplay>();
        if (uiGameplay != null)
            uiGameplay.UpdateRankings(scores);
    }

    protected override void KillNotify(string killerName, string victimName, string weaponId)
    {
        var uiGameplay = FindObjectOfType<UIGameplay>();
        if (uiGameplay != null)
            uiGameplay.KillNotify(killerName, victimName, weaponId);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        Characters.Clear();
        var characters = FindObjectsOfType<BaseNetworkGameCharacter>();
        foreach (var character in characters)
        {
            if (character is MonsterEntity) continue;
            Characters.Add(character);
        }
    }
}
