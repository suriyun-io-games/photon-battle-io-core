using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterEntity : CharacterEntity
{
    public enum Characteristic
    {
        Normal,
        NoneAttack
    }
    protected string monsterPlayerName;
    public override string playerName
    {
        get { return monsterPlayerName; }
        set
        {
            if (PhotonNetwork.isMasterClient)
            {
                monsterPlayerName = value;
                photonView.RPC("RpcUpdateMonsterName", PhotonTargets.Others, value);
            }
        }
    }
    public const float ReachedTargetDistance = 0.1f;
    [Header("Monster config set here")]
    public float wanderDistanceAroundSpawnPosition = 1f;
    public float updateWanderDuration = 2f;
    public float attackDuration = 2f;
    public float respawnDuration = 5f;
    public float detectEnemyDistance = 2f;
    public float turnSpeed = 5f;
    public Characteristic characteristic;
    public string monsterName;
    public int monsterLevel;
    public int monsterRewardExp;
    public int monsterKillScore;
    public CharacterModel monsterCharacterModel;
    public WeaponData monsterWeaponData;
    public CharacterStats monsterStats;

    private Vector3 targetPosition;
    private float lastUpdateWanderTime;
    private float lastAttackTime;
    private Vector3 spawnPosition;

    public override int Exp
    {
        get { return exp; }
        set { }
    }

    public override CharacterStats SumAddStats
    {
        get { return monsterStats; }
    }

    public override int TotalHp
    {
        get { return SumAddStats.addHp; }
    }

    public override int TotalAttack
    {
        get { return SumAddStats.addAttack; }
    }

    public override int TotalDefend
    {
        get { return SumAddStats.addDefend; }
    }

    public override int TotalMoveSpeed
    {
        get { return SumAddStats.addMoveSpeed; }
    }

    public override float TotalExpRate
    {
        get { return 0; }
    }

    public override float TotalScoreRate
    {
        get { return 0; }
    }

    public override int TotalSpreadDamages
    {
        get { return SumAddStats.addSpreadDamages; }
    }

    public override int RewardExp
    {
        get { return monsterRewardExp; }
    }

    public override int KillScore
    {
        get { return monsterKillScore; }
    }

    protected override void Awake()
    {
        base.Awake();
        characterModel = monsterCharacterModel;
        weaponData = monsterWeaponData;
        spawnPosition = TempTransform.position;

        if (PhotonNetwork.isMasterClient)
        {
            playerName = monsterName;
            level = monsterLevel;
            lastUpdateWanderTime = Time.unscaledTime - updateWanderDuration;
            lastAttackTime = Time.unscaledTime - attackDuration;
            ServerSpawn(false);
        }
    }

    protected override void SyncData()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        base.SyncData();
        photonView.RPC("RpcUpdateMonsterName", PhotonTargets.Others, monsterPlayerName);
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        base.OnPhotonPlayerConnected(newPlayer);
        photonView.RPC("RpcUpdateMonsterName", newPlayer, monsterPlayerName);
    }

    protected override void SetLocalPlayer()
    {
        // Override base function to changes functionality, to do nothing
    }
    
    protected override void UpdateInput()
    {
        // Override base function to changes functionality, to do nothing
    }

    protected override void OnStartLocalPlayer()
    {
        // Override base function to changes functionality, to do nothing
    }

    [PunRPC]
    protected override void RpcUpdateSelectCharacter(string value)
    {
        // Override base function to changes functionality, to do nothing
    }

    [PunRPC]
    protected override void RpcUpdateSelectHead(string value)
    {
        // Override base function to changes functionality, to do nothing
    }

    [PunRPC]
    protected override void RpcUpdateSelectWeapon(string value)
    {
        // Override base function to changes functionality, to do nothing
    }

    protected override void UpdateMovements()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        if (Hp <= 0)
        {
            ServerRespawn(false);
            return;
        }

        // Monster will update target movement when reached move target / hitting the walls / it's time
        if (Time.unscaledTime - lastUpdateWanderTime >= updateWanderDuration)
        {
            lastUpdateWanderTime = Time.unscaledTime;
            targetPosition = new Vector3(
                spawnPosition.x + Random.Range(-wanderDistanceAroundSpawnPosition, wanderDistanceAroundSpawnPosition),
                0,
                spawnPosition.z + Random.Range(-wanderDistanceAroundSpawnPosition, wanderDistanceAroundSpawnPosition));
        }

        var rotatePosition = targetPosition;
        CharacterEntity enemy;
        if (FindEnemy(out enemy))
        {
            if (characteristic == Characteristic.Normal)
            {
                if (Time.unscaledTime - lastAttackTime >= attackDuration)
                {
                    if (attackingActionId < 0)
                        attackingActionId = weaponData.GetRandomAttackAnimation().actionId;
                    lastAttackTime = Time.unscaledTime;
                }
                else if (attackingActionId >= 0)
                    attackingActionId = -1;
            }
            else if (attackingActionId >= 0)
                attackingActionId = -1;
            rotatePosition = enemy.TempTransform.position;
        }
        else if (attackingActionId >= 0)
            attackingActionId = -1;

        // Gets a vector that points from the player's position to the target's.
        if (!IsReachedTargetPosition())
            Move((targetPosition - TempTransform.position).normalized);
        if (IsReachedTargetPosition())
        {
            targetPosition = TempTransform.position + (TempTransform.forward * ReachedTargetDistance / 2f);
            TempRigidbody.velocity = new Vector3(0, TempRigidbody.velocity.y, 0);
        }
        // Rotate to target
        var rotateHeading = rotatePosition - TempTransform.position;
        var targetRotation = Quaternion.LookRotation(rotateHeading);
        TempTransform.rotation = Quaternion.Lerp(TempTransform.rotation, Quaternion.Euler(0, targetRotation.eulerAngles.y, 0), Time.deltaTime * turnSpeed);
    }

    private bool IsReachedTargetPosition()
    {
        return Vector3.Distance(targetPosition, TempTransform.position) < ReachedTargetDistance;
    }

    private bool FindEnemy(out CharacterEntity enemy)
    {
        enemy = null;
        var colliders = Physics.OverlapSphere(TempTransform.position, detectEnemyDistance);
        foreach (var collider in colliders)
        {
            var character = collider.GetComponent<CharacterEntity>();
            if (character != null && character != this && character.Hp > 0)
            {
                enemy = character;
                return true;
            }
        }
        return false;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        weaponData = monsterWeaponData;
        level = monsterLevel;
    }

    public override bool CanRespawn(params object[] extraParams)
    {
        return Time.unscaledTime - deathTime >= respawnDuration;
    }

    public override Vector3 GetSpawnPosition()
    {
        return spawnPosition;
    }

    [PunRPC]
    protected void RpcUpdateMonsterName(string name)
    {
        monsterPlayerName = name;
    }
}
