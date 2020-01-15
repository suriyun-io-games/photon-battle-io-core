using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;

public class GameplayManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public struct RewardCurrency
    {
        public string currencyId;
        public IntAttribute amount;
    }
    public const int UNIQUE_VIEW_ID = 998;
    public const float REAL_MOVE_SPEED_RATE = 0.1f;
    public static GameplayManager Singleton { get; private set; }
    [Header("Character stats")]
    public int maxLevel = 1000;
    public IntAttribute exp = new IntAttribute() { minValue = 20, maxValue = 1023050, growth = 2.5f };
    public IntAttribute rewardExp = new IntAttribute() { minValue = 8, maxValue = 409220, growth = 2.5f };
    public RewardCurrency[] rewardCurrencies;
    public IntAttribute killScore = new IntAttribute() { minValue = 10, maxValue = 511525, growth = 1f };
    public int minHp = 100;
    public int minAttack = 30;
    public int minDefend = 20;
    public int minMoveSpeed = 30;
    public int maxSpreadDamages = 6;
    public int addingStatPoint = 1;
    public float minAttackVaryRate = -0.07f;
    public float maxAttackVaryRate = 0.07f;
    public CharacterAttributes[] availableAttributes;
    [Header("Game rules")]
    public int watchAdsRespawnAvailable = 2;
    public float respawnDuration = 5f;
    public float invincibleDuration = 1.5f;
    public SpawnArea[] characterSpawnAreas;
    public SpawnArea[] characterSpawnAreasForTeamA;
    public SpawnArea[] characterSpawnAreasForTeamB;
    public SpawnArea[] powerUpSpawnAreas;
    public PowerUpSpawnData[] powerUps;
    public readonly Dictionary<string, PowerUpEntity> powerUpEntities = new Dictionary<string, PowerUpEntity>();
    public readonly Dictionary<string, CharacterAttributes> attributes = new Dictionary<string, CharacterAttributes>();

    protected virtual void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;

        powerUpEntities.Clear();
        foreach (var powerUp in powerUps)
        {
            var powerUpPrefab = powerUp.powerUpPrefab;
            if (powerUpPrefab != null && !powerUpEntities.ContainsKey(powerUpPrefab.name))
                powerUpEntities.Add(powerUpPrefab.name, powerUpPrefab);
        }
        attributes.Clear();
        foreach (var availableAttribute in availableAttributes)
        {
            attributes[availableAttribute.name] = availableAttribute;
        }
        // Set unique view id
        PhotonView view = GetComponent<PhotonView>();
        if (view == null)
            view = gameObject.AddComponent<PhotonView>();
        view.ViewID = UNIQUE_VIEW_ID;
    }

    protected virtual void Start()
    {
        if (PhotonNetwork.IsMasterClient)
            OnStartServer();
    }

    protected virtual void OnStartServer()
    {
        foreach (var powerUp in powerUps)
        {
            if (powerUp.powerUpPrefab == null)
                continue;
            for (var i = 0; i < powerUp.amount; ++i)
                SpawnPowerUp(powerUp.powerUpPrefab.name);
        }
        // Spawn monsters
        MonsterSpawnArea[] monsterSpawnAreas = FindObjectsOfType<MonsterSpawnArea>();
        foreach (var monsterSpawnArea in monsterSpawnAreas)
        {
            monsterSpawnArea.SpawnMonsters();
        }
    }

    public void SpawnPowerUp(string prefabName)
    {
        if (!PhotonNetwork.IsMasterClient || string.IsNullOrEmpty(prefabName))
            return;
        PowerUpEntity powerUpPrefab = null;
        if (powerUpEntities.TryGetValue(prefabName, out powerUpPrefab))
        {
            var powerUpEntityGo = PhotonNetwork.InstantiateSceneObject(powerUpPrefab.name, GetPowerUpSpawnPosition(), Quaternion.identity, 0, new object[0]);
            var powerUpEntity = powerUpEntityGo.GetComponent<PowerUpEntity>();
            powerUpEntity.prefabName = prefabName;
        }
    }

    public Vector3 GetCharacterSpawnPosition(CharacterEntity character)
    {
        if (character.playerTeam == PunTeams.Team.red && 
            characterSpawnAreasForTeamA != null &&
            characterSpawnAreasForTeamA.Length > 0)
            return characterSpawnAreasForTeamA[Random.Range(0, characterSpawnAreasForTeamA.Length)].GetSpawnPosition();

        if (character.playerTeam == PunTeams.Team.blue &&
            characterSpawnAreasForTeamB != null &&
            characterSpawnAreasForTeamB.Length > 0)
            return characterSpawnAreasForTeamB[Random.Range(0, characterSpawnAreasForTeamB.Length)].GetSpawnPosition();

        if (characterSpawnAreas == null || characterSpawnAreas.Length == 0)
            return Vector3.zero;
        return characterSpawnAreas[Random.Range(0, characterSpawnAreas.Length)].GetSpawnPosition();
    }

    public Vector3 GetPowerUpSpawnPosition()
    {
        if (powerUpSpawnAreas == null || powerUpSpawnAreas.Length == 0)
            return Vector3.zero;
        return powerUpSpawnAreas[Random.Range(0, powerUpSpawnAreas.Length)].GetSpawnPosition();
    }

    public int GetExp(int currentLevel)
    {
        return exp.Calculate(currentLevel, maxLevel);
    }

    public int GetRewardExp(int currentLevel)
    {
        return rewardExp.Calculate(currentLevel, maxLevel);
    }

    public int GetKillScore(int currentLevel)
    {
        return killScore.Calculate(currentLevel, maxLevel);
    }

    public virtual bool CanRespawn(CharacterEntity character)
    {
        var networkGameplayManager = BaseNetworkGameManager.Singleton;
        if (networkGameplayManager != null)
        {
            if (networkGameplayManager.IsMatchEnded)
                return false;
        }
        return true;
    }

    public virtual bool CanReceiveDamage(CharacterEntity damageReceiver, CharacterEntity attacker)
    {
        var networkGameplayManager = BaseNetworkGameManager.Singleton;
        if (networkGameplayManager != null)
        {
            if (networkGameplayManager.gameRule != null && networkGameplayManager.gameRule.IsTeamGameplay)
                return damageReceiver.playerTeam != attacker.playerTeam;
            if (networkGameplayManager.IsMatchEnded)
                return false;
        }
        return true;
    }

    public virtual bool CanAttack(CharacterEntity character)
    {
        var networkGameplayManager = BaseNetworkGameManager.Singleton;
        if (networkGameplayManager != null)
        {
            if (networkGameplayManager.IsMatchEnded)
                return false;
        }
        return true;
    }
}
