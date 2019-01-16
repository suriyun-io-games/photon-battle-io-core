using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotEntity : CharacterEntity
{
    public enum Characteristic
    {
        Normal,
        NoneAttack
    }
    protected string botPlayerName;
    public override string playerName
    {
        get { return botPlayerName; }
        set
        {
            if (PhotonNetwork.isMasterClient)
            {
                botPlayerName = value;
                photonView.RPC("RpcUpdateBotName", PhotonTargets.Others, value);
            }
        }
    }
    protected PunTeams.Team botPlayerTeam;
    public override PunTeams.Team playerTeam
    {
        get { return botPlayerTeam; }
        set
        {
            if (PhotonNetwork.isMasterClient)
            {
                botPlayerTeam = value;
                photonView.RPC("RpcUpdateBotTeam", PhotonTargets.Others, value);
            }
        }
    }
    public const float ReachedTargetDistance = 0.1f;
    public float updateMovementDuration = 2;
    public float attackDuration = 0;
    public float randomMoveDistance = 5f;
    public float detectEnemyDistance = 2f;
    public float turnSpeed = 5f;
    public Characteristic characteristic;
    public CharacterStats startAddStats;
    [HideInInspector, System.NonSerialized]
    public bool isFixRandomMoveAroundPoint;
    [HideInInspector, System.NonSerialized]
    public Vector3 fixRandomMoveAroundPoint;
    [HideInInspector, System.NonSerialized]
    public float fixRandomMoveAroundDistance;
    private Vector3 targetPosition;
    private float lastUpdateMovementTime;
    private float lastAttackTime;
    private bool isWallHit;

    protected override void Awake()
    {
        base.Awake();
        if (PhotonNetwork.isMasterClient)
        {
            ServerSpawn(false);
            lastUpdateMovementTime = Time.unscaledTime - updateMovementDuration;
            lastAttackTime = Time.unscaledTime - attackDuration;
        }
    }

    protected override void SyncData()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        base.SyncData();
        photonView.RPC("RpcUpdateBotName", PhotonTargets.Others, botPlayerName);
        photonView.RPC("RpcUpdateBotTeam", PhotonTargets.Others, botPlayerTeam);
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        base.OnPhotonPlayerConnected(newPlayer);
        photonView.RPC("RpcUpdateBotName", newPlayer, botPlayerName);
        photonView.RPC("RpcUpdateBotTeam", newPlayer, botPlayerTeam);
    }

    // Override to do nothing
    protected override void SetLocalPlayer()
    {
    }

    // Override to do nothing
    protected override void OnStartLocalPlayer()
    {
    }

    // Override to do nothing
    protected override void UpdateInput()
    {
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
        // Bots will update target movement when reached move target / hitting the walls / it's time
        var isReachedTarget = IsReachedTargetPosition();
        if (isReachedTarget || isWallHit || Time.unscaledTime - lastUpdateMovementTime >= updateMovementDuration)
        {
            lastUpdateMovementTime = Time.unscaledTime;
            if (isFixRandomMoveAroundPoint)
            {
                targetPosition = new Vector3(
                    fixRandomMoveAroundPoint.x + Random.Range(-fixRandomMoveAroundDistance, fixRandomMoveAroundDistance),
                    0,
                    fixRandomMoveAroundPoint.z + Random.Range(-fixRandomMoveAroundDistance, fixRandomMoveAroundDistance));
            }
            else
            {
                targetPosition = new Vector3(
                    TempTransform.position.x + Random.Range(-randomMoveDistance, randomMoveDistance),
                    0,
                    TempTransform.position.z + Random.Range(-randomMoveDistance, randomMoveDistance));
            }
            isWallHit = false;
        }

        var rotatePosition = targetPosition;
        CharacterEntity enemy;
        if (FindEnemy(out enemy) && characteristic == Characteristic.Normal && Time.unscaledTime - lastAttackTime >= attackDuration)
        {
            lastAttackTime = Time.unscaledTime;
            if (attackingActionId < 0)
                attackingActionId = weaponData.GetRandomAttackAnimation().actionId;
            rotatePosition = enemy.TempTransform.position;
        }
        else if (attackingActionId >= 0)
            attackingActionId = -1;

        // Gets a vector that points from the player's position to the target's.
        var heading = targetPosition - TempTransform.position;
        var distance = heading.magnitude;
        var direction = heading / distance; // This is now the normalized direction.
        Move(direction);
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
        var gameplayManager = GameplayManager.Singleton;
        var colliders = Physics.OverlapSphere(TempTransform.position, detectEnemyDistance);
        foreach (var collider in colliders)
        {
            var character = collider.GetComponent<CharacterEntity>();
            if (character != null && character != this && character.Hp > 0 && gameplayManager.CanReceiveDamage(character, this))
            {
                enemy = character;
                return true;
            }
        }
        return false;
    }

    protected override void OnCollisionStay(Collision collision)
    {
        base.OnCollisionStay(collision);
        if (collision.collider.tag == "Wall")
            isWallHit = true;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        addStats += startAddStats;
        Hp = TotalHp;
    }

    [PunRPC]
    protected void RpcUpdateBotName(string name)
    {
        botPlayerName = name;
    }

    [PunRPC]
    protected void RpcUpdateBotTeam(PunTeams.Team team)
    {
        botPlayerTeam = team;
    }
}
