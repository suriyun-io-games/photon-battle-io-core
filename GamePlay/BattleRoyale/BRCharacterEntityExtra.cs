using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;

[RequireComponent(typeof(CharacterEntity))]
public class BRCharacterEntityExtra : PunBehaviour
{
    protected bool _isSpawned;
    public bool isSpawned
    {
        get { return _isSpawned; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != _isSpawned)
            {
                _isSpawned = value;
                photonView.RPC("RpcUpdateIsSpawned", PhotonTargets.Others, value);
            }
        }
    }

    private Transform tempTransform;
    public Transform TempTransform
    {
        get
        {
            if (tempTransform == null)
                tempTransform = GetComponent<Transform>();
            return tempTransform;
        }
    }

    private CharacterEntity tempCharacterEntity;
    public CharacterEntity TempCharacterEntity
    {
        get
        {
            if (tempCharacterEntity == null)
                tempCharacterEntity = GetComponent<CharacterEntity>();
            return tempCharacterEntity;
        }
    }
    private float botRandomSpawn;
    private bool botSpawnCalled;
    private bool botDeadRemoveCalled;
    private float lastCircleCheckTime;

    public bool IsMine { get { return photonView.isMine && !(TempCharacterEntity is BotEntity); } }

    private void Awake()
    {
        TempCharacterEntity.enabled = false;
        TempCharacterEntity.IsHidding = true;
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        var maxRandomDist = 30f;
        if (brGameManager != null)
            maxRandomDist = brGameManager.spawnerMoveDuration * 0.25f;
        botRandomSpawn = Random.Range(0f, maxRandomDist);

        if (IsMine)
        {
            if (brGameManager != null && brGameManager.currentState != BRState.WaitingForPlayers)
                GameNetworkManager.Singleton.LeaveRoom();
        }
    }

    private void Start()
    {
        TempCharacterEntity.onDead += OnDead;
    }

    private void OnDestroy()
    {
        TempCharacterEntity.onDead -= OnDead;
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        base.OnPhotonPlayerConnected(newPlayer);
        if (!PhotonNetwork.isMasterClient)
            return;
        photonView.RPC("RpcUpdateIsSpawned", newPlayer, isSpawned);
    }

    private void Update()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (PhotonNetwork.isMasterClient)
        {
            if (brGameManager.currentState != BRState.WaitingForPlayers && Time.realtimeSinceStartup - lastCircleCheckTime >= 1f)
            {
                var currentPosition = TempTransform.position;
                currentPosition.y = 0;

                var centerPosition = brGameManager.currentCenterPosition;
                centerPosition.y = 0;
                var distance = Vector3.Distance(currentPosition, centerPosition);
                var currentRadius = brGameManager.currentRadius;
                if (distance > currentRadius)
                    TempCharacterEntity.Hp -= Mathf.CeilToInt(brGameManager.CurrentCircleHpRateDps * TempCharacterEntity.TotalHp);
                lastCircleCheckTime = Time.realtimeSinceStartup;
            }
        }
        if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned)
        {
            if (PhotonNetwork.isMasterClient && !botSpawnCalled && TempCharacterEntity is BotEntity && brGameManager.CanSpawnCharacter(TempCharacterEntity))
            {
                botSpawnCalled = true;
                StartCoroutine(BotSpawnRoutine());
            }
            if (TempCharacterEntity.TempRigidbody.useGravity)
                TempCharacterEntity.TempRigidbody.useGravity = false;
            if (TempCharacterEntity.enabled)
                TempCharacterEntity.enabled = false;
            TempCharacterEntity.IsHidding = true;
            if (PhotonNetwork.isMasterClient || IsMine)
            {
                TempTransform.position = brGameManager.GetSpawnerPosition();
                TempTransform.rotation = brGameManager.GetSpawnerRotation();
            }
        }
        else if (brGameManager.currentState == BRState.WaitingForPlayers || isSpawned)
        {
            if (PhotonNetwork.isMasterClient && !botDeadRemoveCalled && TempCharacterEntity is BotEntity && TempCharacterEntity.IsDead)
            {
                botDeadRemoveCalled = true;
                StartCoroutine(BotDeadRemoveRoutine());
            }
            if (!TempCharacterEntity.TempRigidbody.useGravity)
                TempCharacterEntity.TempRigidbody.useGravity = true;
            if (!TempCharacterEntity.enabled)
                TempCharacterEntity.enabled = true;
            TempCharacterEntity.IsHidding = false;
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
        if (!PhotonNetwork.isMasterClient)
            return;
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameplayManager != null)
            photonView.RPC("RpcRankResult", photonView.owner, BaseNetworkGameManager.Singleton.CountAliveCharacters() + 1);
    }

    IEnumerator ShowRankResultRoutine(int rank)
    {
        yield return new WaitForSeconds(3f);
        var ui = UIBRGameplay.Singleton;
        if (ui != null)
            ui.ShowRankResult(rank);
    }
    
    public void ServerCharacterSpawn()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null)
        {
            isSpawned = true;
            photonView.RPC("RpcCharacterSpawned", PhotonTargets.All, brGameplayManager.SpawnCharacter(TempCharacterEntity) + new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-2.5f, 2.5f)));
        }
    }
    
    public void CmdCharacterSpawn()
    {
        photonView.RPC("RpcServerCharacterSpawn", PhotonTargets.MasterClient);
    }
    
    [PunRPC]
    protected void RpcServerCharacterSpawn()
    {
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null && brGameplayManager.CanSpawnCharacter(TempCharacterEntity))
            ServerCharacterSpawn();
    }

    [PunRPC]
    protected void RpcCharacterSpawned(Vector3 spawnPosition)
    {
        TempCharacterEntity.TempTransform.position = spawnPosition;
        TempCharacterEntity.TempRigidbody.useGravity = true;
        TempCharacterEntity.TempRigidbody.isKinematic = false;
    }

    [PunRPC]
    protected virtual void RpcRankResult(int rank)
    {
        if (IsMine)
            StartCoroutine(ShowRankResultRoutine(rank));
    }

    [PunRPC]
    protected virtual void RpcUpdateIsSpawned(bool isSpawned)
    {
        _isSpawned = isSpawned;
    }
}
