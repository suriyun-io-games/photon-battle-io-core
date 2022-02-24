using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(CharacterAction))]
[RequireComponent(typeof(SyncHpRpcComponent))]
[RequireComponent(typeof(SyncExpRpcComponent))]
[RequireComponent(typeof(SyncLevelRpcComponent))]
[RequireComponent(typeof(SyncStatPointRpcComponent))]
[RequireComponent(typeof(SyncWatchAdsCountRpcComponent))]
[RequireComponent(typeof(SyncSelectCharacterRpcComponent))]
[RequireComponent(typeof(SyncSelectHeadRpcComponent))]
[RequireComponent(typeof(SyncSelectWeaponRpcComponent))]
[RequireComponent(typeof(SyncSelectCustomEquipmentsRpcComponent))]
[RequireComponent(typeof(SyncIsInvincibleRpcComponent))]
[RequireComponent(typeof(SyncAttributeAmountsRpcComponent))]
[RequireComponent(typeof(SyncExtraRpcComponent))]
[RequireComponent(typeof(SyncDefaultSelectWeaponRpcComponent))]
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
    public float blockMoveSpeedMultiplier = 0.75f;
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
    private SyncHpRpcComponent syncHp = null;
    public int Hp
    {
        get { return syncHp.Value; }
        set
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (value <= 0)
            {
                value = 0;
                if (!IsDeadMarked)
                {
                    photonView.TargetRPC(RpcTargetDead, photonView.Owner);
                    DeathTime = Time.unscaledTime;
                    ++syncDieCount.Value;
                    IsDeadMarked = true;
                }
            }

            if (value > TotalHp)
                value = TotalHp;

            syncHp.Value = value;
        }
    }

    private SyncExpRpcComponent syncExp = null;
    public virtual int Exp
    {
        get { return syncExp.Value; }
        set
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            var gameplayManager = GameplayManager.Singleton;
            while (true)
            {
                if (Level == gameplayManager.maxLevel)
                    break;

                var currentExp = gameplayManager.GetExp(Level);
                if (value < currentExp)
                    break;
                var remainExp = value - currentExp;
                value = remainExp;
                ++Level;
                StatPoint += gameplayManager.addingStatPoint;
            }

            syncExp.Value = value;
        }
    }

    private SyncLevelRpcComponent syncLevel = null;
    public int Level { get { return syncLevel.Value; } set { syncLevel.Value = value; } }

    private SyncStatPointRpcComponent syncStatPoint = null;
    public int StatPoint { get { return syncStatPoint.Value; } set { syncStatPoint.Value = value; } }

    private SyncWatchAdsCountRpcComponent syncWatchAdsCount = null;
    public byte WatchAdsCount { get { return syncWatchAdsCount.Value; } set { syncWatchAdsCount.Value = value; } }

    private SyncSelectCharacterRpcComponent syncSelectCharacter = null;
    public int SelectCharacter { get { return syncSelectCharacter.Value; } set { syncSelectCharacter.Value = value; } }

    private SyncSelectHeadRpcComponent syncSelectHead = null;
    public int SelectHead { get { return syncSelectHead.Value; } set { syncSelectHead.Value = value; } }

    private SyncSelectWeaponRpcComponent syncSelectWeapon = null;
    public int SelectWeapon { get { return syncSelectWeapon.Value; } set { syncSelectWeapon.Value = value; } }

    private SyncSelectCustomEquipmentsRpcComponent syncSelectCustomEquipments = null;
    public int[] SelectCustomEquipments { get { return syncSelectCustomEquipments.Value; } set { syncSelectCustomEquipments.Value = value; } }

    private SyncIsInvincibleRpcComponent syncIsInvincible = null;
    public bool IsInvincible { get { return syncIsInvincible.Value; } set { syncIsInvincible.Value = value; } }

    private SyncAttributeAmountsRpcComponent syncAttributeAmounts = null;
    public AttributeAmounts AttributeAmounts { get { return syncAttributeAmounts.Value; } set { syncAttributeAmounts.Value = value; } }

    private SyncExtraRpcComponent syncExtra = null;
    public string Extra { get { return syncExtra.Value; } set { syncExtra.Value = value; } }

    private SyncDefaultSelectWeaponRpcComponent syncDefaultSelectWeapon = null;
    public virtual int DefaultSelectWeapon { get { return syncDefaultSelectWeapon.Value; } set { syncDefaultSelectWeapon.Value = value; } }

    public virtual bool IsBlocking
    {
        get { return CacheCharacterAction.IsBlocking; }
        set { CacheCharacterAction.IsBlocking = value; }
    }
    public virtual short AttackingActionId
    {
        get { return CacheCharacterAction.AttackingActionId; }
        set { CacheCharacterAction.AttackingActionId = value; }
    }
    public virtual short UsingSkillHotkeyId
    {
        get { return CacheCharacterAction.UsingSkillHotkeyId; }
        set { CacheCharacterAction.UsingSkillHotkeyId = value; }
    }
    public virtual Vector3 AimPosition
    {
        get { return CacheCharacterAction.AimPosition; }
        set { CacheCharacterAction.AimPosition = value; }
    }
    #endregion

    public override bool IsDead
    {
        get { return Hp <= 0; }
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

    public bool IsReady { get; protected set; }
    public bool IsDeadMarked { get; protected set; }
    public bool IsGrounded { get { return CacheCharacterMovement.IsGrounded; } }
    public bool IsPlayingAttackAnim { get; protected set; }
    public bool IsPlayingUseSkillAnim { get; protected set; }
    public DamageEntity AttackingDamageEntity { get; protected set; }
    public int AttackingSpreadDamages { get; protected set; }
    public bool IsDashing { get; protected set; }
    public float DeathTime { get; protected set; }
    public float InvincibleTime { get; protected set; }

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
    public CharacterMovement CacheCharacterMovement { get; private set; }
    public CharacterAction CacheCharacterAction { get; private set; }

    protected bool refreshingSumAddStats = true;
    protected CharacterStats sumAddStats = new CharacterStats();
    public virtual CharacterStats SumAddStats
    {
        get
        {
            if (refreshingSumAddStats)
            {
                var addStats = new CharacterStats();
                if (headData != null)
                    addStats += headData.stats;
                if (characterData != null)
                    addStats += characterData.stats;
                if (weaponData != null)
                    addStats += weaponData.stats;
                if (customEquipmentDict != null)
                {
                    foreach (var value in customEquipmentDict.Values)
                    {
                        addStats += value.stats;
                    }
                }
                if (AttributeAmounts.Dict != null)
                {
                    foreach (var kv in AttributeAmounts.Dict)
                    {
                        CharacterAttributes attribute;
                        if (GameplayManager.Singleton.Attributes.TryGetValue(kv.Key, out attribute))
                            addStats += attribute.stats * kv.Value;
                    }
                }
                if (appliedStatusEffects != null)
                {
                    foreach (var value in appliedStatusEffects.Values)
                        addStats += value.addStats;
                }
                sumAddStats = addStats;
                refreshingSumAddStats = false;
            }
            return sumAddStats;
        }
    }

    public virtual int TotalHp
    {
        get
        {
            var total = GameplayManager.Singleton.baseHp + SumAddStats.addHp;
            return total;
        }
    }

    public virtual int TotalAttack
    {
        get
        {
            var total = GameplayManager.Singleton.baseAttack + SumAddStats.addAttack;
            return total;
        }
    }

    public virtual int TotalDefend
    {
        get
        {
            var total = GameplayManager.Singleton.baseDefend + SumAddStats.addDefend;
            return total;
        }
    }

    public virtual int TotalMoveSpeed
    {
        get
        {
            var total = GameplayManager.Singleton.baseMoveSpeed + SumAddStats.addMoveSpeed;
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

    public virtual float TotalBlockReduceDamageRate
    {
        get
        {
            var total = GameplayManager.Singleton.baseBlockReduceDamageRate + SumAddStats.addBlockReduceDamageRate;

            var maxValue = GameplayManager.Singleton.maxBlockReduceDamageRate;
            if (total < maxValue)
                return total;
            else
                return maxValue;
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
        get { return GameplayManager.Singleton.GetRewardExp(Level); }
    }

    public virtual int KillScore
    {
        get { return GameplayManager.Singleton.GetKillScore(Level); }
    }

    protected override void Init()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.Init();
        Hp = 0;
        Exp = 0;
        Level = 1;
        StatPoint = 0;
        WatchAdsCount = 0;
        SelectCharacter = 0;
        SelectHead = 0;
        SelectWeapon = 0;
        SelectCustomEquipments = new int[0];
        IsInvincible = false;
        AttributeAmounts = new AttributeAmounts(0);
        Extra = string.Empty;
    }

    protected override void Awake()
    {
        base.Awake();
        gameObject.layer = GameInstance.Singleton.characterLayer;
        CacheTransform = transform;
        CacheCharacterMovement = gameObject.GetOrAddComponent<CharacterMovement>();
        CacheCharacterAction = gameObject.GetOrAddComponent<CharacterAction>();
        if (!photonView.ObservedComponents.Contains(CacheCharacterAction))
            photonView.ObservedComponents.Add(CacheCharacterAction);
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
        DeathTime = Time.unscaledTime;
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
            if (!PhotonNetwork.IsMasterClient && photonView.IsMine && Time.unscaledTime - DeathTime >= DISCONNECT_WHEN_NOT_RESPAWN_DURATION)
                GameNetworkManager.Singleton.LeaveRoom();

            if (photonView.IsMine)
            {
                AttackingActionId = -1;
                UsingSkillHotkeyId = -1;
                IsBlocking = false;
            }
        }

        if (PhotonNetwork.IsMasterClient && IsInvincible && Time.unscaledTime - InvincibleTime >= GameplayManager.Singleton.invincibleDuration)
            IsInvincible = false;
        if (invincibleEffect != null)
            invincibleEffect.SetActive(IsInvincible);
        if (nameText != null)
            nameText.text = PlayerName;
        if (hpBarContainer != null)
            hpBarContainer.gameObject.SetActive(Hp > 0);
        if (hpFillImage != null)
            hpFillImage.fillAmount = (float)Hp / (float)TotalHp;
        if (hpText != null)
            hpText.text = Hp + "/" + TotalHp;
        if (levelText != null)
            levelText.text = Level.ToString("N0");
        UpdateViewMode();
        UpdateAimPosition();
        UpdateAnimation();
        UpdateInput();
        // Update dash state
        if (IsDashing && Time.unscaledTime - dashingTime > dashDuration)
            IsDashing = false;
        // Update attack signal
        if (attackSignalObject != null)
            attackSignalObject.SetActive(IsPlayingAttackAnim || IsPlayingUseSkillAnim);
        // TODO: Improve team codes
        if (attackSignalObjectForTeamA != null)
            attackSignalObjectForTeamA.SetActive((IsPlayingAttackAnim || IsPlayingUseSkillAnim) && PlayerTeam == 1);
        if (attackSignalObjectForTeamB != null)
            attackSignalObjectForTeamB.SetActive((IsPlayingAttackAnim || IsPlayingUseSkillAnim) && PlayerTeam == 2);
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

            // Bloacking
            IsBlocking = !IsDead && !IsDashing && AttackingActionId < 0 && UsingSkillHotkeyId < 0 && IsGrounded && InputManager.GetButton("Block");

            // Jump
            if (!IsDead && !IsBlocking && !inputJump)
                inputJump = InputManager.GetButtonDown("Jump") && IsGrounded && !IsDashing;

            if (!IsBlocking && !IsDashing)
            {
                UpdateInputDirection_TopDown(canAttack);
                UpdateInputDirection_ThirdPerson(canAttack);
                if (!IsDead)
                    IsDashing = InputManager.GetButtonDown("Dash") && IsGrounded;
                if (IsDashing)
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
                Transform launchTransform;
                GetDamageLaunchTransform(CurrentActionIsForLeftHand(), out launchTransform);
                AimPosition = launchTransform.position + (CacheTransform.forward * attackDist);
                break;
            case ViewMode.ThirdPerson:
                float distanceToCharacter = Vector3.Distance(CacheTransform.position, followCameraControls.CacheCameraTransform.position);
                float distanceToTarget = attackDist;
                Vector3 lookAtCharacterPosition = targetCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToCharacter));
                Vector3 lookAtTargetPosition = targetCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToTarget));
                AimPosition = lookAtTargetPosition;
                RaycastHit[] hits = Physics.RaycastAll(lookAtCharacterPosition, (lookAtTargetPosition - lookAtCharacterPosition).normalized, attackDist);
                for (int i = 0; i < hits.Length; ++i)
                {
                    if (hits[i].transform.root != transform.root)
                        AimPosition = hits[i].point;
                }
                break;
        }
    }

    protected virtual void UpdateAnimation()
    {
        if (characterModel == null)
            return;

        var animator = characterModel.CacheAnimator;
        if (animator == null)
            return;

        if (Hp <= 0)
        {
            animator.SetBool("IsDead", true);
            animator.SetFloat("JumpSpeed", 0);
            animator.SetFloat("MoveSpeed", 0);
            animator.SetBool("IsGround", true);
            animator.SetBool("IsDash", false);
            animator.SetBool("IsBlock", false);
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
            animator.SetBool("IsDash", IsDashing);
            animator.SetBool("IsBlock", IsBlocking);
        }

        animator.SetInteger("WeaponAnimId", weaponData != null ? weaponData.weaponAnimId : 0);
        animator.SetBool("IsIdle", !animator.GetBool("IsDead") && !animator.GetBool("DoAction") && animator.GetBool("IsGround"));

        if (AttackingActionId >= 0 && UsingSkillHotkeyId < 0 && !IsPlayingAttackAnim)
            StartCoroutine(AttackRoutine());

        if (UsingSkillHotkeyId >= 0 && !IsPlayingUseSkillAnim)
            StartCoroutine(UseSkillRoutine());
    }

    protected virtual float GetMoveSpeed()
    {
        return TotalMoveSpeed * GameplayManager.REAL_MOVE_SPEED_RATE;
    }

    protected virtual bool CurrentActionIsForLeftHand()
    {
        if (UsingSkillHotkeyId >= 0)
        {
            SkillData skillData;
            if (skills.TryGetValue((sbyte)UsingSkillHotkeyId, out skillData))
                return skillData.attackAnimation.isAnimationForLeftHandWeapon;
        }
        else if (AttackingActionId >= 0)
        {
            AttackAnimation attackAnimation;
            if (weaponData.AttackAnimations.TryGetValue(AttackingActionId, out attackAnimation))
                return attackAnimation.isAnimationForLeftHandWeapon;
        }
        return false;
    }

    protected virtual void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude > 1)
            direction = direction.normalized;
        direction.y = 0;

        var targetSpeed = GetMoveSpeed() * (IsBlocking ? blockMoveSpeedMultiplier : (IsDashing ? dashMoveSpeedMultiplier : 1f));
        CacheCharacterMovement.UpdateMovement(Time.deltaTime, targetSpeed, direction, inputJump);
    }

    protected virtual void UpdateMovements()
    {
        if (!photonView.IsMine)
            return;

        var moveDirection = inputMove;
        var dashDirection = dashInputMove;

        Move(IsDashing ? dashDirection : moveDirection);
        // Turn character to move direction
        if (inputDirection.magnitude <= 0 && inputMove.magnitude > 0 || viewMode == ViewMode.ThirdPerson)
            inputDirection = inputMove;
        if (characterModel && characterModel.CacheAnimator && (characterModel.CacheAnimator.GetBool("DoAction") || Time.unscaledTime - lastActionTime <= returnToMoveDirectionDelay) && viewMode == ViewMode.ThirdPerson)
            inputDirection = cameraForward;
        if (!IsDead)
            Rotate(IsDashing ? dashInputMove : inputDirection);

        if (!IsDead && !IsBlocking)
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
        if (IsPlayingAttackAnim || IsBlocking)
            return;

        if (AttackingActionId < 0 && photonView.IsMine)
        {
            if (weaponData != null)
                AttackingActionId = weaponData.GetRandomAttackAnimation().actionId;
            else
                AttackingActionId = -1;
        }
    }

    protected void StopAttack()
    {
        if (AttackingActionId >= 0 && photonView.IsMine)
            AttackingActionId = -1;
    }

    public void UseSkill(sbyte hotkeyId)
    {
        SkillData skill;
        if (AttackingActionId < 0 &&
            UsingSkillHotkeyId < 0 &&
            photonView.IsMine && skills.TryGetValue(hotkeyId, out skill) &&
            GetSkillCoolDownCount(hotkeyId) > skill.coolDown)
        {
            lastSkillUseTimes[hotkeyId] = Time.unscaledTime;
            UsingSkillHotkeyId = hotkeyId;
        }
    }

    public float GetSkillCoolDownCount(sbyte hotkeyId)
    {
        return Time.unscaledTime - lastSkillUseTimes[hotkeyId];
    }

    IEnumerator AttackRoutine()
    {
        if (!IsPlayingAttackAnim &&
            Hp > 0 &&
            characterModel != null &&
            characterModel.CacheAnimator != null)
        {
            IsPlayingAttackAnim = true;
            AttackAnimation attackAnimation;
            if (weaponData != null && AttackingActionId >= 0 && AttackingActionId < 255 &&
                weaponData.AttackAnimations.TryGetValue(AttackingActionId, out attackAnimation))
            {
                AttackingDamageEntity = weaponData.damagePrefab;
                AttackingSpreadDamages = TotalSpreadDamages;
                byte actionId = (byte)AttackingActionId;
                yield return StartCoroutine(PlayAttackAnimationRoutine(attackAnimation, weaponData.attackFx, () =>
                {
                    weaponData.Launch(this, AimPosition, actionId);
                }));
                // If player still attacking, random new attacking action id
                if (PhotonNetwork.IsMasterClient && AttackingActionId >= 0 && weaponData != null)
                    AttackingActionId = weaponData.GetRandomAttackAnimation().actionId;
                AttackingDamageEntity = null;
                AttackingSpreadDamages = 0;
            }
            IsPlayingAttackAnim = false;
        }
    }

    IEnumerator UseSkillRoutine()
    {
        if (!IsPlayingUseSkillAnim &&
            Hp > 0 &&
            characterModel != null &&
            characterModel.CacheAnimator != null)
        {
            IsPlayingUseSkillAnim = true;
            SkillData skillData;
            if (skills.TryGetValue((sbyte)UsingSkillHotkeyId, out skillData))
            {
                AttackingDamageEntity = skillData.damagePrefab;
                AttackingSpreadDamages = skillData.TotalSpreadDamages;
                yield return StartCoroutine(PlayAttackAnimationRoutine(skillData.attackAnimation, skillData.attackFx, () =>
                {
                    skillData.Launch(this, AimPosition);
                }));
                AttackingDamageEntity = null;
                AttackingSpreadDamages = 0;
            }
            UsingSkillHotkeyId = -1;
            IsPlayingUseSkillAnim = false;
        }
    }

    IEnumerator PlayAttackAnimationRoutine(AttackAnimation attackAnimation, AudioClip[] attackFx, System.Action onAttack)
    {
        var animator = characterModel.CacheAnimator;
        if (animator != null && attackAnimation != null)
        {
            if (endActionDelayCoroutine != null)
                StopCoroutine(endActionDelayCoroutine);
            // Play attack animation
            characterModel.CacheAnimator.SetBool("DoAction", true);
            characterModel.CacheAnimator.SetInteger("ActionID", attackAnimation.actionId);
            characterModel.CacheAnimator.Play(0, 1, 0);

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
        characterModel.CacheAnimator.SetBool("DoAction", false);
    }

    public virtual bool ReceiveDamage(CharacterEntity attacker, int damage)
    {
        if (Hp <= 0 || IsInvincible)
            return false;

        if (!GameplayManager.Singleton.CanReceiveDamage(this, attacker))
            return false;

        // Calculate damage and reduceHp
        int reduceHp = (int)(damage + ((float)damage * TotalIncreaseDamageRate) - ((float)damage * TotalReduceReceiveDamageRate)) - TotalDefend;

        // Blocking
        if (IsBlocking)
            reduceHp -= Mathf.CeilToInt(damage * TotalBlockReduceDamageRate);

        // Avoid increasing hp by damage
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
                ++syncDieCount.Value;
            }
        }
        return true;
    }

    public void KilledTarget(CharacterEntity target)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        var gameplayManager = GameplayManager.Singleton;
        var targetLevel = target.Level;
        var maxLevel = gameplayManager.maxLevel;
        Exp += Mathf.CeilToInt(target.RewardExp * TotalExpRate);
        var increaseScore = Mathf.CeilToInt(target.KillScore * TotalScoreRate);
        syncScore.Value += increaseScore;
        GameNetworkManager.Singleton.OnScoreIncrease(this, increaseScore);
        foreach (var rewardCurrency in gameplayManager.rewardCurrencies)
        {
            var currencyId = rewardCurrency.currencyId;
            var amount = rewardCurrency.amount.Calculate(targetLevel, maxLevel);
            photonView.TargetRPC(RpcTargetRewardCurrency, photonView.Owner, currencyId, amount);
        }
        var increaseKill = 1;
        syncKillCount.Value += increaseKill;
        GameNetworkManager.Singleton.OnKillIncrease(this, increaseKill);
        GameNetworkManager.Singleton.SendKillNotify(PlayerName, target.PlayerName, weaponData == null ? string.Empty : weaponData.GetId());
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
        SelectWeapon = weaponData.GetHashId();
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

    public virtual void OnUpdateSelectCharacter(int selectCharacter)
    {
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

    public virtual void OnUpdateSelectHead(int selectHead)
    {
        headData = GameInstance.GetHead(selectHead);
        if (characterModel != null && headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        UpdateCharacterModelHiddingState();
        UpdateSkills();
    }

    public virtual void OnUpdateSelectWeapon(int selectWeapon)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (DefaultSelectWeapon == 0)
                DefaultSelectWeapon = selectWeapon;
        }
        weaponData = GameInstance.GetWeapon(selectWeapon);
        if (characterModel != null && weaponData != null)
            characterModel.SetWeaponModel(weaponData.rightHandObject, weaponData.leftHandObject, weaponData.shieldObject);
        UpdateCharacterModelHiddingState();
        UpdateSkills();
    }

    public virtual void OnUpdateSelectCustomEquipments(int[] selectCustomEquipments)
    {
        if (characterModel != null)
            characterModel.ClearCustomModels();
        customEquipmentDict.Clear();
        if (selectCustomEquipments != null)
        {
            for (var i = 0; i < selectCustomEquipments.Length; ++i)
            {
                var customEquipmentData = GameInstance.GetCustomEquipment(selectCustomEquipments[i]);
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

    public virtual void OnUpdateAttributeAmounts()
    {
        refreshingSumAddStats = true;
    }

    public void ServerInvincible()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        InvincibleTime = Time.unscaledTime;
        IsInvincible = true;
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
        if (DefaultSelectWeapon != 0)
            SelectWeapon = DefaultSelectWeapon;
        IsPlayingAttackAnim = false;
        IsPlayingUseSkillAnim = false;
        IsDeadMarked = false;
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
            SelectHead = selectHead;
            SelectCharacter = selectCharacter;
            SelectWeapon = selectWeapon;
            SelectCustomEquipments = selectCustomEquipments;
            Extra = extra;
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
        if (!IsReady)
        {
            ServerSpawn(false);
            IsReady = true;
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

    public void CmdAddAttribute(int id)
    {
        photonView.MasterRPC(RpcServerAddAttribute, id);
    }

    public void CmdDash()
    {
        // Play dash animation on other clients
        photonView.OthersRPC(RpcDash);
    }

    [PunRPC]
    protected void RpcServerAddAttribute(int id)
    {
        if (StatPoint > 0)
        {
            if (GameplayManager.Singleton.Attributes.ContainsKey(id))
            {
                AttributeAmounts = AttributeAmounts.Increase(id, 1);
                if (GameplayManager.Singleton.Attributes[id].changingWeapon)
                    SelectWeapon = GameplayManager.Singleton.Attributes[id].changingWeapon.GetHashId();
                --StatPoint;
            }
        }
    }

    [PunRPC]
    public void RpcApplyStatusEffect(int dataId, int applierId)
    {
        // Destroy applied status effect, because it cannot be stacked
        StatusEffectEntity statusEffect;
        if (!GameInstance.StatusEffects.TryGetValue(dataId, out statusEffect))
            return;
        RemoveAppliedStatusEffect(dataId);
        // Find applier
        CharacterEntity applier = null;
        PhotonView applierView = PhotonView.Find(applierId);
        if (applierView != null)
            applier = applierView.GetComponent<CharacterEntity>();
        refreshingSumAddStats = true;
        // Found prefab, instantiates to character
        statusEffect = Instantiate(statusEffect, transform.position, transform.rotation, transform);
        // Just in case the game object might be not activated by default
        statusEffect.gameObject.SetActive(true);
        // Set applying character
        statusEffect.Applied(this, applier);
        // Add to applied status effects
        appliedStatusEffects[dataId] = statusEffect;
    }

    public void RemoveAppliedStatusEffect(int dataId)
    {
        StatusEffectEntity statusEffect;
        if (!appliedStatusEffects.TryGetValue(dataId, out statusEffect))
            return;
        refreshingSumAddStats = true;
        appliedStatusEffects.Remove(dataId);
        if (statusEffect)
            Destroy(statusEffect.gameObject);
    }

    [PunRPC]
    protected void RpcDash()
    {
        // Just play dash animation on another clients
        IsDashing = true;
        dashingTime = Time.unscaledTime;
    }

    [PunRPC]
    protected void RpcTargetDead()
    {
        DeathTime = Time.unscaledTime;
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
}
