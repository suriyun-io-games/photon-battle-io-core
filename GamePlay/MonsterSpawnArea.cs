using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MonsterSpawnArea : SpawnArea
{
    public MonsterEntity monsterPrefab;
    [Range(1, 100)]
    public int amount;

    public void SpawnMonsters()
    {
        for (int i = 0; i < amount; ++i)
        {
            PhotonNetwork.InstantiateRoomObject(monsterPrefab.name, GetSpawnPosition(), Quaternion.identity, 0, new object[0]);
        }
    }
}
