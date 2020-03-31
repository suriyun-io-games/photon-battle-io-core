﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Linq;

public class BotEntity : CharacterEntity
{
    public enum Characteristic
    {
        Aggressive,
        NoneAttack
    }
    protected string botPlayerName;
    public override string playerName
    {
        get { return botPlayerName; }
        set
        {
            if (PhotonNetwork.IsMasterClient)
            {
                botPlayerName = value;
                photonView.RPC("RpcUpdateBotName", RpcTarget.Others, value);
            }
        }
    }
    protected PunTeams.Team botPlayerTeam;
    public override PunTeams.Team playerTeam
    {
        get { return botPlayerTeam; }
        set
        {
            if (PhotonNetwork.IsMasterClient)
            {
                botPlayerTeam = value;
                photonView.RPC("RpcUpdateBotTeam", RpcTarget.Others, value);
            }
        }
    }
    public const float ReachedTargetDistance = 0.1f;
    [Header("Bot configs")]
    public float minimumAttackRange = 5f;
    public float updateMovementDuration = 2f;
    public float attackDuration = 0f;
    public float useSkillDuration = 3f;
    public float forgetEnemyDuration = 3f;
    public float randomDashDurationMin = 3f;
    public float randomDashDurationMax = 5f;
    [FormerlySerializedAs("randomMoveDistance")]
    public float randomMoveDistanceMin = 5f;
    public float randomMoveDistanceMax = 5f;
    [FormerlySerializedAs("detectEnemyDistance")]
    public float detectEnemyDistanceMin = 2f;
    public float detectEnemyDistanceMax = 2f;
    public float turnSpeed = 5f;
    public int[] navMeshAreas = new int[] { 0, 1, 2 };
    public Characteristic characteristic;
    public CharacterStats startAddStats;
    [HideInInspector, System.NonSerialized]
    public bool isFixRandomMoveAroundPoint;
    [HideInInspector, System.NonSerialized]
    public Vector3 fixRandomMoveAroundPoint;
    [HideInInspector, System.NonSerialized]
    public float fixRandomMoveAroundDistance;
    private float lastUpdateMovementTime;
    private float lastAttackTime;
    private float lastUseSkillTime;
    private float randomDashDuration;
    private CharacterEntity enemy;
    private Vector3 dashDirection;
    private Queue<Vector3> navPaths;
    private Vector3 targetPosition;
    private Vector3 lookingPosition;

    protected override void Awake()
    {
        base.Awake();
        if (PhotonNetwork.IsMasterClient)
        {
            ServerSpawn(false);
            lastUpdateMovementTime = Time.unscaledTime - updateMovementDuration;
            lastAttackTime = Time.unscaledTime - attackDuration;
            randomDashDuration = dashDuration + Random.Range(randomDashDurationMin, randomDashDurationMax);
        }
    }

    protected override void SyncData()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.SyncData();
        photonView.RPC("RpcUpdateBotName", RpcTarget.Others, botPlayerName);
        photonView.RPC("RpcUpdateBotTeam", RpcTarget.Others, botPlayerTeam);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.OnPlayerEnteredRoom(newPlayer);
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

    public int RandomPosNeg()
    {
        return Random.value > 0.5f ? -1 : 1;
    }

    protected override void UpdateMovements()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (Hp <= 0)
        {
            ServerRespawn(false);
            CacheRigidbody.velocity = new Vector3(0, CacheRigidbody.velocity.y, 0);
            return;
        }

        // Bots will update target movement when reached move target / hitting the walls / it's time
        var isReachedTarget = IsReachedTargetPosition();
        if (isReachedTarget || Time.unscaledTime - lastUpdateMovementTime >= updateMovementDuration)
        {
            lastUpdateMovementTime = Time.unscaledTime;
            if (enemy != null)
            {
                GetMovePaths(new Vector3(
                    enemy.CacheTransform.position.x + (Random.Range(detectEnemyDistanceMin, detectEnemyDistanceMax) * RandomPosNeg()),
                    0,
                    enemy.CacheTransform.position.z + (Random.Range(detectEnemyDistanceMin, detectEnemyDistanceMax) * RandomPosNeg())));
            }
            else if (isFixRandomMoveAroundPoint)
            {
                GetMovePaths(new Vector3(
                    fixRandomMoveAroundPoint.x + Random.Range(-1f, 1f) * fixRandomMoveAroundDistance,
                    0,
                    fixRandomMoveAroundPoint.z + Random.Range(-1f, 1f) * fixRandomMoveAroundDistance));
            }
            else
            {
                GetMovePaths(new Vector3(
                    CacheTransform.position.x + (Random.Range(randomMoveDistanceMin, randomMoveDistanceMax) * RandomPosNeg()),
                    0,
                    CacheTransform.position.z + (Random.Range(randomMoveDistanceMin, randomMoveDistanceMax) * RandomPosNeg())));
            }
        }

        lookingPosition = targetPosition;
        if (enemy == null || enemy.IsDead || Time.unscaledTime - lastAttackTime >= forgetEnemyDuration)
        {
            enemy = null;
            // Try find enemy. If found move to target in next frame
            switch (characteristic)
            {
                case Characteristic.Aggressive:
                    if (FindEnemy(out enemy))
                    {
                        lastAttackTime = Time.unscaledTime;
                        lastUpdateMovementTime = Time.unscaledTime - updateMovementDuration;
                    }
                    break;
            }
        }
        else
        {
            // Set target rotation to enemy position
            lookingPosition = enemy.CacheTransform.position;
        }

        attackingActionId = -1;
        if (enemy != null)
        {
            switch (characteristic)
            {
                case Characteristic.Aggressive:
                    if (Time.unscaledTime - lastAttackTime >= attackDuration &&
                    Vector3.Distance(enemy.CacheTransform.position, CacheTransform.position) < GetAttackRange())
                    {
                        // Attack when nearby enemy
                        sbyte usingSkillHotkeyId;
                        if (RandomUseSkill(out usingSkillHotkeyId))
                            this.usingSkillHotkeyId = usingSkillHotkeyId;
                        else
                            attackingActionId = weaponData.GetRandomAttackAnimation().actionId;
                        lastAttackTime = Time.unscaledTime;
                    }
                    break;
            }
        }

        // Dashing
        if (Time.unscaledTime - dashingTime >= randomDashDuration && !isDashing)
        {
            randomDashDuration = dashDuration + Random.Range(randomDashDurationMin, randomDashDurationMax);
            dashDirection = CacheTransform.forward;
            dashDirection.y = 0;
            dashDirection.Normalize();
            isDashing = true;
            dashingTime = Time.unscaledTime;
            CmdDash();
        }

        // Gets a vector that points from the player's position to the target's.
        isReachedTarget = IsReachedTargetPosition();
        if (!isReachedTarget)
        {
            Move(isDashing ? dashDirection : (targetPosition - CacheTransform.position).normalized);
        }

        if (isReachedTarget)
        {
            targetPosition = CacheTransform.position + (CacheTransform.forward * ReachedTargetDistance / 2f);
            CacheRigidbody.velocity = new Vector3(0, CacheRigidbody.velocity.y, 0);
            if (navPaths.Count > 0)
                targetPosition = navPaths.Dequeue();
        }
        // Rotate to target
        var rotateHeading = lookingPosition - CacheTransform.position;
        var targetRotation = Quaternion.LookRotation(rotateHeading);
        CacheTransform.rotation = Quaternion.Lerp(CacheTransform.rotation, Quaternion.Euler(0, targetRotation.eulerAngles.y, 0), Time.deltaTime * turnSpeed);
        UpdateStatPoint();
    }

    private void GetMovePaths(Vector3 position)
    {
        int areaMask = 0;
        if (navMeshAreas.Length == 0)
        {
            areaMask = NavMesh.AllAreas;
        }
        else
        {
            for (int i = 0; i < navMeshAreas.Length; ++i)
            {
                areaMask = areaMask | 1 << navMeshAreas[i];
            }
        }
        NavMeshPath navPath = new NavMeshPath();
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(position, out navHit, 5f, areaMask) &&
            NavMesh.CalculatePath(CacheTransform.position, navHit.position, areaMask, navPath))
        {
            navPaths = new Queue<Vector3>(navPath.corners);
            // Dequeue first path it's not require for future movement
            navPaths.Dequeue();
        }
        // Initial queue
        if (navPaths == null)
            navPaths = new Queue<Vector3>();
        // Set first target position immediately
        if (navPaths.Count > 0)
            targetPosition = navPaths.Dequeue();
        else
            targetPosition = position;
    }

    private void UpdateStatPoint()
    {
        if (statPoint <= 0)
            return;
        var dict = new Dictionary<CharacterAttributes, int>();
        var list = GameplayManager.Singleton.attributes.Values.ToList();
        foreach (var entry in list)
        {
            dict.Add(entry, entry.randomWeight);
        }
        CmdAddAttribute(WeightedRandomizer.From(dict).TakeOne().name);
    }

    private bool RandomUseSkill(out sbyte hotkeyId)
    {
        hotkeyId = -1;
        if (Time.unscaledTime - lastUseSkillTime < useSkillDuration)
            return false;
        if (Skills == null || Skills.Count == 0)
            return false;
        hotkeyId = Skills.Keys.Skip(Random.Range(0, Skills.Count)).Take(1).First();
        SkillData skill;
        if (Skills.TryGetValue(hotkeyId, out skill) &&
            GetSkillCoolDownCount(hotkeyId) > skill.coolDown)
        {
            lastUseSkillTime = Time.unscaledTime;
            return true;
        }
        hotkeyId = -1;
        return false;
    }

    private bool IsReachedTargetPosition()
    {
        if (enemy != null)
            return Vector3.Distance(targetPosition, CacheTransform.position) < Mathf.Min(enemy.CacheCollider.bounds.size.x, enemy.CacheCollider.bounds.size.z);
        return Vector3.Distance(targetPosition, CacheTransform.position) < ReachedTargetDistance;
    }

    private bool FindEnemy(out CharacterEntity enemy)
    {
        enemy = null;
        var gameplayManager = GameplayManager.Singleton;
        var colliders = Physics.OverlapSphere(CacheTransform.position, detectEnemyDistanceMin);
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

        if (collision.collider.CompareTag("Wall"))
        {
            // Find another position to move in next frame
            lastUpdateMovementTime = Time.unscaledTime - updateMovementDuration;
        }
    }

    public override bool ReceiveDamage(CharacterEntity attacker, int damage, byte type, int dataId, byte actionId)
    {
        if (base.ReceiveDamage(attacker, damage, type, dataId, actionId))
        {
            switch (characteristic)
            {
                case Characteristic.Aggressive:
                    if (enemy == null)
                        enemy = attacker;
                    else if (Random.value > 0.5f)
                        enemy = attacker;
                    break;
            }
            return true;
        }
        return false;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        addStats += startAddStats;
        Hp = TotalHp;
    }

    public override float GetAttackRange()
    {
        float range = base.GetAttackRange();
        if (range < minimumAttackRange)
            return minimumAttackRange;
        return range;
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
