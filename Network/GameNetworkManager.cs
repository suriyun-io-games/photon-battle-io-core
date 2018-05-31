using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameNetworkManager : BaseNetworkGameManager
{
    public static new GameNetworkManager Singleton
    {
        get { return SimplePhotonNetworkManager.Singleton as GameNetworkManager; }
    }

    [PunRPC]
    protected void RpcCharacterAttack(
        string weaponId, 
        bool isLeftHandWeapon, 
        Vector3 position, 
        Vector3 direction, 
        int attackerViewId, 
        float addRotationX, 
        float addRotationY)
    {
        // Instantiates damage entities on clients only
        if (!PhotonNetwork.isMasterClient)
            DamageEntity.InstantiateNewEntity(weaponId, isLeftHandWeapon, position, direction, attackerViewId, addRotationX, addRotationY);
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
        character.CmdInit(GameInstance.GetAvailableHead(PlayerSave.GetHead()).GetId(),
            GameInstance.GetAvailableCharacter(PlayerSave.GetCharacter()).GetId(),
            GameInstance.GetAvailableWeapon(PlayerSave.GetWeapon()).GetId(),
            "");
    }

    protected override void UpdateScores(NetworkGameScore[] scores)
    {
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
}
