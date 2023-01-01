using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Threading.Tasks;

[RequireComponent(typeof(CharacterEntity))]
public class BRCharacterEntityExtra : MonoBehaviourPunCallbacks
{
    public static float BotSpawnDuration = 0f;
    protected bool _isSpawned;
    public bool isSpawned
    {
        get { return _isSpawned; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != _isSpawned)
            {
                _isSpawned = value;
                photonView.OthersRPC(RpcUpdateIsSpawned, value);
            }
        }
    }
    public bool isGroundOnce { get; private set; }
    public Transform CacheTransform { get; private set; }
    public CharacterEntity CacheCharacterEntity { get; private set; }
    public CharacterMovement CacheCharacterMovement
    {
        get { return CacheCharacterEntity.CacheCharacterMovement; }
    }
    private float botRandomSpawn;
    private bool botSpawnCalled;
    private bool botDeadRemoveCalled;
    private float lastCircleCheckTime;

    public bool IsMine { get { return photonView.IsMine && !(CacheCharacterEntity is BotEntity); } }

    private void Awake()
    {
        CacheTransform = transform;
        CacheCharacterEntity = GetComponent<CharacterEntity>();
        CacheCharacterEntity.enabled = false;
        CacheCharacterEntity.IsHidding = true;
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (IsMine)
        {
            if (brGameManager != null && brGameManager.currentState != BRState.WaitingForPlayers)
                GameNetworkManager.Singleton.LeaveRoom();
        }
        botRandomSpawn = BotSpawnDuration = BotSpawnDuration + Random.Range(0.1f, 1f);
    }

    private void Start()
    {
        CacheCharacterEntity.onDead += OnDead;
    }

    private void OnDestroy()
    {
        CacheCharacterEntity.onDead -= OnDead;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (!PhotonNetwork.IsMasterClient)
            return;
        photonView.TargetRPC(RpcUpdateIsSpawned, newPlayer, isSpawned);
    }

    private void Update()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager == null)
            return;
        var botEntity = CacheCharacterEntity as BotEntity;
        // Monster entity does not have to move following the air plane
        if (PhotonNetwork.IsMasterClient && CacheCharacterEntity is MonsterEntity)
        {
            if (!CacheCharacterMovement.enabled)
                CacheCharacterMovement.enabled = true;
            if (!CacheCharacterEntity.enabled)
                CacheCharacterEntity.enabled = true;
            CacheCharacterEntity.IsHidding = false;
            if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned)
                isSpawned = true;
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (brGameManager.currentState != BRState.WaitingForPlayers && Time.realtimeSinceStartup - lastCircleCheckTime >= 1f)
            {
                var currentPosition = CacheTransform.position;
                currentPosition.y = 0;

                var centerPosition = brGameManager.currentCenterPosition;
                centerPosition.y = 0;
                var distance = Vector3.Distance(currentPosition, centerPosition);
                var currentRadius = brGameManager.currentRadius;
                if (distance > currentRadius)
                    CacheCharacterEntity.Hp -= Mathf.CeilToInt(brGameManager.CurrentCircleHpRateDps * CacheCharacterEntity.TotalHp);
                lastCircleCheckTime = Time.realtimeSinceStartup;
                if (botEntity != null)
                {
                    botEntity.isFixRandomMoveAroundPoint = currentRadius > 0 && distance > currentRadius;
                    botEntity.fixRandomMoveAroundPoint = centerPosition;
                    botEntity.fixRandomMoveAroundDistance = currentRadius;
                }
            }
        }

        if (brGameManager.currentState == BRState.WaitingForPlayers || isSpawned)
        {
            if (PhotonNetwork.IsMasterClient && !botDeadRemoveCalled && botEntity != null && CacheCharacterEntity.IsDead)
            {
                botDeadRemoveCalled = true;
                StartCoroutine(BotDeadRemoveRoutine());
            }
            if (!CacheCharacterMovement.enabled)
                CacheCharacterMovement.enabled = true;
            if (!CacheCharacterEntity.enabled)
                CacheCharacterEntity.enabled = true;
            CacheCharacterEntity.IsHidding = false;
        }

        switch (brGameManager.spawnType)
        {
            case BRSpawnType.BattleRoyale:
                UpdateSpawnBattleRoyale();
                break;
            case BRSpawnType.Random:
                UpdateSpawnRandom();
                break;
        }

        if (isSpawned && !isGroundOnce && CacheCharacterMovement.IsGrounded)
            isGroundOnce = true;
    }

    private void UpdateSpawnBattleRoyale()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager == null)
            return;
        var botEntity = CacheCharacterEntity as BotEntity;
        if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned)
        {
            if (PhotonNetwork.IsMasterClient && !botSpawnCalled && botEntity != null && brGameManager.CanSpawnCharacter(CacheCharacterEntity))
            {
                botSpawnCalled = true;
                StartCoroutine(BotSpawnRoutine());
            }
            // Hide character and disable physics while in airplane
            if (CacheCharacterMovement.enabled)
                CacheCharacterMovement.enabled = false;
            if (CacheCharacterEntity.enabled)
                CacheCharacterEntity.enabled = false;
            CacheCharacterEntity.IsHidding = true;
            // Move position / rotation follow the airplane
            if (PhotonNetwork.IsMasterClient || IsMine)
            {
                var position = brGameManager.GetSpawnerPosition();
                CacheCharacterMovement.SetPosition(position);
                CacheTransform.rotation = brGameManager.GetSpawnerRotation();
            }
        }
    }

    private void UpdateSpawnRandom()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager == null)
            return;

        if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned && PhotonNetwork.IsMasterClient)
        {
            var position = CacheCharacterEntity.GetSpawnPosition();
            CacheCharacterMovement.SetPosition(position);
            CacheCharacterEntity.photonView.TargetRPC(CacheCharacterEntity.RpcTargetSpawn, CacheCharacterEntity.photonView.Owner, position.x, position.y, position.z);
            isSpawned = true;
        }
    }

    IEnumerator BotSpawnRoutine()
    {
        yield return new WaitForSeconds(botRandomSpawn);
        ServerCharacterSpawn();
    }

    IEnumerator BotDeadRemoveRoutine()
    {
        yield return new WaitForSeconds(5f);
        PhotonNetwork.Destroy(gameObject);
    }

    private void OnDead()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameplayManager != null)
            photonView.TargetRPC(RpcRankResult, photonView.Owner, BaseNetworkGameManager.Singleton.CountAliveCharacters() + 1);
    }

    public void ServerCharacterSpawn()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null)
        {
            isSpawned = true;
            photonView.AllRPC(RpcCharacterSpawned, brGameplayManager.SpawnCharacter(CacheCharacterEntity) + new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-2.5f, 2.5f)));
        }
    }

    public void CmdCharacterSpawn()
    {
        photonView.MasterRPC(RpcServerCharacterSpawn);
    }

    [PunRPC]
    public void RpcServerCharacterSpawn()
    {
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null && brGameplayManager.CanSpawnCharacter(CacheCharacterEntity))
            ServerCharacterSpawn();
    }

    [PunRPC]
    public void RpcCharacterSpawned(Vector3 spawnPosition)
    {
        CacheCharacterMovement.SetPosition(spawnPosition);
        CacheCharacterMovement.enabled = true;
    }

    [PunRPC]
    public virtual void RpcRankResult(int rank)
    {
        if (!IsMine)
            return;
        if (GameNetworkManager.Singleton.gameRule is BattleRoyaleNetworkGameRule)
            (GameNetworkManager.Singleton.gameRule as BattleRoyaleNetworkGameRule).SetRewards(rank);
        ShowRankResultRoutine(rank);
    }

    async void ShowRankResultRoutine(int rank)
    {
        await Task.Delay(3000);
        if (UIBRGameplay.Singleton != null)
            UIBRGameplay.Singleton.ShowRankResult(rank);
    }

    [PunRPC]
    public virtual void RpcUpdateIsSpawned(bool isSpawned)
    {
        _isSpawned = isSpawned;
    }
}
