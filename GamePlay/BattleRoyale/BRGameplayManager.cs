using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;

[System.Serializable]
public struct BRCircle
{
    public SimpleSphereData circleData;
    public float shrinkDelay;
    public float shrinkDuration;
    [Range(0.01f, 1f)]
    public float hpRateDps;
}

[System.Serializable]
public struct BRPattern
{
    public BRCircle[] circles;
    public SimpleLineData spawnerMovement;
    public float spawnerMoveDuration;
}

public enum BRState : byte
{
    WaitingForPlayers,
    WaitingForFirstCircle,
    ShrinkDelaying,
    Shrinking,
    LastCircle,
}

public enum BRSpawnType : byte
{
    BattleRoyale,
    Random,
}

public class BRGameplayManager : GameplayManager
{
    public const string CUSTOM_ROOM_CURRENT_PATTERN = "b01";
    public const string CUSTOM_ROOM_CURRENT_CIRCLE = "b02";
    public const string CUSTOM_ROOM_CURRENT_RADIUS = "b03";
    public const string CUSTOM_ROOM_CURRENT_CENTER_POSITION = "b04";
    public const string CUSTOM_ROOM_NEXT_RADIUS = "b05";
    public const string CUSTOM_ROOM_NEXT_CENTER_POSITION = "b06";
    public const string CUSTOM_ROOM_CURRENT_STATE = "b07";
    public const string CUSTOM_ROOM_CURRENT_DURATION = "b08";
    public const string CUSTOM_ROOM_CURRENT_COUNTDOWN = "b09";
    public const string CUSTOM_ROOM_SPAWNER_MOVE_FROM = "b10";
    public const string CUSTOM_ROOM_SPAWNER_MOVE_TO = "b11";
    public const string CUSTOM_ROOM_SPAWNER_MOVE_DURATION = "b12";
    public const string CUSTOM_ROOM_SPAWNER_MOVE_COUNTDOWN = "b13";
    public const string CUSTOM_ROOM_COUNT_ALIVE_CHARACTERS = "b14";
    public const string CUSTOM_ROOM_COUNT_ALL_CHARACTERS = "b15";

    [Header("Battle Royale")]
    public BRSpawnType spawnType;
    public float waitForPlayersDuration;
    public float waitForFirstCircleDuration;
    public SimpleCubeData spawnableArea;
    public BRPattern[] patterns;
    public GameObject circleObject;
    public GameObject airplanePrefab;

    private bool spawnedAirplane;
    private GameObject airplane;

    #region Sync Vars
    private int _currentPattern;
    public int currentPattern
    {
        get
        {
            try { return (int)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_CURRENT_PATTERN); } catch { }
            return _currentPattern;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != currentPattern)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_CURRENT_PATTERN, value);
                _currentPattern = value;
            }
        }
    }
    private int _currentCircle;
    public int currentCircle
    {
        get
        {
            try { return (int)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_CURRENT_CIRCLE); } catch { }
            return _currentCircle;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != currentCircle)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_CURRENT_CIRCLE, value);
                _currentCircle = value;
            }
        }
    }
    private float _currentRadius;
    public float currentRadius
    {
        get
        {
            try { return (float)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_CURRENT_RADIUS); } catch { }
            return _currentRadius;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != currentRadius)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_CURRENT_RADIUS, value);
                _currentRadius = value;
            }
        }
    }
    private Vector3 _currentCenterPosition;
    public Vector3 currentCenterPosition
    {
        get
        {
            try { return (Vector3)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_CURRENT_CENTER_POSITION); } catch { }
            return _currentCenterPosition;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != currentCenterPosition)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_CURRENT_CENTER_POSITION, value);
                _currentCenterPosition = value;
            }
        }
    }
    private float _nextRadius;
    public float nextRadius
    {
        get
        {
            try { return (float)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_NEXT_RADIUS); } catch { }
            return _nextRadius;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != nextRadius)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_NEXT_RADIUS, value);
                _nextRadius = value;
            }
        }
    }
    private Vector3 _nextCenterPosition;
    public Vector3 nextCenterPosition
    {
        get
        {
            try { return (Vector3)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_NEXT_CENTER_POSITION); } catch { }
            return _nextCenterPosition;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != nextCenterPosition)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_NEXT_CENTER_POSITION, value);
                _nextCenterPosition = value;
            }
        }
    }
    private BRState _currentState;
    public BRState currentState
    {
        get
        {
            try { return (BRState)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_CURRENT_STATE); } catch { }
            return _currentState;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != currentState)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_CURRENT_STATE, (byte)value);
                _currentState = value;
            }
        }
    }
    private float _currentDuration;
    public float currentDuration
    {
        get
        {
            try { return (float)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_CURRENT_DURATION); } catch { }
            return _currentDuration;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != currentDuration)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_CURRENT_DURATION, value);
                _currentDuration = value;
            }
        }
    }
    private float _currentCountdown;
    public float currentCountdown
    {
        get
        {
            try { return (float)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_CURRENT_COUNTDOWN); } catch { }
            return _currentCountdown;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != currentCountdown)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_CURRENT_COUNTDOWN, value);
                photonView.AllRPC(RpcOnCurrentCountdownChanged, value);
                _currentCountdown = value;
            }
        }
    }
    private Vector3 _spawnerMoveFrom;
    public Vector3 spawnerMoveFrom
    {
        get
        {
            try { return (Vector3)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_SPAWNER_MOVE_FROM); } catch { }
            return _spawnerMoveFrom;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != spawnerMoveFrom)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_SPAWNER_MOVE_FROM, value);
                _spawnerMoveFrom = value;
            }
        }
    }
    private Vector3 _spawnerMoveTo;
    public Vector3 spawnerMoveTo
    {
        get
        {
            try { return (Vector3)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_SPAWNER_MOVE_TO); } catch { }
            return _spawnerMoveTo;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != spawnerMoveTo)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_SPAWNER_MOVE_TO, value);
                _spawnerMoveTo = value;
            }
        }
    }
    private float _spawnerMoveDuration;
    public float spawnerMoveDuration
    {
        get
        {
            try { return (float)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_SPAWNER_MOVE_DURATION); } catch { }
            return _spawnerMoveDuration;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != spawnerMoveDuration)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_SPAWNER_MOVE_DURATION, value);
                _spawnerMoveDuration = value;
            }
        }
    }
    private float _spawnerMoveCountdown;
    public float spawnerMoveCountdown
    {
        get
        {
            try { return (float)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_SPAWNER_MOVE_COUNTDOWN); } catch { }
            return _spawnerMoveCountdown;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != spawnerMoveCountdown)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_SPAWNER_MOVE_COUNTDOWN, value);
                photonView.AllRPC(RpcOnSpawnerMoveCountdownChanged, value);
                _spawnerMoveCountdown = value;
            }
        }
    }
    private int _countAliveCharacters;
    public int countAliveCharacters
    {
        get
        {
            try { return (int)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_COUNT_ALIVE_CHARACTERS); } catch { }
            return _countAliveCharacters;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != countAliveCharacters)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_COUNT_ALIVE_CHARACTERS, value);
                _countAliveCharacters = value;
            }
        }
    }
    private int _countAllCharacters;
    public int countAllCharacters
    {
        get
        {
            try { return (int)SimplePhotonNetworkManager.Singleton.GetRoomProperty(CUSTOM_ROOM_COUNT_ALL_CHARACTERS); } catch { }
            return _countAllCharacters;
        }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != countAllCharacters)
            {
                SimplePhotonNetworkManager.Singleton.SetRoomProperty(CUSTOM_ROOM_COUNT_ALL_CHARACTERS, value);
                _countAllCharacters = value;
            }
        }
    }
    #endregion

    public float CurrentCircleHpRateDps { get; private set; }
    // make this as field to make client update smoothly
    public float CurrentCountdown { get; private set; }
    // make this as field to make client update smoothly
    public float SpawnerMoveCountdown { get; private set; }
    public readonly List<BRCharacterEntityExtra> SpawningCharacters = new List<BRCharacterEntityExtra>();
    private float currentShrinkDuration;
    private float startShrinkRadius;
    private Vector3 startShrinkCenterPosition;
    private bool isInSpawnableArea;
    private float secondCollector;
    private BRPattern randomedPattern { get { return patterns[currentPattern]; } }

    protected override void OnStartServer()
    {
        currentPattern = Random.Range(0, patterns.Length);
        currentCircle = 0;
        currentRadius = 0;
        currentState = BRState.WaitingForPlayers;
        currentDuration = currentCountdown = waitForPlayersDuration;
        CurrentCircleHpRateDps = 0;
        CurrentCountdown = 0;
        SpawnerMoveCountdown = 0;
        isInSpawnableArea = false;
    }

    public override bool CanRespawn(CharacterEntity character)
    {
        return false;
    }

    public override bool CanReceiveDamage(CharacterEntity damageReceiver, CharacterEntity attacker)
    {
        if (base.CanReceiveDamage(damageReceiver, attacker))
            return damageReceiver.GetComponent<BRCharacterEntityExtra>().isSpawned;
        return false;
    }

    public override bool CanApplyStatusEffect(CharacterEntity effectReceiver, CharacterEntity effectApplier)
    {
        if (base.CanApplyStatusEffect(effectReceiver, effectApplier))
            return effectReceiver.GetComponent<BRCharacterEntityExtra>().isSpawned;
        return false;
    }

    public override bool CanAttack(CharacterEntity character)
    {
        var networkGameplayManager = BaseNetworkGameManager.Singleton;
        if (networkGameplayManager != null && networkGameplayManager.IsMatchEnded)
            return false;
        var extra = character.GetComponent<BRCharacterEntityExtra>();
        return currentState == BRState.WaitingForPlayers || (extra.isSpawned && extra.isGroundOnce);
    }

    private void Update()
    {
        var networkGameManager = BaseNetworkGameManager.Singleton;
        if (!PhotonNetwork.IsMasterClient)
            networkGameManager.maxConnections = (byte)countAllCharacters;
        UpdateGameState();
        UpdateCircle();
        UpdateSpawner();
        if (circleObject != null)
        {
            circleObject.SetActive(currentState != BRState.WaitingForPlayers);
            circleObject.transform.localScale = Vector3.one * currentRadius * 2f;
            circleObject.transform.position = currentCenterPosition;
        }
        if (CurrentCountdown > 0)
            CurrentCountdown -= Time.unscaledDeltaTime;
        if (SpawnerMoveCountdown > 0)
        {
            SpawnerMoveCountdown -= Time.unscaledDeltaTime;
            if (!spawnedAirplane)
            {
                spawnedAirplane = true;
                if (airplanePrefab != null)
                    airplane = Instantiate(airplanePrefab);
            }
            if (airplane != null)
            {
                airplane.transform.position = GetSpawnerPosition();
                airplane.transform.rotation = GetSpawnerRotation();
            }
        }
        else
        {
            if (spawnedAirplane)
            {
                if (airplane != null)
                    Destroy(airplane);
            }
        }
        if (PhotonNetwork.IsMasterClient)
        {
            secondCollector += Time.unscaledDeltaTime;
            if (secondCollector > 1f)
            {
                secondCollector = 0f;
                currentCountdown -= 1f;
                if (currentState != BRState.WaitingForPlayers && spawnerMoveCountdown >= 0f)
                    spawnerMoveCountdown -= 1f;
            }
        }
    }

    private void UpdateGameState()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        var networkGameManager = BaseNetworkGameManager.Singleton;
        var gameRule = networkGameManager.gameRule == null ? null : networkGameManager.gameRule as BattleRoyaleNetworkGameRule;
        var characters = networkGameManager.Characters;
        countAliveCharacters = networkGameManager.CountAliveCharacters();
        countAllCharacters = PhotonNetwork.CurrentRoom.MaxPlayers;
        BRCircle circle;
        switch (currentState)
        {
            case BRState.WaitingForPlayers:
                // Start game immediately when players are full
                if (currentCountdown <= 0)
                {
                    foreach (var character in characters)
                    {
                        if (character == null)
                            continue;
                        SpawningCharacters.Add(character.GetComponent<BRCharacterEntityExtra>());
                    }
                    spawnerMoveFrom = randomedPattern.spawnerMovement.GetFromPosition();
                    spawnerMoveTo = randomedPattern.spawnerMovement.GetToPosition();
                    spawnerMoveDuration = spawnerMoveCountdown = randomedPattern.spawnerMoveDuration;
                    if (gameRule != null)
                        gameRule.AddBots();
                    currentState = BRState.WaitingForFirstCircle;
                    currentDuration = currentCountdown = waitForFirstCircleDuration;
                    // Spawn powerup items
                    foreach (var powerUp in powerUps)
                    {
                        if (powerUp.powerUpPrefab == null)
                            continue;
                        for (var i = 0; i < powerUp.amount; ++i)
                            SpawnPowerUp(powerUp.powerUpPrefab.name);
                    }
                }
                break;
            case BRState.WaitingForFirstCircle:
                if (currentCountdown <= 0)
                {
                    currentCircle = 0;
                    if (TryGetCircle(out circle))
                    {
                        currentState = BRState.ShrinkDelaying;
                        currentDuration = currentCountdown = circle.shrinkDelay;
                        CurrentCircleHpRateDps = circle.hpRateDps;
                        startShrinkRadius = currentRadius = circle.circleData.radius;
                        startShrinkCenterPosition = currentCenterPosition = circle.circleData.transform.position;
                        nextRadius = circle.circleData.radius;
                        nextCenterPosition = circle.circleData.transform.position;
                    }
                    else
                    {
                        currentState = BRState.LastCircle;
                        currentDuration = currentCountdown = 0;
                    }
                }
                break;
            case BRState.ShrinkDelaying:
                if (currentCountdown <= 0)
                {
                    BRCircle nextCircle;
                    if (TryGetCircle(out circle) && TryGetCircle(currentCircle + 1, out nextCircle))
                    {
                        currentState = BRState.Shrinking;
                        currentShrinkDuration = currentDuration = currentCountdown = circle.shrinkDuration;
                        CurrentCircleHpRateDps = circle.hpRateDps;
                        startShrinkRadius = currentRadius = circle.circleData.radius;
                        startShrinkCenterPosition = currentCenterPosition = circle.circleData.transform.position;
                        nextRadius = nextCircle.circleData.radius;
                        nextCenterPosition = nextCircle.circleData.transform.position;
                    }
                    else
                    {
                        currentState = BRState.LastCircle;
                        currentDuration = currentCountdown = 0;
                    }
                }
                break;
            case BRState.Shrinking:
                if (currentCountdown <= 0)
                {
                    ++currentCircle;
                    BRCircle nextCircle;
                    if (TryGetCircle(out circle) && TryGetCircle(currentCircle + 1, out nextCircle))
                    {
                        currentState = BRState.ShrinkDelaying;
                        currentDuration = currentCountdown = circle.shrinkDelay;
                        CurrentCircleHpRateDps = circle.hpRateDps;
                    }
                    else
                    {
                        currentState = BRState.LastCircle;
                        currentDuration = currentCountdown = 0;
                    }
                }
                break;
            case BRState.LastCircle:
                currentDuration = currentCountdown = 0;
                break;
        }
    }

    private void UpdateCircle()
    {
        if (currentState == BRState.Shrinking)
        {
            var interp = (currentShrinkDuration - CurrentCountdown) / currentShrinkDuration;
            currentRadius = Mathf.Lerp(startShrinkRadius, nextRadius, interp);
            currentCenterPosition = Vector3.Lerp(startShrinkCenterPosition, nextCenterPosition, interp);
        }
    }

    private void UpdateSpawner()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (currentState != BRState.WaitingForPlayers)
        {
            if (!isInSpawnableArea && IsSpawnerInsideSpawnableArea())
                isInSpawnableArea = true;

            if (isInSpawnableArea && !IsSpawnerInsideSpawnableArea())
            {
                var characters = SpawningCharacters;
                foreach (var character in characters)
                {
                    if (character == null)
                        continue;
                    character.ServerCharacterSpawn();
                }
                // Spawn players that does not spawned
                isInSpawnableArea = false;
            }
        }
    }

    public bool TryGetCircle(out BRCircle circle)
    {
        return TryGetCircle(currentCircle, out circle);
    }

    public bool TryGetCircle(int currentCircle, out BRCircle circle)
    {
        circle = new BRCircle();
        if (currentCircle < 0 || currentCircle >= randomedPattern.circles.Length)
            return false;
        circle = randomedPattern.circles[currentCircle];
        return true;
    }

    public Vector3 GetSpawnerPosition()
    {
        var interp = (spawnerMoveDuration - SpawnerMoveCountdown) / spawnerMoveDuration;
        return Vector3.Lerp(spawnerMoveFrom, spawnerMoveTo, interp);
    }

    public Quaternion GetSpawnerRotation()
    {
        var heading = spawnerMoveTo - spawnerMoveFrom;
        heading.y = 0f;
        return Quaternion.LookRotation(heading, Vector3.up);
    }

    public bool CanSpawnCharacter(CharacterEntity character)
    {
        var extra = character.GetComponent<BRCharacterEntityExtra>();
        return PhotonNetwork.IsMasterClient && (extra == null || !extra.isSpawned) && IsSpawnerInsideSpawnableArea();
    }

    public bool IsSpawnerInsideSpawnableArea()
    {
        var position = GetSpawnerPosition();
        var dist = Vector3.Distance(position, spawnableArea.transform.position);
        return dist <= spawnableArea.size.x * 0.5f &&
            dist <= spawnableArea.size.z * 0.5f;
    }

    public Vector3 SpawnCharacter(CharacterEntity character)
    {
        return character.CacheTransform.position = GetSpawnerPosition();
    }

    [PunRPC]
    protected void RpcOnCurrentCountdownChanged(float currentCountdown)
    {
        CurrentCountdown = currentCountdown;
    }

    [PunRPC]
    protected void RpcOnSpawnerMoveCountdownChanged(float spawnerMoveCountdown)
    {
        SpawnerMoveCountdown = spawnerMoveCountdown;
    }
}
