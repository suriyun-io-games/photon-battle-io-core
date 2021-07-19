using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterMovement))]
public class CharacterEntity : BaseNetworkGameCharacter
{
    public const float DISCONNECT_WHEN_NOT_RESPAWN_DURATION = 60;

    public enum ViewMode
    {
        TopDown,
        ThirdPerson,
    }

    [System.Serializable]
    public class ViewModeSettings
    {
        public Vector3 targetOffsets = Vector3.zero;
        public float zoomDistance = 3f;
        public float minZoomDistance = 3f;
        public float maxZoomDistance = 3f;
        public float xRotation = 45f;
        public float minXRotation = 45f;
        public float maxXRotation = 45f;
        public float yRotation = 0f;
        public float fov = 60f;
        public float nearClipPlane = 0.3f;
        public float farClipPlane = 1000f;
    }

    public ViewMode viewMode;
    public ViewModeSettings topDownViewModeSettings;
    public ViewModeSettings thirdPersionViewModeSettings;
    public bool doNotLockCursor;
    public Transform damageLaunchTransform;
    public Transform effectTransform;
    public Transform characterModelTransform;
    public GameObject[] localPlayerObjects;
    public float dashDuration = 1.5f;
    public float dashMoveSpeedMultiplier = 1.5f;
    public float returnToMoveDirectionDelay = 1f;
    public float endActionDelay = 0.75f;
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
    [Header("Online data")]
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
    protected Vector3 _aimPosition;

    public virtual int hp
    {
        get { return _hp; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != hp)
            {
                _hp = value;
                photonView.OthersRPC(RpcUpdateHp, value);
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
                    photonView.TargetRPC(RpcTargetDead, photonView.Owner);
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
                photonView.OthersRPC(RpcUpdateExp, value);
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
                photonView.OthersRPC(RpcUpdateLevel, value);
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
                photonView.OthersRPC(RpcUpdateStatPoint, value);
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
                photonView.OthersRPC(RpcUpdateWatchAdsCount, value);
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
                photonView.AllRPC(RpcUpdateSelectCharacter, value);
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
                photonView.AllRPC(RpcUpdateSelectHead, value);
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
                photonView.AllRPC(RpcUpdateSelectWeapon, value);
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
                photonView.AllRPC(RpcUpdateSelectCustomEquipments, value);
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
                photonView.OthersRPC(RpcUpdateIsInvincible, value);
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
                photonView.OthersRPC(RpcUpdateAttackingActionId, value);
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
                photonView.OthersRPC(RpcUpdateUsingSkillHotkeyId, value);
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
                photonView.OthersRPC(RpcUpdateAddStats, value);
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
                photonView.OthersRPC(RpcUpdateExtra, value);
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
                photonView.OthersRPC(RpcUpdateDefaultSelectWeapon, value);
            }
        }
    }
    public virtual Vector3 aimPosition
    {
        get { return _aimPosition; }
        set
        {
            if (photonView.IsMine && value != aimPosition)
            {
                _aimPosition = value;
                photonView.OthersRPC(RpcUpdateAimPosition, value);
            }
        }
    }
    #endregion

    public override bool IsDead
    {
        get { return hp <= 0; }
    }

    public override bool IsBot
    {
        get { return false; }
    }

    public System.Action onDead;
    protected ViewMode dirtyViewMode;
    protected Camera targetCamera;
    protected Vector3 cameraForward;
    protected Vector3 cameraRight;
    protected FollowCameraControls followCameraControls;
    protected CharacterModel characterModel;
    protected CharacterData characterData;
    protected HeadData headData;
    protected WeaponData weaponData;
    protected Dictionary<int, CustomEquipmentData> customEquipmentDict = new Dictionary<int, CustomEquipmentData>();
    protected Dictionary<int, StatusEffectEntity> appliedStatusEffects = new Dictionary<int, StatusEffectEntity>();
    protected bool isMobileInput;
    protected Vector3 inputMove;
    protected Vector3 inputDirection;
    protected bool inputAttack;
    protected bool inputJump;
    protected bool isDashing;
    protected Vector3 dashInputMove;
    protected float dashingTime;
    protected Dictionary<sbyte, SkillData> skills = new Dictionary<sbyte, SkillData>();
    protected float[] lastSkillUseTimes = new float[8];
    protected bool inputCancelUsingSkill;
    protected sbyte holdingUseSkillHotkeyId = -1;
    protected sbyte releasedUseSkillHotkeyId = -1;
    protected Vector3? previousPosition;
    protected Vector3 currentVelocity;
    protected float lastActionTime;
    protected Coroutine endActionDelayCoroutine;

    public bool isReady { get; private set; }
    public bool isDead { get; private set; }
    public bool isGrounded { get { return CacheCharacterMovement.IsGrounded; } }
    public bool isPlayingAttackAnim { get; private set; }
    public bool isPlayingUseSkillAnim { get; private set; }
    public DamageEntity attackingDamageEntity { get; private set; }
    public int attackingSpreadDamages { get; private set; }
    public float deathTime { get; private set; }
    public float invincibleTime { get; private set; }
    public bool currentActionIsForLeftHand { get; protected set; }

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
            var projectors = GetComponentsInChildren<Projector>();
            foreach (var projector in projectors)
                projector.enabled = !isHidding;
        }
    }
    public Transform CacheTransform { get; private set; }
    public Rigidbody CacheRigidbody { get; private set; }
    public CharacterMovement CacheCharacterMovement { get; private set; }

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
        CacheRigidbody.useGravity = false;
        CacheCharacterMovement = gameObject.GetOrAddComponent<CharacterMovement>();
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
        photonView.OthersRPC(RpcUpdateHp, hp);
        photonView.OthersRPC(RpcUpdateExp, exp);
        photonView.OthersRPC(RpcUpdateLevel, level);
        photonView.OthersRPC(RpcUpdateStatPoint, statPoint);
        photonView.OthersRPC(RpcUpdateWatchAdsCount, watchAdsCount);
        photonView.OthersRPC(RpcUpdateSelectCharacter, selectCharacter);
        photonView.OthersRPC(RpcUpdateSelectHead, selectHead);
        photonView.OthersRPC(RpcUpdateSelectWeapon, selectWeapon);
        photonView.OthersRPC(RpcUpdateSelectCustomEquipments, selectCustomEquipments);
        photonView.OthersRPC(RpcUpdateIsInvincible, isInvincible);
        photonView.OthersRPC(RpcUpdateAttackingActionId, attackingActionId);
        photonView.OthersRPC(RpcUpdateUsingSkillHotkeyId, usingSkillHotkeyId);
        photonView.OthersRPC(RpcUpdateAddStats, addStats);
        photonView.OthersRPC(RpcUpdateExtra, extra);
        photonView.OthersRPC(RpcUpdateAimPosition, aimPosition);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.OnPlayerEnteredRoom(newPlayer);
        photonView.TargetRPC(RpcUpdateHp, newPlayer, hp);
        photonView.TargetRPC(RpcUpdateExp, newPlayer, exp);
        photonView.TargetRPC(RpcUpdateLevel, newPlayer, level);
        photonView.TargetRPC(RpcUpdateStatPoint, newPlayer, statPoint);
        photonView.TargetRPC(RpcUpdateWatchAdsCount, newPlayer, watchAdsCount);
        photonView.TargetRPC(RpcUpdateSelectCharacter, newPlayer, selectCharacter);
        photonView.TargetRPC(RpcUpdateSelectHead, newPlayer, selectHead);
        photonView.TargetRPC(RpcUpdateSelectWeapon, newPlayer, selectWeapon);
        photonView.TargetRPC(RpcUpdateSelectCustomEquipments, newPlayer, selectCustomEquipments);
        photonView.TargetRPC(RpcUpdateIsInvincible, newPlayer, isInvincible);
        photonView.TargetRPC(RpcUpdateAttackingActionId, newPlayer, attackingActionId);
        photonView.TargetRPC(RpcUpdateUsingSkillHotkeyId, newPlayer, usingSkillHotkeyId);
        photonView.TargetRPC(RpcUpdateAddStats, newPlayer, addStats);
        photonView.TargetRPC(RpcUpdateExtra, newPlayer, extra);
        photonView.TargetRPC(RpcUpdateAimPosition, newPlayer, aimPosition);
    }

    protected override void OnStartLocalPlayer()
    {
        if (photonView.IsMine)
        {
            followCameraControls = FindObjectOfType<FollowCameraControls>();
            followCameraControls.target = CacheTransform;
            targetCamera = followCameraControls.CacheCamera;

            foreach (var localPlayerObject in localPlayerObjects)
            {
                localPlayerObject.SetActive(true);
            }

            StartCoroutine(DelayReady());
        }
    }

    IEnumerator DelayReady()
    {
        yield return new WaitForSeconds(0.5f);
        // Add some delay before ready to make sure that it can receive team and game rule
        var uiGameplay = FindObjectOfType<UIGameplay>();
        if (uiGameplay != null)
            uiGameplay.FadeOut();
        CmdReady();
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
        UpdateViewMode();
        UpdateAimPosition();
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
        if (!photonView.IsMine)
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
        inputMove = Vector3.zero;
        inputDirection = Vector3.zero;
        inputAttack = false;

        if (inputCancelUsingSkill = InputManager.GetButton("CancelUsingSkill"))
        {
            holdingUseSkillHotkeyId = -1;
            releasedUseSkillHotkeyId = -1;
        }

        if (canControl)
        {
            cameraForward = followCameraControls.CacheCameraTransform.forward;
            cameraForward.y = 0;
            cameraForward = cameraForward.normalized;
            cameraRight = followCameraControls.CacheCameraTransform.right;
            cameraRight.y = 0;
            cameraRight = cameraRight.normalized;
            inputMove = Vector3.zero;
            if (!IsDead)
            {
                inputMove += cameraForward * InputManager.GetAxis("Vertical", false);
                inputMove += cameraRight * InputManager.GetAxis("Horizontal", false);
            }

            // Jump
            if (!IsDead && !inputJump)
                inputJump = InputManager.GetButtonDown("Jump") && isGrounded && !isDashing;

            if (!isDashing)
            {
                UpdateInputDirection_TopDown(canAttack);
                UpdateInputDirection_ThirdPerson(canAttack);
                if (!IsDead)
                    isDashing = InputManager.GetButtonDown("Dash") && isGrounded;
                if (isDashing)
                {
                    if (isMobileInput)
                        dashInputMove = inputMove.normalized;
                    else
                        dashInputMove = new Vector3(CacheTransform.forward.x, 0f, CacheTransform.forward.z).normalized;
                    inputAttack = false;
                    dashingTime = Time.unscaledTime;
                    CmdDash();
                }
            }
        }
    }

    protected virtual void UpdateInputDirection_TopDown(bool canAttack)
    {
        if (viewMode != ViewMode.TopDown)
            return;
        doNotLockCursor = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        followCameraControls.updateRotation = false;
        followCameraControls.updateZoom = true;
        if (isMobileInput)
        {
            inputDirection = Vector3.zero;
            inputDirection += InputManager.GetAxis("Mouse Y", false) * cameraForward;
            inputDirection += InputManager.GetAxis("Mouse X", false) * cameraRight;
            if (canAttack)
            {
                inputAttack = inputDirection.magnitude != 0;
                if (!inputAttack)
                {
                    // Find out that player pressed on skill hotkey or not
                    for (sbyte i = 0; i < 8; ++i)
                    {
                        inputDirection = Vector3.zero;
                        inputDirection += InputManager.GetAxis("Skill Y " + i, false) * cameraForward;
                        inputDirection += InputManager.GetAxis("Skill X " + i, false) * cameraRight;
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
            inputDirection = new Vector3(inputDirection.x, 0, inputDirection.y);
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

    protected virtual void UpdateInputDirection_ThirdPerson(bool canAttack)
    {
        if (viewMode != ViewMode.ThirdPerson)
            return;
        if (isMobileInput || doNotLockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (isMobileInput)
        {
            followCameraControls.updateRotation = InputManager.GetButton("CameraRotate");
            followCameraControls.updateZoom = true;
            inputDirection = Vector3.zero;
            inputDirection += InputManager.GetAxis("Mouse Y", false) * cameraForward;
            inputDirection += InputManager.GetAxis("Mouse X", false) * cameraRight;
            if (canAttack)
            {
                inputAttack = InputManager.GetButton("Fire1");
                if (!inputAttack)
                {
                    // Find out that player pressed on skill hotkey or not
                    for (sbyte i = 0; i < 8; ++i)
                    {
                        inputDirection = Vector3.zero;
                        inputDirection += InputManager.GetAxis("Skill Y " + i, false) * cameraForward;
                        inputDirection += InputManager.GetAxis("Skill X " + i, false) * cameraRight;
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
            followCameraControls.updateRotation = true;
            followCameraControls.updateZoom = true;
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
        if (inputAttack || releasedUseSkillHotkeyId >= 0)
            lastActionTime = Time.unscaledTime;
    }

    protected virtual void UpdateViewMode(bool force = false)
    {
        if (!photonView.IsMine)
            return;

        if (force || dirtyViewMode != viewMode)
        {
            dirtyViewMode = viewMode;
            ViewModeSettings settings = viewMode == ViewMode.ThirdPerson ? thirdPersionViewModeSettings : topDownViewModeSettings;
            followCameraControls.limitXRotation = true;
            followCameraControls.limitYRotation = false;
            followCameraControls.limitZoomDistance = true;
            followCameraControls.targetOffset = settings.targetOffsets;
            followCameraControls.zoomDistance = settings.zoomDistance;
            followCameraControls.minZoomDistance = settings.minZoomDistance;
            followCameraControls.maxZoomDistance = settings.maxZoomDistance;
            followCameraControls.xRotation = settings.xRotation;
            followCameraControls.minXRotation = settings.minXRotation;
            followCameraControls.maxXRotation = settings.maxXRotation;
            followCameraControls.yRotation = settings.yRotation;
            targetCamera.fieldOfView = settings.fov;
            targetCamera.nearClipPlane = settings.nearClipPlane;
            targetCamera.farClipPlane = settings.farClipPlane;
        }
    }

    protected virtual void UpdateAimPosition()
    {
        if (!photonView.IsMine || !weaponData)
            return;

        float attackDist = weaponData.damagePrefab.GetAttackRange();
        switch (viewMode)
        {
            case ViewMode.TopDown:
                // Update aim position
                currentActionIsForLeftHand = CurrentActionIsForLeftHand();
                Transform launchTransform;
                GetDamageLaunchTransform(currentActionIsForLeftHand, out launchTransform);
                aimPosition = launchTransform.position + (CacheTransform.forward * attackDist);
                break;
            case ViewMode.ThirdPerson:
                float distanceToCharacter = Vector3.Distance(CacheTransform.position, followCameraControls.CacheCameraTransform.position);
                float distanceToTarget = attackDist;
                Vector3 lookAtCharacterPosition = targetCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToCharacter));
                Vector3 lookAtTargetPosition = targetCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToTarget));
                aimPosition = lookAtTargetPosition;
                RaycastHit[] hits = Physics.RaycastAll(lookAtCharacterPosition, (lookAtTargetPosition - lookAtCharacterPosition).normalized, attackDist);
                for (int i = 0; i < hits.Length; ++i)
                {
                    if (hits[i].transform.root != transform.root)
                        aimPosition = hits[i].point;
                }
                break;
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

    protected virtual bool CurrentActionIsForLeftHand()
    {
        if (usingSkillHotkeyId >= 0)
        {
            SkillData skillData;
            if (skills.TryGetValue((sbyte)usingSkillHotkeyId, out skillData))
                return skillData.attackAnimation.isAnimationForLeftHandWeapon;
        }
        else if (attackingActionId >= 0)
        {
            AttackAnimation attackAnimation;
            if (weaponData.AttackAnimations.TryGetValue(attackingActionId, out attackAnimation))
                return attackAnimation.isAnimationForLeftHandWeapon;
        }
        return false;
    }

    protected virtual void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude > 1)
            direction = direction.normalized;
        direction.y = 0;

        var targetSpeed = GetMoveSpeed() * (isDashing ? dashMoveSpeedMultiplier : 1f);
        CacheCharacterMovement.UpdateMovement(Time.deltaTime, targetSpeed, direction, inputJump);
    }

    protected virtual void UpdateMovements()
    {
        if (!photonView.IsMine)
            return;

        var moveDirection = inputMove;
        var dashDirection = dashInputMove;

        Move(isDashing ? dashDirection : moveDirection);
        // Turn character to move direction
        if (inputDirection.magnitude <= 0 && inputMove.magnitude > 0 || viewMode == ViewMode.ThirdPerson)
            inputDirection = inputMove;
        if (characterModel && characterModel.TempAnimator && (characterModel.TempAnimator.GetBool("DoAction") || Time.unscaledTime - lastActionTime <= returnToMoveDirectionDelay) && viewMode == ViewMode.ThirdPerson)
            inputDirection = cameraForward;
        if (!IsDead)
            Rotate(isDashing ? dashInputMove : inputDirection);

        if (!IsDead)
        {
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
        }

        inputJump = false;
    }

    protected void Rotate(Vector3 direction)
    {
        if (direction.sqrMagnitude != 0)
            CacheTransform.rotation = Quaternion.LookRotation(direction);
    }

    public void GetDamageLaunchTransform(bool isLeftHandWeapon, out Transform launchTransform)
    {
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
                    weaponData.Launch(this, aimPosition, actionId);
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
                    skillData.Launch(this, aimPosition);
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
            if (endActionDelayCoroutine != null)
                StopCoroutine(endActionDelayCoroutine);
            // Play attack animation
            characterModel.TempAnimator.SetBool("DoAction", true);
            characterModel.TempAnimator.SetInteger("ActionID", attackAnimation.actionId);
            characterModel.TempAnimator.Play(0, 1, 0);

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
            endActionDelayCoroutine = StartCoroutine(DelayEndAction(endActionDelay));
        }
    }

    IEnumerator DelayEndAction(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        characterModel.TempAnimator.SetBool("DoAction", false);
    }

    public virtual bool ReceiveDamage(CharacterEntity attacker, int damage)
    {
        if (Hp <= 0 || isInvincible)
            return false;

        if (!GameplayManager.Singleton.CanReceiveDamage(this, attacker))
            return false;

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
            photonView.TargetRPC(RpcTargetRewardCurrency, photonView.Owner, currencyId, amount);
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
            ServerInvincible();
            OnSpawn();
            var position = GetSpawnPosition();
            CacheTransform.position = position;
            photonView.TargetRPC(RpcTargetSpawn, photonView.Owner, position.x, position.y, position.z);
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
        photonView.MasterRPC(RpcServerInit, selectHead, selectCharacter, selectWeapon, selectCustomEquipments, extra);
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
        photonView.MasterRPC(RpcServerReady);
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
        photonView.MasterRPC(RpcServerRespawn, isWatchedAds);
    }

    [PunRPC]
    protected void RpcServerRespawn(bool isWatchedAds)
    {
        ServerRespawn(isWatchedAds);
    }

    public void CmdAddAttribute(string name)
    {
        photonView.MasterRPC(RpcServerAddAttribute, name);
    }

    public void CmdDash()
    {
        // Play dash animation on other clients
        photonView.OthersRPC(RpcDash);
    }

    [PunRPC]
    protected void RpcServerAddAttribute(string name)
    {
        if (statPoint > 0)
        {
            CharacterAttributes attribute;
            if (GameplayManager.Singleton.attributes.TryGetValue(name, out attribute))
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
    public void RpcApplyStatusEffect(int dataId, int applierId)
    {
        // Destroy applied status effect, because it cannot be stacked
        RemoveAppliedStatusEffect(dataId);
        StatusEffectEntity statusEffect;
        if (GameInstance.StatusEffects.TryGetValue(dataId, out statusEffect) && Random.value <= statusEffect.applyRate)
        {
            // Find applier
            CharacterEntity applier = null;
            PhotonView applierView = PhotonView.Find(applierId);
            if (applierView != null)
                applier = applierView.GetComponent<CharacterEntity>();
            // Found prefab, instantiates to character
            statusEffect = Instantiate(statusEffect, transform.position, transform.rotation, transform);
            // Just in case the game object might be not activated by default
            statusEffect.gameObject.SetActive(true);
            // Set applying character
            statusEffect.Applied(this, applier);
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
    public void RpcTargetSpawn(float x, float y, float z)
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
    [PunRPC]
    protected virtual void RpcUpdateAimPosition(Vector3 aimPosition)
    {
        _aimPosition = aimPosition;
    }
    #endregion
}
