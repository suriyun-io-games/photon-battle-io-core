using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;

[RequireComponent(typeof(Rigidbody))]
public class CharacterEntity : BaseNetworkGameCharacter
{
    public const float DISCONNECT_WHEN_NOT_RESPAWN_DURATION = 60;
    public const byte RPC_EFFECT_DAMAGE_SPAWN = 0;
    public const byte RPC_EFFECT_DAMAGE_HIT = 1;
    public const byte RPC_EFFECT_TRAP_HIT = 2;
    public const byte RPC_EFFECT_SKILL_SPAWN = 3;
    public const byte RPC_EFFECT_SKILL_HIT = 4;

    public Transform damageLaunchTransform;
    public Transform effectTransform;
    public Transform characterModelTransform;
    public GameObject[] localPlayerObjects;
    public float jumpHeight = 2f;
    public float dashDuration = 1.5f;
    public float dashMoveSpeedMultiplier = 1.5f;
    [Header("UI")]
    public Transform hpBarContainer;
    public Image hpFillImage;
    public Text hpText;
    public Text nameText;
    public Text levelText;
    public GameObject attackSignalObject;
    public GameObject attackSignalObjectForTeamA;
    public GameObject attackSignalObjectForTeamB;
    [Header("Effect")]
    public GameObject invincibleEffect;

    #region Sync Vars
    protected int _hp;
    protected int _exp;
    protected int _level;
    protected int _statPoint;
    protected int _watchAdsCount;
    protected int _selectCharacter;
    protected int _selectHead;
    protected int _selectWeapon;
    protected int[] _selectCustomEquipments;
    protected bool _isInvincible;
    protected short _attackingActionId = -1;
    protected short _usingSkillHotkeyId = -1;
    protected CharacterStats _addStats;
    protected string _extra;
    protected int _defaultSelectWeapon;

    public virtual int hp
    {
        get { return _hp; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != hp)
            {
                _hp = value;
                photonView.RPC("RpcUpdateHp", RpcTarget.Others, value);
            }
        }
    }
    public int Hp
    {
        get { return hp; }
        set
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (value <= 0)
            {
                value = 0;
                if (!isDead)
                {
                    photonView.RPC("RpcTargetDead", photonView.Owner);
                    deathTime = Time.unscaledTime;
                    ++dieCount;
                    isDead = true;
                }
            }
            if (value > TotalHp)
                value = TotalHp;
            hp = value;
        }
    }
    public virtual int exp
    {
        get { return _exp; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != exp)
            {
                _exp = value;
                photonView.RPC("RpcUpdateExp", RpcTarget.Others, value);
            }
        }
    }
    public virtual int Exp
    {
        get { return exp; }
        set
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            var gameplayManager = GameplayManager.Singleton;
            while (true)
            {
                if (level == gameplayManager.maxLevel)
                    break;

                var currentExp = gameplayManager.GetExp(level);
                if (value < currentExp)
                    break;
                var remainExp = value - currentExp;
                value = remainExp;
                ++level;
                statPoint += gameplayManager.addingStatPoint;
            }
            exp = value;
        }
    }
    public virtual int level
    {
        get { return _level; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != level)
            {
                _level = value;
                photonView.RPC("RpcUpdateLevel", RpcTarget.Others, value);
            }
        }
    }
    public virtual int statPoint
    {
        get { return _statPoint; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != statPoint)
            {
                _statPoint = value;
                photonView.RPC("RpcUpdateStatPoint", RpcTarget.Others, value);
            }
        }
    }
    public virtual int watchAdsCount
    {
        get { return _watchAdsCount; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != watchAdsCount)
            {
                _watchAdsCount = value;
                photonView.RPC("RpcUpdateWatchAdsCount", RpcTarget.Others, value);
            }
        }
    }
    public virtual int selectCharacter
    {
        get { return _selectCharacter; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != selectCharacter)
            {
                _selectCharacter = value;
                photonView.RPC("RpcUpdateSelectCharacter", RpcTarget.All, value);
            }
        }
    }
    public virtual int selectHead
    {
        get { return _selectHead; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != selectHead)
            {
                _selectHead = value;
                photonView.RPC("RpcUpdateSelectHead", RpcTarget.All, value);
            }
        }
    }
    public virtual int selectWeapon
    {
        get { return _selectWeapon; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != selectWeapon)
            {
                _selectWeapon = value;
                photonView.RPC("RpcUpdateSelectWeapon", RpcTarget.All, value);
            }
        }
    }
    public virtual int[] selectCustomEquipments
    {
        get { return _selectCustomEquipments; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != selectCustomEquipments)
            {
                _selectCustomEquipments = value;
                photonView.RPC("RpcUpdateSelectCustomEquipments", RpcTarget.All, value);
            }
        }
    }
    public virtual bool isInvincible
    {
        get { return _isInvincible; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != isInvincible)
            {
                _isInvincible = value;
                photonView.RPC("RpcUpdateIsInvincible", RpcTarget.Others, value);
            }
        }
    }
    public virtual short attackingActionId
    {
        get { return _attackingActionId; }
        set
        {
            if (photonView.IsMine && value != attackingActionId)
            {
                _attackingActionId = value;
                photonView.RPC("RpcUpdateAttackingActionId", RpcTarget.Others, value);
            }
        }
    }
    public virtual short usingSkillHotkeyId
    {
        get { return _usingSkillHotkeyId; }
        set
        {
            if (photonView.IsMine && value != usingSkillHotkeyId)
            {
                _usingSkillHotkeyId = value;
                photonView.RPC("RpcUpdateUsingSkillHotkeyId", RpcTarget.Others, value);
            }
        }
    }
    public virtual CharacterStats addStats
    {
        get { return _addStats; }
        set
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _addStats = value;
                photonView.RPC("RpcUpdateAddStats", RpcTarget.Others, value);
            }
        }
    }
    public virtual string extra
    {
        get { return _extra; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != extra)
            {
                _extra = value;
                photonView.RPC("RpcUpdateExtra", RpcTarget.Others, value);
            }
        }
    }
    public virtual int defaultSelectWeapon
    {
        get { return _defaultSelectWeapon; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != _defaultSelectWeapon)
            {
                _defaultSelectWeapon = value;
                photonView.RPC("RpcUpdateDefaultSelectWeapon", RpcTarget.Others, value);
            }
        }
    }
    #endregion

    [HideInInspector]
    public int rank = 0;

    public override bool IsDead
    {
        get { return hp <= 0; }
    }

    public System.Action onDead;
    protected Camera targetCamera;
    protected CharacterModel characterModel;
    protected CharacterData characterData;
    protected HeadData headData;
    protected WeaponData weaponData;
    protected Dictionary<int, CustomEquipmentData> customEquipmentDict = new Dictionary<int, CustomEquipmentData>();
    protected Dictionary<int, StatusEffectEntity> appliedStatusEffects = new Dictionary<int, StatusEffectEntity>();
    protected bool isMobileInput;
    protected Vector2 inputMove;
    protected Vector2 inputDirection;
    protected bool inputAttack;
    protected bool inputJump;
    protected bool isDashing;
    protected Vector2 dashInputMove;
    protected float dashingTime;
    protected Dictionary<sbyte, SkillData> skills = new Dictionary<sbyte, SkillData>();
    protected float[] lastSkillUseTimes = new float[8];
    protected bool inputCancelUsingSkill;
    protected sbyte holdingUseSkillHotkeyId = -1;
    protected sbyte releasedUseSkillHotkeyId = -1;
    protected Vector3? previousPosition;
    protected Vector3 currentVelocity;

    public bool isReady { get; private set; }
    public bool isDead { get; private set; }
    public bool isGround { get; private set; }
    public bool isPlayingAttackAnim { get; private set; }
    public bool isPlayingUseSkillAnim { get; private set; }
    public DamageEntity attackingDamageEntity { get; private set; }
    public int attackingSpreadDamages { get; private set; }
    public float deathTime { get; private set; }
    public float invincibleTime { get; private set; }

    public Dictionary<sbyte, SkillData> Skills
    {
        get { return skills; }
    }

    private bool isHidding;
    public bool IsHidding
    {
        get { return isHidding; }
        set
        {
            isHidding = value;
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                renderer.enabled = !isHidding;
            var canvases = GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
                canvas.enabled = !isHidding;
        }
    }
    public Transform CacheTransform { get; private set; }
    public Rigidbody CacheRigidbody { get; private set; }
    public Collider CacheCollider { get; private set; }

    public virtual CharacterStats SumAddStats
    {
        get
        {
            var stats = new CharacterStats();
            stats += addStats;
            if (headData != null)
                stats += headData.stats;
            if (characterData != null)
                stats += characterData.stats;
            if (weaponData != null)
                stats += weaponData.stats;
            if (customEquipmentDict != null)
            {
                foreach (var value in customEquipmentDict.Values)
                    stats += value.stats;
            }
            if (appliedStatusEffects != null)
            {
                foreach (var value in appliedStatusEffects.Values)
                    stats += value.addStats;
            }
            return stats;
        }
    }

    public virtual int TotalHp
    {
        get
        {
            var total = GameplayManager.Singleton.minHp + SumAddStats.addHp;
            return total;
        }
    }

    public virtual int TotalAttack
    {
        get
        {
            var total = GameplayManager.Singleton.minAttack + SumAddStats.addAttack;
            return total;
        }
    }

    public virtual int TotalDefend
    {
        get
        {
            var total = GameplayManager.Singleton.minDefend + SumAddStats.addDefend;
            return total;
        }
    }

    public virtual int TotalMoveSpeed
    {
        get
        {
            var total = GameplayManager.Singleton.minMoveSpeed + SumAddStats.addMoveSpeed;
            return total;
        }
    }

    public virtual float TotalExpRate
    {
        get
        {
            var total = 1 + SumAddStats.addExpRate;
            return total;
        }
    }

    public virtual float TotalScoreRate
    {
        get
        {
            var total = 1 + SumAddStats.addScoreRate;
            return total;
        }
    }

    public virtual float TotalHpRecoveryRate
    {
        get
        {
            var total = 1 + SumAddStats.addHpRecoveryRate;
            return total;
        }
    }

    public virtual float TotalDamageRateLeechHp
    {
        get
        {
            var total = SumAddStats.addDamageRateLeechHp;
            return total;
        }
    }

    public virtual int TotalSpreadDamages
    {
        get
        {
            var total = 1 + SumAddStats.addSpreadDamages;

            var maxValue = GameplayManager.Singleton.maxSpreadDamages;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public virtual float TotalIncreaseDamageRate
    {
        get
        {
            var total = SumAddStats.increaseDamageRate;
            if (total < -1f)
                total = -0.9f;
            return total;
        }
    }

    public virtual float TotalReduceReceiveDamageRate
    {
        get
        {
            var total = SumAddStats.reduceReceiveDamageRate;
            return total;
        }
    }

    public virtual int RewardExp
    {
        get { return GameplayManager.Singleton.GetRewardExp(level); }
    }

    public virtual int KillScore
    {
        get { return GameplayManager.Singleton.GetKillScore(level); }
    }

    protected override void Init()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.Init();
        hp = 0;
        exp = 0;
        level = 1;
        statPoint = 0;
        watchAdsCount = 0;
        selectCharacter = 0;
        selectHead = 0;
        selectWeapon = 0;
        selectCustomEquipments = new int[0];
        isInvincible = false;
        addStats = new CharacterStats();
        extra = "";
    }

    protected override void Awake()
    {
        base.Awake();
        gameObject.layer = GameInstance.Singleton.characterLayer;
        CacheTransform = transform;
        CacheRigidbody = GetComponent<Rigidbody>();
        CacheCollider = GetComponent<Collider>();
        if (damageLaunchTransform == null)
            damageLaunchTransform = CacheTransform;
        if (effectTransform == null)
            effectTransform = CacheTransform;
        if (characterModelTransform == null)
            characterModelTransform = CacheTransform;
        foreach (var localPlayerObject in localPlayerObjects)
        {
            localPlayerObject.SetActive(false);
        }
        deathTime = Time.unscaledTime;
    }

    protected override void SyncData()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.SyncData();
        photonView.RPC("RpcUpdateHp", RpcTarget.Others, hp);
        photonView.RPC("RpcUpdateExp", RpcTarget.Others, exp);
        photonView.RPC("RpcUpdateLevel", RpcTarget.Others, level);
        photonView.RPC("RpcUpdateStatPoint", RpcTarget.Others, statPoint);
        photonView.RPC("RpcUpdateWatchAdsCount", RpcTarget.Others, watchAdsCount);
        photonView.RPC("RpcUpdateSelectCharacter", RpcTarget.Others, selectCharacter);
        photonView.RPC("RpcUpdateSelectHead", RpcTarget.Others, selectHead);
        photonView.RPC("RpcUpdateSelectWeapon", RpcTarget.Others, selectWeapon);
        photonView.RPC("RpcUpdateSelectCustomEquipments", RpcTarget.Others, selectCustomEquipments);
        photonView.RPC("RpcUpdateIsInvincible", RpcTarget.Others, isInvincible);
        photonView.RPC("RpcUpdateAttackingActionId", RpcTarget.Others, attackingActionId);
        photonView.RPC("RpcUpdateUsingSkillHotkeyId", RpcTarget.Others, usingSkillHotkeyId);
        photonView.RPC("RpcUpdateAddStats", RpcTarget.Others, addStats);
        photonView.RPC("RpcUpdateExtra", RpcTarget.Others, extra);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.OnPlayerEnteredRoom(newPlayer);
        photonView.RPC("RpcUpdateHp", newPlayer, hp);
        photonView.RPC("RpcUpdateExp", newPlayer, exp);
        photonView.RPC("RpcUpdateLevel", newPlayer, level);
        photonView.RPC("RpcUpdateStatPoint", newPlayer, statPoint);
        photonView.RPC("RpcUpdateWatchAdsCount", newPlayer, watchAdsCount);
        photonView.RPC("RpcUpdateSelectCharacter", newPlayer, selectCharacter);
        photonView.RPC("RpcUpdateSelectHead", newPlayer, selectHead);
        photonView.RPC("RpcUpdateSelectWeapon", newPlayer, selectWeapon);
        photonView.RPC("RpcUpdateSelectCustomEquipments", newPlayer, selectCustomEquipments);
        photonView.RPC("RpcUpdateIsInvincible", newPlayer, isInvincible);
        photonView.RPC("RpcUpdateAttackingActionId", newPlayer, attackingActionId);
        photonView.RPC("RpcUpdateUsingSkillHotkeyId", newPlayer, usingSkillHotkeyId);
        photonView.RPC("RpcUpdateAddStats", newPlayer, addStats);
        photonView.RPC("RpcUpdateExtra", newPlayer, extra);
    }

    protected override void OnStartLocalPlayer()
    {
        if (photonView.IsMine)
        {
            var followCam = FindObjectOfType<FollowCamera>();
            followCam.target = CacheTransform;
            targetCamera = followCam.GetComponent<Camera>();
            var uiGameplay = FindObjectOfType<UIGameplay>();
            if (uiGameplay != null)
                uiGameplay.FadeOut();

            foreach (var localPlayerObject in localPlayerObjects)
            {
                localPlayerObject.SetActive(true);
            }
            CmdReady();
        }
    }

    protected override void Update()
    {
        base.Update();
        if (NetworkManager != null && NetworkManager.IsMatchEnded)
            return;

        if (Hp <= 0)
        {
            if (!PhotonNetwork.IsMasterClient && photonView.IsMine && Time.unscaledTime - deathTime >= DISCONNECT_WHEN_NOT_RESPAWN_DURATION)
                GameNetworkManager.Singleton.LeaveRoom();

            if (photonView.IsMine)
            {
                attackingActionId = -1;
                usingSkillHotkeyId = -1;
            }
        }

        if (PhotonNetwork.IsMasterClient && isInvincible && Time.unscaledTime - invincibleTime >= GameplayManager.Singleton.invincibleDuration)
            isInvincible = false;
        if (invincibleEffect != null)
            invincibleEffect.SetActive(isInvincible);
        if (nameText != null)
            nameText.text = playerName;
        if (hpBarContainer != null)
            hpBarContainer.gameObject.SetActive(hp > 0);
        if (hpFillImage != null)
            hpFillImage.fillAmount = (float)hp / (float)TotalHp;
        if (hpText != null)
            hpText.text = hp + "/" + TotalHp;
        if (levelText != null)
            levelText.text = level.ToString("N0");
        UpdateAnimation();
        UpdateInput();
        // Update dash state
        if (isDashing && Time.unscaledTime - dashingTime > dashDuration)
            isDashing = false;
        // Update attack signal
        if (attackSignalObject != null)
            attackSignalObject.SetActive(isPlayingAttackAnim || isPlayingUseSkillAnim);
        // TODO: Improve team codes
        if (attackSignalObjectForTeamA != null)
            attackSignalObjectForTeamA.SetActive((isPlayingAttackAnim || isPlayingUseSkillAnim) && playerTeam == 1);
        if (attackSignalObjectForTeamB != null)
            attackSignalObjectForTeamB.SetActive((isPlayingAttackAnim || isPlayingUseSkillAnim) && playerTeam == 2);
    }

    private void FixedUpdate()
    {
        if (!previousPosition.HasValue)
            previousPosition = CacheTransform.position;
        var currentMove = CacheTransform.position - previousPosition.Value;
        currentVelocity = currentMove / Time.deltaTime;
        previousPosition = CacheTransform.position;

        if (NetworkManager != null && NetworkManager.IsMatchEnded)
            return;

        UpdateMovements();
    }

    protected virtual void UpdateInput()
    {
        if (!photonView.IsMine || Hp <= 0)
            return;

        bool canControl = true;
        var fields = FindObjectsOfType<InputField>();
        foreach (var field in fields)
        {
            if (field.isFocused)
            {
                canControl = false;
                break;
            }
        }

        isMobileInput = Application.isMobilePlatform;
#if UNITY_EDITOR
        isMobileInput = GameInstance.Singleton.showJoystickInEditor;
#endif
        InputManager.useMobileInputOnNonMobile = isMobileInput;

        var canAttack = isMobileInput || !EventSystem.current.IsPointerOverGameObject();
        // Reset input states
        inputMove = Vector2.zero;
        inputDirection = Vector2.zero;
        inputAttack = false;
        if (inputCancelUsingSkill = InputManager.GetButton("CancelUsingSkill"))
        {
            holdingUseSkillHotkeyId = -1;
            releasedUseSkillHotkeyId = -1;
        }

        if (canControl)
        {
            inputMove = new Vector2(InputManager.GetAxis("Horizontal", false), InputManager.GetAxis("Vertical", false));
            // Jump
            if (!inputJump)
                inputJump = InputManager.GetButtonDown("Jump") && isGround && !isDashing;
            // Attack, Can attack while not dashing
            if (!isDashing)
            {
                if (isMobileInput)
                {
                    inputDirection = new Vector2(InputManager.GetAxis("Mouse X", false), InputManager.GetAxis("Mouse Y", false));
                    if (canAttack)
                    {
                        inputAttack = inputDirection.sqrMagnitude != 0;
                        if (!inputAttack)
                        {
                            // Find out that player pressed on skill hotkey or not
                            for (sbyte i = 0; i < 8; ++i)
                            {
                                inputDirection = new Vector2(InputManager.GetAxis("Skill X " + i, false), InputManager.GetAxis("Skill Y " + i, false));
                                if (inputDirection.sqrMagnitude != 0 && holdingUseSkillHotkeyId < 0)
                                {
                                    // Start drag
                                    holdingUseSkillHotkeyId = i;
                                    releasedUseSkillHotkeyId = -1;
                                    break;
                                }
                                if (inputDirection.sqrMagnitude != 0 && holdingUseSkillHotkeyId == i)
                                {
                                    // Holding
                                    break;
                                }
                                if (inputDirection.sqrMagnitude == 0 && holdingUseSkillHotkeyId == i)
                                {
                                    // End drag
                                    holdingUseSkillHotkeyId = -1;
                                    releasedUseSkillHotkeyId = i;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    inputDirection = (InputManager.MousePosition() - targetCamera.WorldToScreenPoint(CacheTransform.position)).normalized;
                    if (canAttack)
                    {
                        inputAttack = InputManager.GetButton("Fire1");
                        if (!inputAttack)
                        {
                            // Find out that player pressed on skill hotkey or not
                            for (sbyte i = 0; i < 8; ++i)
                            {
                                if (InputManager.GetButton("Skill " + i) && holdingUseSkillHotkeyId < 0)
                                {
                                    // Break if use skill
                                    holdingUseSkillHotkeyId = -1;
                                    releasedUseSkillHotkeyId = i;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Dash
            if (!isDashing)
            {
                isDashing = InputManager.GetButtonDown("Dash") && isGround;
                if (isDashing)
                {
                    if (isMobileInput)
                        dashInputMove = inputMove.normalized;
                    else
                        dashInputMove = new Vector2(CacheTransform.forward.x, CacheTransform.forward.z).normalized;
                    inputAttack = false;
                    dashingTime = Time.unscaledTime;
                    CmdDash();
                }
            }
        }
    }

    protected virtual void UpdateAnimation()
    {
        if (characterModel == null)
            return;

        var animator = characterModel.TempAnimator;
        if (animator == null)
            return;

        if (Hp <= 0)
        {
            animator.SetBool("IsDead", true);
            animator.SetFloat("JumpSpeed", 0);
            animator.SetFloat("MoveSpeed", 0);
            animator.SetBool("IsGround", true);
            animator.SetBool("IsDash", false);
        }
        else
        {
            var velocity = currentVelocity;
            var xzMagnitude = new Vector3(velocity.x, 0, velocity.z).magnitude;
            var ySpeed = velocity.y;
            animator.SetBool("IsDead", false);
            animator.SetFloat("JumpSpeed", ySpeed);
            animator.SetFloat("MoveSpeed", xzMagnitude);
            animator.SetBool("IsGround", Mathf.Abs(ySpeed) < 0.5f);
            animator.SetBool("IsDash", isDashing);
        }

        animator.SetInteger("WeaponAnimId", weaponData != null ? weaponData.weaponAnimId : 0);

        animator.SetBool("IsIdle", !animator.GetBool("IsDead") && !animator.GetBool("DoAction") && animator.GetBool("IsGround"));

        if (attackingActionId >= 0 && usingSkillHotkeyId < 0 && !isPlayingAttackAnim)
            StartCoroutine(AttackRoutine());

        if (usingSkillHotkeyId >= 0 && !isPlayingUseSkillAnim)
            StartCoroutine(UseSkillRoutine());
    }

    protected virtual float GetMoveSpeed()
    {
        return TotalMoveSpeed * GameplayManager.REAL_MOVE_SPEED_RATE;
    }

    protected virtual void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0)
        {
            direction = direction.normalized;

            var targetSpeed = GetMoveSpeed() * (isDashing ? dashMoveSpeedMultiplier : 1f);
            var targetVelocity = direction * targetSpeed;

            // Apply a force that attempts to reach our target velocity
            Vector3 velocity = CacheRigidbody.velocity;
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;
            CacheRigidbody.velocity = velocity;
        }
    }

    protected virtual void UpdateMovements()
    {
        if (!photonView.IsMine || Hp <= 0)
            return;

        var moveDirection = new Vector3(inputMove.x, 0, inputMove.y);
        var dashDirection = new Vector3(dashInputMove.x, 0, dashInputMove.y);

        Move(isDashing ? dashDirection : moveDirection);
        // Turn character to move direction
        if (inputDirection.sqrMagnitude <= 0 && inputMove.sqrMagnitude > 0)
            inputDirection = inputMove;
        Rotate(isDashing ? dashInputMove : inputDirection);

        if (inputAttack && GameplayManager.Singleton.CanAttack(this))
            Attack();
        else
            StopAttack();

        if (!inputCancelUsingSkill && releasedUseSkillHotkeyId >= 0 && GameplayManager.Singleton.CanAttack(this))
        {
            UseSkill(releasedUseSkillHotkeyId);
            holdingUseSkillHotkeyId = -1;
            releasedUseSkillHotkeyId = -1;
        }

        var velocity = CacheRigidbody.velocity;
        if (isGround && inputJump)
        {
            CacheRigidbody.velocity = new Vector3(velocity.x, CalculateJumpVerticalSpeed(), velocity.z);
            isGround = false;
            inputJump = false;
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (!isGround && collision.impulse.y > 0)
            isGround = true;
    }

    protected virtual void OnCollisionStay(Collision collision)
    {
        if (!isGround && collision.impulse.y > 0)
            isGround = true;
    }

    protected float CalculateJumpVerticalSpeed()
    {
        // From the jump height and gravity we deduce the upwards speed 
        // for the character to reach at the apex.
        return Mathf.Sqrt(2f * jumpHeight * -Physics.gravity.y);
    }

    protected void Rotate(Vector2 direction)
    {
        if (direction.sqrMagnitude != 0)
        {
            int newRotation = (int)(Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y + targetCamera.transform.eulerAngles.y);
            Quaternion targetRotation = Quaternion.Euler(0, newRotation, 0);
            CacheTransform.rotation = targetRotation;
        }
    }

    public void GetDamageLaunchTransform(bool isLeftHandWeapon, out Transform launchTransform)
    {
        launchTransform = null;
        if (characterModel == null || !characterModel.TryGetDamageLaunchTransform(isLeftHandWeapon, out launchTransform))
            launchTransform = damageLaunchTransform;
    }

    protected void Attack()
    {
        if (attackingActionId < 0 && photonView.IsMine)
        {
            if (weaponData != null)
                attackingActionId = weaponData.GetRandomAttackAnimation().actionId;
            else
                attackingActionId = -1;
        }
    }

    protected void StopAttack()
    {
        if (attackingActionId >= 0 && photonView.IsMine)
            attackingActionId = -1;
    }

    public void UseSkill(sbyte hotkeyId)
    {
        SkillData skill;
        if (attackingActionId < 0 &&
            usingSkillHotkeyId < 0 &&
            photonView.IsMine && skills.TryGetValue(hotkeyId, out skill) &&
            GetSkillCoolDownCount(hotkeyId) > skill.coolDown)
        {
            lastSkillUseTimes[hotkeyId] = Time.unscaledTime;
            usingSkillHotkeyId = hotkeyId;
        }
    }

    public float GetSkillCoolDownCount(sbyte hotkeyId)
    {
        return Time.unscaledTime - lastSkillUseTimes[hotkeyId];
    }

    IEnumerator AttackRoutine()
    {
        if (!isPlayingAttackAnim &&
            Hp > 0 &&
            characterModel != null &&
            characterModel.TempAnimator != null)
        {
            isPlayingAttackAnim = true;
            AttackAnimation attackAnimation;
            if (weaponData != null && attackingActionId >= 0 && attackingActionId < 255 &&
                weaponData.AttackAnimations.TryGetValue(attackingActionId, out attackAnimation))
            {
                attackingDamageEntity = weaponData.damagePrefab;
                attackingSpreadDamages = TotalSpreadDamages;
                byte actionId = (byte)attackingActionId;
                yield return StartCoroutine(PlayAttackAnimationRoutine(attackAnimation, weaponData.attackFx, () =>
                {
                    // Launch damage entity on owning client then update on other clients
                    if (photonView.IsMine)
                        weaponData.Launch(this, actionId);
                }));
                // If player still attacking, random new attacking action id
                if (PhotonNetwork.IsMasterClient && attackingActionId >= 0 && weaponData != null)
                    attackingActionId = weaponData.GetRandomAttackAnimation().actionId;
                attackingDamageEntity = null;
                attackingSpreadDamages = 0;
            }
            isPlayingAttackAnim = false;
        }
    }

    IEnumerator UseSkillRoutine()
    {
        if (!isPlayingUseSkillAnim &&
            Hp > 0 &&
            characterModel != null &&
            characterModel.TempAnimator != null)
        {
            isPlayingUseSkillAnim = true;
            SkillData skillData;
            if (skills.TryGetValue((sbyte)usingSkillHotkeyId, out skillData))
            {
                attackingDamageEntity = skillData.damagePrefab;
                attackingSpreadDamages = skillData.TotalSpreadDamages;
                yield return StartCoroutine(PlayAttackAnimationRoutine(skillData.attackAnimation, skillData.attackFx, () =>
                {
                    // Launch damage entity on owning client then update on other clients
                    if (photonView.IsMine)
                        skillData.Launch(this);
                }));
                attackingDamageEntity = null;
                attackingSpreadDamages = 0;
            }
            usingSkillHotkeyId = -1;
            isPlayingUseSkillAnim = false;
        }
    }

    IEnumerator PlayAttackAnimationRoutine(AttackAnimation attackAnimation, AudioClip[] attackFx, System.Action onAttack)
    {
        var animator = characterModel.TempAnimator;
        if (animator != null && attackAnimation != null)
        {
            // Play attack animation
            animator.SetBool("DoAction", false);
            yield return new WaitForEndOfFrame();
            animator.SetBool("DoAction", true);
            animator.SetInteger("ActionID", attackAnimation.actionId);

            // Wait to launch damage entity
            var speed = attackAnimation.speed;
            var animationDuration = attackAnimation.animationDuration;
            var launchDuration = attackAnimation.launchDuration;
            if (launchDuration > animationDuration)
                launchDuration = animationDuration;
            yield return new WaitForSeconds(launchDuration / speed);

            onAttack.Invoke();

            // Random play shoot sounds
            if (attackFx != null && attackFx.Length > 0 && AudioManager.Singleton != null)
                AudioSource.PlayClipAtPoint(attackFx[Random.Range(0, weaponData.attackFx.Length - 1)], CacheTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);

            // Wait till animation end
            yield return new WaitForSeconds((animationDuration - launchDuration) / speed);

            // Attack animation ended
            animator.SetBool("DoAction", false);
        }
    }

    public virtual bool ReceiveDamage(CharacterEntity attacker, int damage, byte type, int dataId, byte actionId)
    {
        if (Hp <= 0 || isInvincible)
            return false;

        var gameplayManager = GameplayManager.Singleton;
        if (!gameplayManager.CanReceiveDamage(this, attacker))
            return false;

        photonView.RPC("RpcEffect", RpcTarget.All, attacker.photonView.ViewID, type, dataId, actionId);
        int reduceHp = (int)(damage + ((float)damage * TotalIncreaseDamageRate) - ((float)damage * TotalReduceReceiveDamageRate)) - TotalDefend;
        if (reduceHp < 0)
            reduceHp = 0;

        Hp -= reduceHp;
        if (attacker != null)
        {
            if (attacker.Hp > 0)
            {
                var leechHpAmount = Mathf.CeilToInt(attacker.TotalDamageRateLeechHp * reduceHp);
                attacker.Hp += leechHpAmount;
            }
            if (Hp == 0)
            {
                var statusEffects = new List<StatusEffectEntity>(appliedStatusEffects.Values);
                foreach (var statusEffect in statusEffects)
                {
                    if (statusEffect)
                        Destroy(statusEffect.gameObject);
                }
                statusEffects.Clear();
                if (onDead != null)
                    onDead.Invoke();
                attacker.KilledTarget(this);
                ++dieCount;
            }
        }
        return true;
    }

    public void KilledTarget(CharacterEntity target)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        var gameplayManager = GameplayManager.Singleton;
        var targetLevel = target.level;
        var maxLevel = gameplayManager.maxLevel;
        Exp += Mathf.CeilToInt(target.RewardExp * TotalExpRate);
        score += Mathf.CeilToInt(target.KillScore * TotalScoreRate);
        foreach (var rewardCurrency in gameplayManager.rewardCurrencies)
        {
            var currencyId = rewardCurrency.currencyId;
            var amount = rewardCurrency.amount.Calculate(targetLevel, maxLevel);
            photonView.RPC("RpcTargetRewardCurrency", photonView.Owner, currencyId, amount);
        }
        ++killCount;
        GameNetworkManager.Singleton.SendKillNotify(playerName, target.playerName, weaponData == null ? string.Empty : weaponData.GetId());
    }

    public void Heal(int amount)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (Hp <= 0)
            return;

        Hp += amount;
    }

    public virtual float GetAttackRange()
    {
        if (weaponData == null || weaponData.damagePrefab == null)
            return 0;
        return weaponData.damagePrefab.GetAttackRange();
    }

    public void ChangeWeapon(WeaponData weaponData)
    {
        if (weaponData == null)
            return;
        selectWeapon = weaponData.GetHashId();
    }

    public void UpdateCharacterModelHiddingState()
    {
        if (characterModel == null)
            return;
        var renderers = characterModel.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
            renderer.enabled = !IsHidding;
    }

    public virtual Vector3 GetSpawnPosition()
    {
        return GameplayManager.Singleton.GetCharacterSpawnPosition(this);
    }

    public virtual void OnSpawn() { }

    public void ServerInvincible()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        invincibleTime = Time.unscaledTime;
        isInvincible = true;
    }

    public void ServerSpawn(bool isWatchedAds)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (Respawn(isWatchedAds))
        {
            var gameplayManager = GameplayManager.Singleton;
            ServerInvincible();
            OnSpawn();
            var position = GetSpawnPosition();
            CacheTransform.position = position;
            photonView.RPC("RpcTargetSpawn", photonView.Owner, position.x, position.y, position.z);
            ServerRevive();
        }
    }

    public void ServerRespawn(bool isWatchedAds)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (CanRespawn(isWatchedAds))
            ServerSpawn(isWatchedAds);
    }

    public void ServerRevive()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (defaultSelectWeapon != 0)
            selectWeapon = defaultSelectWeapon;
        isPlayingAttackAnim = false;
        isDead = false;
        Hp = TotalHp;
        holdingUseSkillHotkeyId = -1;
        releasedUseSkillHotkeyId = -1;
    }

    public void CmdInit(int selectHead, int selectCharacter, int selectWeapon, int[] selectCustomEquipments, string extra)
    {
        photonView.RPC("RpcServerInit", RpcTarget.MasterClient, selectHead, selectCharacter, selectWeapon, selectCustomEquipments, extra);
    }

    [PunRPC]
    protected void RpcServerInit(int selectHead, int selectCharacter, int selectWeapon, int[] selectCustomEquipments, string extra)
    {
        var alreadyInit = false;
        var networkManager = BaseNetworkGameManager.Singleton;
        if (networkManager != null)
        {
            networkManager.RegisterCharacter(this);
            var gameRule = networkManager.gameRule;
            if (gameRule != null && gameRule is IONetworkGameRule)
            {
                var ioGameRule = gameRule as IONetworkGameRule;
                ioGameRule.NewPlayer(this, selectHead, selectCharacter, selectWeapon, selectCustomEquipments, extra);
                alreadyInit = true;
            }
        }
        if (!alreadyInit)
        {
            this.selectHead = selectHead;
            this.selectCharacter = selectCharacter;
            this.selectWeapon = selectWeapon;
            this.selectCustomEquipments = selectCustomEquipments;
            this.extra = extra;
        }
        Hp = TotalHp;
    }

    public void CmdReady()
    {
        photonView.RPC("RpcServerReady", RpcTarget.MasterClient);
    }

    [PunRPC]
    protected void RpcServerReady()
    {
        if (!isReady)
        {
            ServerSpawn(false);
            isReady = true;
        }
    }

    public void CmdRespawn(bool isWatchedAds)
    {
        photonView.RPC("RpcServerRespawn", RpcTarget.MasterClient, isWatchedAds);
    }

    [PunRPC]
    protected void RpcServerRespawn(bool isWatchedAds)
    {
        ServerRespawn(isWatchedAds);
    }

    public void CmdAddAttribute(string name)
    {
        photonView.RPC("RpcServerAddAttribute", RpcTarget.MasterClient, name);
    }

    public void CmdDash()
    {
        // Play dash animation on other clients
        photonView.RPC("RpcDash", RpcTarget.Others);
    }

    [PunRPC]
    protected void RpcServerAddAttribute(string name)
    {
        if (statPoint > 0)
        {
            var gameplay = GameplayManager.Singleton;
            CharacterAttributes attribute;
            if (gameplay.attributes.TryGetValue(name, out attribute))
            {
                addStats += attribute.stats;
                var changingWeapon = attribute.changingWeapon;
                if (changingWeapon != null)
                    ChangeWeapon(changingWeapon);
                --statPoint;
            }
        }
    }

    [PunRPC]
    protected void RpcApplyStatusEffect(int dataId)
    {
        // Destroy applied status effect, because it cannot be stacked
        RemoveAppliedStatusEffect(dataId);
        StatusEffectEntity statusEffect;
        if (GameInstance.StatusEffects.TryGetValue(dataId, out statusEffect) && Random.value <= statusEffect.applyRate)
        {
            // Found prefab, instantiates to character
            statusEffect = Instantiate(statusEffect, transform.position, transform.rotation, transform);
            // Just in case the game object might be not activated by default
            statusEffect.gameObject.SetActive(true);
            // Set applying character
            statusEffect.Applied(this);
            // Add to applied status effects
            appliedStatusEffects[dataId] = statusEffect;
        }
    }

    public void RemoveAppliedStatusEffect(int dataId)
    {
        StatusEffectEntity statusEffect;
        if (appliedStatusEffects.TryGetValue(dataId, out statusEffect))
        {
            appliedStatusEffects.Remove(dataId);
            if (statusEffect)
                Destroy(statusEffect.gameObject);
        }
    }

    [PunRPC]
    protected void RpcEffect(int triggerViewId, byte effectType, int dataId, byte actionId)
    {
        var triggerObject = PhotonView.Find(triggerViewId);

        if (triggerObject != null)
        {
            if (effectType == RPC_EFFECT_DAMAGE_SPAWN || effectType == RPC_EFFECT_DAMAGE_HIT)
            {
                WeaponData weaponData;
                if (GameInstance.Weapons.TryGetValue(dataId, out weaponData))
                {
                    var damagePrefab = weaponData.damagePrefab;
                    if (weaponData.AttackAnimations.ContainsKey(actionId) &&
                        weaponData.AttackAnimations[actionId].damagePrefab != null)
                        damagePrefab = weaponData.AttackAnimations[actionId].damagePrefab;
                    if (damagePrefab)
                    {
                        switch (effectType)
                        {
                            case RPC_EFFECT_DAMAGE_SPAWN:
                                EffectEntity.PlayEffect(damagePrefab.spawnEffectPrefab, effectTransform);
                                break;
                            case RPC_EFFECT_DAMAGE_HIT:
                                EffectEntity.PlayEffect(damagePrefab.hitEffectPrefab, effectTransform);
                                break;
                        }
                    }
                }
            }
            else if (effectType == RPC_EFFECT_TRAP_HIT)
            {
                var trap = triggerObject.GetComponent<TrapEntity>();
                if (trap != null)
                    EffectEntity.PlayEffect(trap.hitEffectPrefab, effectTransform);
            }
            else if (effectType == RPC_EFFECT_SKILL_SPAWN || effectType == RPC_EFFECT_SKILL_HIT)
            {
                SkillData skillData;
                if (GameInstance.Skills.TryGetValue(dataId, out skillData) &&
                    skillData.damagePrefab != null)
                {
                    var damagePrefab = skillData.damagePrefab;
                    switch (effectType)
                    {
                        case RPC_EFFECT_SKILL_SPAWN:
                            EffectEntity.PlayEffect(damagePrefab.spawnEffectPrefab, effectTransform);
                            break;
                        case RPC_EFFECT_SKILL_HIT:
                            EffectEntity.PlayEffect(damagePrefab.hitEffectPrefab, effectTransform);
                            break;
                    }
                }
            }
        }
    }

    [PunRPC]
    protected void RpcDash()
    {
        // Just play dash animation on another clients
        isDashing = true;
        dashingTime = Time.unscaledTime;
    }

    [PunRPC]
    protected void RpcTargetDead()
    {
        deathTime = Time.unscaledTime;
    }

    [PunRPC]
    protected void RpcTargetSpawn(float x, float y, float z)
    {
        transform.position = new Vector3(x, y, z);
    }

    [PunRPC]
    protected void RpcTargetRewardCurrency(string currencyId, int amount)
    {
        MonetizationManager.Save.AddCurrency(currencyId, amount);
    }

    protected void UpdateSkills()
    {
        skills.Clear();
        if (characterData != null && characterData.skills != null)
        {
            foreach (var skill in characterData.skills)
            {
                skills[skill.hotkeyId] = skill;
            }
        }
        if (headData != null && headData.skills != null)
        {
            foreach (var skill in headData.skills)
            {
                skills[skill.hotkeyId] = skill;
            }
        }
        if (weaponData != null && weaponData.skills != null)
        {
            foreach (var skill in weaponData.skills)
            {
                skills[skill.hotkeyId] = skill;
            }
        }
        if (customEquipmentDict.Count > 0)
        {
            foreach (var customEquipment in customEquipmentDict.Values)
            {
                foreach (var skill in customEquipment.skills)
                {
                    skills[skill.hotkeyId] = skill;
                }
            }
        }
    }

    #region Update RPCs
    [PunRPC]
    protected virtual void RpcUpdateHp(int hp)
    {
        _hp = hp;
    }
    [PunRPC]
    protected virtual void RpcUpdateExp(int exp)
    {
        _exp = exp;
    }
    [PunRPC]
    protected virtual void RpcUpdateLevel(int level)
    {
        _level = level;
    }
    [PunRPC]
    protected virtual void RpcUpdateStatPoint(int statPoint)
    {
        _statPoint = statPoint;
    }
    [PunRPC]
    protected virtual void RpcUpdateWatchAdsCount(int watchAdsCount)
    {
        _watchAdsCount = watchAdsCount;
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectCharacter(int selectCharacter)
    {
        _selectCharacter = selectCharacter;

        if (characterModel != null)
            Destroy(characterModel.gameObject);
        characterData = GameInstance.GetCharacter(selectCharacter);
        if (characterData == null || characterData.modelObject == null)
            return;
        characterModel = Instantiate(characterData.modelObject, characterModelTransform);
        characterModel.transform.localPosition = Vector3.zero;
        characterModel.transform.localEulerAngles = Vector3.zero;
        characterModel.transform.localScale = Vector3.one;
        if (headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        if (weaponData != null)
            characterModel.SetWeaponModel(weaponData.rightHandObject, weaponData.leftHandObject, weaponData.shieldObject);
        if (customEquipmentDict != null)
        {
            characterModel.ClearCustomModels();
            foreach (var value in customEquipmentDict.Values)
            {
                characterModel.SetCustomModel(value.containerIndex, value.modelObject);
            }
        }
        characterModel.gameObject.SetActive(true);
        UpdateCharacterModelHiddingState();
        UpdateSkills();
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectHead(int selectHead)
    {
        _selectHead = selectHead;
        headData = GameInstance.GetHead(selectHead);
        if (characterModel != null && headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        UpdateCharacterModelHiddingState();
        UpdateSkills();
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectWeapon(int selectWeapon)
    {
        _selectWeapon = selectWeapon;
        if (PhotonNetwork.IsMasterClient)
        {
            if (defaultSelectWeapon == 0)
                defaultSelectWeapon = selectWeapon;
        }
        weaponData = GameInstance.GetWeapon(selectWeapon);
        if (characterModel != null && weaponData != null)
            characterModel.SetWeaponModel(weaponData.rightHandObject, weaponData.leftHandObject, weaponData.shieldObject);
        UpdateCharacterModelHiddingState();
        UpdateSkills();
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectCustomEquipments(int[] selectCustomEquipments)
    {
        _selectCustomEquipments = selectCustomEquipments;
        if (characterModel != null)
            characterModel.ClearCustomModels();
        customEquipmentDict.Clear();
        if (_selectCustomEquipments != null)
        {
            for (var i = 0; i < _selectCustomEquipments.Length; ++i)
            {
                var customEquipmentData = GameInstance.GetCustomEquipment(_selectCustomEquipments[i]);
                if (customEquipmentData != null &&
                    !customEquipmentDict.ContainsKey(customEquipmentData.containerIndex))
                {
                    customEquipmentDict[customEquipmentData.containerIndex] = customEquipmentData;
                    if (characterModel != null)
                        characterModel.SetCustomModel(customEquipmentData.containerIndex, customEquipmentData.modelObject);
                }
            }
        }
        UpdateCharacterModelHiddingState();
        UpdateSkills();
    }
    [PunRPC]
    protected virtual void RpcUpdateIsInvincible(bool isInvincible)
    {
        _isInvincible = isInvincible;
    }
    [PunRPC]
    protected virtual void RpcUpdateAttackingActionId(short attackingActionId)
    {
        _attackingActionId = attackingActionId;
    }
    [PunRPC]
    protected virtual void RpcUpdateUsingSkillHotkeyId(short usingSkillHotkeyId)
    {
        _usingSkillHotkeyId = usingSkillHotkeyId;
    }
    [PunRPC]
    protected virtual void RpcUpdateAddStats(CharacterStats addStats)
    {
        _addStats = addStats;
    }
    [PunRPC]
    protected virtual void RpcUpdateExtra(string extra)
    {
        _extra = extra;
    }
    [PunRPC]
    protected virtual void RpcUpdateDefaultSelectWeapon(int defaultSelectWeapon)
    {
        _defaultSelectWeapon = defaultSelectWeapon;
    }
    #endregion
}
