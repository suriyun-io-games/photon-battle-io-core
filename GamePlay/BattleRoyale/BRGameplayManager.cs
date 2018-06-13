using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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

public class BRGameplayManager : GameplayManager
{
    public const string CUSTOM_ROOM_CURRENT_PATTERN = "iCP";
    public const string CUSTOM_ROOM_CURRENT_CIRCLE = "iCC";
    public const string CUSTOM_ROOM_CURRENT_RADIUS = "fCR";
    public const string CUSTOM_ROOM_CURRENT_CENTER_POSITION = "v3CCP";
    public const string CUSTOM_ROOM_NEXT_RADIUS = "fNR";
    public const string CUSTOM_ROOM_NEXT_CENTER_POSITION = "v3NCP";
    public const string CUSTOM_ROOM_CURRENT_STATE = "spCS";
    public const string CUSTOM_ROOM_CURRENT_DURATION = "fCD";
    public const string CUSTOM_ROOM_CURRENT_COUNTDOWN = "fCCD";
    public const string CUSTOM_ROOM_SPAWNER_MOVE_FROM = "v3SMF";
    public const string CUSTOM_ROOM_SPAWNER_MOVE_TO = "v3SMT";
    public const string CUSTOM_ROOM_SPAWNER_MOVE_DURATION = "v3SMD";
    public const string CUSTOM_ROOM_SPAWNER_MOVE_COUNTDOWN = "v3SMCD";
    public const string CUSTOM_ROOM_COUNT_ALIVE_CHARACTERS = "iALI";
    public const string CUSTOM_ROOM_COUNT_ALL_CHARACTERS = "iALL";

    [Header("Battle Royale")]
    public float waitForPlayersDuration;
    public float waitForFirstCircleDuration;
    public SimpleCubeData spawnableArea;
    public BRPattern[] patterns;
    public GameObject circleObject;

    #region Sync Vars
    public int currentPattern
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_CURRENT_PATTERN]; } catch { } return 0; }
        set { if (PhotonNetwork.isMasterClient && value != currentPattern) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_CURRENT_PATTERN, value } }); }
    }
    public int currentCircle
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_CURRENT_CIRCLE]; } catch { } return 0; }
        set { if (PhotonNetwork.isMasterClient && value != currentCircle) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_CURRENT_CIRCLE, value } }); }
    }
    public float currentRadius
    {
        get { try { return (float)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_CURRENT_RADIUS]; } catch { } return 0f; }
        set { if (PhotonNetwork.isMasterClient && value != currentRadius) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_CURRENT_RADIUS, value } }); }
    }
    public Vector3 currentCenterPosition
    {
        get { try { return (Vector3)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_CURRENT_CENTER_POSITION]; } catch { } return Vector3.zero; }
        set { if (PhotonNetwork.isMasterClient && value != currentCenterPosition) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_CURRENT_CENTER_POSITION, value } }); }
    }
    public float nextRadius
    {
        get { try { return (float)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_NEXT_RADIUS]; } catch { } return 0f; }
        set { if (PhotonNetwork.isMasterClient && value != nextRadius) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_NEXT_RADIUS, value } }); }
    }
    public Vector3 nextCenterPosition
    {
        get { try { return (Vector3)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_NEXT_CENTER_POSITION]; } catch { } return Vector3.zero; }
        set { if (PhotonNetwork.isMasterClient && value != nextCenterPosition) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_NEXT_CENTER_POSITION, value } }); }
    }
    public BRState currentState
    {
        get { try { return (BRState)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_CURRENT_STATE]; } catch { } return BRState.WaitingForPlayers; }
        set { if (PhotonNetwork.isMasterClient && value != currentState) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_CURRENT_STATE, (byte)value } }); }
    }
    public float currentDuration
    {
        get { try { return (float)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_CURRENT_DURATION]; } catch { } return 0f; }
        set { if (PhotonNetwork.isMasterClient && value != currentDuration) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_CURRENT_DURATION, value } }); }
    }
    public float currentCountdown
    {
        get { try { return (float)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_CURRENT_COUNTDOWN]; } catch { } return 0f; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != currentCountdown)
            {
                PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_CURRENT_COUNTDOWN, value } });
                photonView.RPC("RpcOnCurrentCountdownChanged", PhotonTargets.All, value);
            }
        }
    }
    public Vector3 spawnerMoveFrom
    {
        get { try { return (Vector3)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_SPAWNER_MOVE_FROM]; } catch { } return Vector3.zero; }
        set { if (PhotonNetwork.isMasterClient && value != spawnerMoveFrom) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_SPAWNER_MOVE_FROM, value } }); }
    }
    public Vector3 spawnerMoveTo
    {
        get { try { return (Vector3)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_SPAWNER_MOVE_TO]; } catch { } return Vector3.zero; }
        set { if (PhotonNetwork.isMasterClient && value != spawnerMoveTo) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_SPAWNER_MOVE_TO, value } }); }
    }
    public float spawnerMoveDuration
    {
        get { try { return (float)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_SPAWNER_MOVE_DURATION]; } catch { } return 0f; }
        set { if (PhotonNetwork.isMasterClient && value != spawnerMoveDuration) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_SPAWNER_MOVE_DURATION, value } }); }
    }
    public float spawnerMoveCountdown
    {
        get { try { return (float)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_SPAWNER_MOVE_COUNTDOWN]; } catch { } return 0f; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != spawnerMoveCountdown)
            {
                PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_SPAWNER_MOVE_COUNTDOWN, value } });
                photonView.RPC("RpcOnSpawnerMoveCountdownChanged", PhotonTargets.All, value);
            }
        }
    }
    public int countAliveCharacters
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_COUNT_ALIVE_CHARACTERS]; } catch { } return 0; }
        set { if (PhotonNetwork.isMasterClient && value != countAliveCharacters) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_COUNT_ALIVE_CHARACTERS, value } }); }
    }
    public int countAllCharacters
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_COUNT_ALL_CHARACTERS]; } catch { } return 0; }
        set { if (PhotonNetwork.isMasterClient && value != countAllCharacters) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { CUSTOM_ROOM_COUNT_ALL_CHARACTERS, value } }); }
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

    public override bool CanReceiveDamage(CharacterEntity character)
    {
        var networkGameplayManager = BaseNetworkGameManager.Singleton;
        if (networkGameplayManager != null && networkGameplayManager.IsMatchEnded)
            return false;
        var extra = character.GetComponent<BRCharacterEntityExtra>();
        return extra.isSpawned;
    }

    private void Update()
    {
        var networkGameManager = BaseNetworkGameManager.Singleton;
        if (!PhotonNetwork.isMasterClient)
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
            CurrentCountdown -= Time.deltaTime;
        if (SpawnerMoveCountdown > 0)
            SpawnerMoveCountdown -= Time.deltaTime;
    }

    private void UpdateGameState()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        currentCountdown -= Time.deltaTime;
        var networkGameManager = BaseNetworkGameManager.Singleton;
        var gameRule = networkGameManager.gameRule == null ? null : networkGameManager.gameRule as BattleRoyaleNetworkGameRule;
        var characters = networkGameManager.Characters;
        countAliveCharacters = networkGameManager.CountAliveCharacters();
        countAllCharacters = PhotonNetwork.room.MaxPlayers;
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
                    // Spawn powerup and pickup items
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
        if (!PhotonNetwork.isMasterClient)
            return;

        if (currentState != BRState.WaitingForPlayers)
        {
            spawnerMoveCountdown -= Time.deltaTime;

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
        return PhotonNetwork.isMasterClient && (extra == null || !extra.isSpawned) && IsSpawnerInsideSpawnableArea();
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
        var spawnPosition = character.TempTransform.position = GetSpawnerPosition();
        return spawnPosition;
    }

    [PunRPC]
    protected void RpcOnCurrentCountdownChanged(float currentCountdown)
    {
        CurrentCountdown = this.currentCountdown = currentCountdown;
    }

    [PunRPC]
    protected void RpcOnSpawnerMoveCountdownChanged(float spawnerMoveCountdown)
    {
        SpawnerMoveCountdown = this.spawnerMoveCountdown = spawnerMoveCountdown;
    }
}
