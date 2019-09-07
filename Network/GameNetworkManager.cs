using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using UnityEngine;
using Photon.Pun;

public class GameNetworkManager : BaseNetworkGameManager
{
    public static new GameNetworkManager Singleton
    {
        get { return SimplePhotonNetworkManager.Singleton as GameNetworkManager; }
    }

    [PunRPC]
    protected void RpcCharacterAttack(
        int weaponId,
        bool isLeftHandWeapon,
        Vector3 position,
        Vector3 direction,
        int attackerViewId,
        float addRotationX,
        float addRotationY)
    {
        // Instantiates damage entities on clients only
        if (!PhotonNetwork.IsMasterClient)
            DamageEntity.InstantiateNewEntityByWeapon(weaponId, isLeftHandWeapon, position, direction, attackerViewId, addRotationX, addRotationY);
    }

    [PunRPC]
    protected void RpcCharacterUseSkill(
        int weaponId,
        Vector3 position,
        Vector3 direction,
        int attackerViewId,
        float addRotationX,
        float addRotationY)
    {
        // Instantiates damage entities on clients only
        if (!PhotonNetwork.IsMasterClient)
            DamageEntity.InstantiateNewEntityBySkill(weaponId, position, direction, attackerViewId, addRotationX, addRotationY);
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
        var headId = 0;
        var characterId = 0;
        var weaponId = 0;
        var savedHead = GameInstance.GetAvailableHead(PlayerSave.GetHead());
        var savedCharacter = GameInstance.GetAvailableCharacter(PlayerSave.GetCharacter());
        var savedWeapon = GameInstance.GetAvailableWeapon(PlayerSave.GetWeapon());
        if (savedHead != null)
            headId = savedHead.GetHashId();
        if (savedCharacter != null)
            characterId = savedCharacter.GetHashId();
        if (savedWeapon != null)
            weaponId = savedWeapon.GetHashId();
        character.CmdInit(headId, characterId, weaponId,
            selectCustomEquipments.ToArray(),
            "");
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
