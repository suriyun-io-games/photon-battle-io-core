using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class CharacterEntity : BaseNetworkGameCharacter
{
    public const float DISCONNECT_WHEN_NOT_RESPAWN_DURATION = 60;
    public const byte RPC_EFFECT_DAMAGE_SPAWN = 0;
    public const byte RPC_EFFECT_DAMAGE_HIT = 1;
    public const byte RPC_EFFECT_TRAP_HIT = 2;

    public Transform damageLaunchTransform;
    public Transform effectTransform;
    public Transform characterModelTransform;
    public GameObject[] localPlayerObjects;
    public float jumpHeight = 2f;
    [Header("UI")]
    public Transform hpBarContainer;
    public Image hpFillImage;
    public Text hpText;
    public Text nameText;
    public Text levelText;
    [Header("Effect")]
    public GameObject invincibleEffect;

    #region Sync Vars
    protected int _hp;
    protected int _exp;
    protected int _level;
    protected int _statPoint;
    protected int _watchAdsCount;
    protected string _selectCharacter;
    protected string _selectHead;
    protected string _selectWeapon;
    protected bool _isInvincible;
    protected int _attackingActionId;
    protected CharacterStats _addStats;
    protected string _extra;

    public virtual int hp
    {
        get { return _hp; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != hp)
            {
                _hp = value;
                photonView.RPC("RpcUpdateHp", PhotonTargets.Others, value);
            }
        }
    }
    public int Hp
    {
        get { return hp; }
        set
        {
            if (!PhotonNetwork.isMasterClient)
                return;

            if (value <= 0)
            {
                value = 0;
                if (!isDead)
                {
                    photonView.RPC("RpcTargetDead", photonView.owner);
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
            if (PhotonNetwork.isMasterClient && value != exp)
            {
                _exp = value;
                photonView.RPC("RpcUpdateExp", PhotonTargets.Others, value);
            }
        }
    }
    public virtual int Exp
    {
        get { return exp; }
        set
        {
            if (!PhotonNetwork.isMasterClient)
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
            if (PhotonNetwork.isMasterClient && value != level)
            {
                _level = value;
                photonView.RPC("RpcUpdateLevel", PhotonTargets.Others, value);
            }
        }
    }
    public virtual int statPoint
    {
        get { return _statPoint; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != statPoint)
            {
                _statPoint = value;
                photonView.RPC("RpcUpdateStatPoint", PhotonTargets.Others, value);
            }
        }
    }
    public virtual int watchAdsCount
    {
        get { return _watchAdsCount; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != watchAdsCount)
            {
                _watchAdsCount = value;
                photonView.RPC("RpcUpdateWatchAdsCount", PhotonTargets.Others, value);
            }
        }
    }
    public virtual string selectCharacter
    {
        get { return _selectCharacter; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != selectCharacter)
            {
                _selectCharacter = value;
                photonView.RPC("RpcUpdateSelectCharacter", PhotonTargets.All, value);
            }
        }
    }
    public virtual string selectHead
    {
        get { return _selectHead; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != selectHead)
            {
                _selectHead = value;
                photonView.RPC("RpcUpdateSelectHead", PhotonTargets.All, value);
            }
        }
    }
    public virtual string selectWeapon
    {
        get { return _selectWeapon; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != selectWeapon)
            {
                _selectWeapon = value;
                photonView.RPC("RpcUpdateSelectWeapon", PhotonTargets.All, value);
            }
        }
    }
    public virtual bool isInvincible
    {
        get { return _isInvincible; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != isInvincible)
            {
                _isInvincible = value;
                photonView.RPC("RpcUpdateIsInvincible", PhotonTargets.Others, value);
            }
        }
    }
    public virtual int attackingActionId
    {
        get { return _attackingActionId; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != attackingActionId)
            {
                _attackingActionId = value;
                photonView.RPC("RpcUpdateAttackingActionId", PhotonTargets.Others, value);
            }
        }
    }
    public virtual CharacterStats addStats
    {
        get { return _addStats; }
        set
        {
            if (PhotonNetwork.isMasterClient)
            {
                _addStats = value;
                photonView.RPC("RpcUpdateAddStats", PhotonTargets.Others, JsonUtility.ToJson(value));
            }
        }
    }
    public virtual string extra
    {
        get { return _extra; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != extra)
            {
                _extra = value;
                photonView.RPC("RpcUpdateExtra", PhotonTargets.Others, value);
            }
        }
    }
    #endregion

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
    protected bool isMobileInput;
    protected Vector2 inputMove;
    protected Vector2 inputDirection;
    protected bool inputAttack;
    protected bool inputJump;
    protected Vector3? previousPosition;
    protected Vector3 currentVelocity;

    public bool isReady { get; private set; }
    public bool isDead { get; private set; }
    public bool isGround { get; private set; }
    public bool isPlayingAttackAnim { get; private set; }
    public float deathTime { get; private set; }
    public float invincibleTime { get; private set; }
    
    private bool isHidding;
    public bool IsHidding
    {
        get { return isHidding; }
        set
        {
            if (isHidding == value)
                return;

            isHidding = value;
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                renderer.enabled = !isHidding;
            var canvases = GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
                canvas.enabled = !isHidding;
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
    private Rigidbody tempRigidbody;
    public Rigidbody TempRigidbody
    {
        get
        {
            if (tempRigidbody == null)
                tempRigidbody = GetComponent<Rigidbody>();
            return tempRigidbody;
        }
    }

    public CharacterStats SumAddStats
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
            return stats;
        }
    }

    public int TotalHp
    {
        get
        {
            var total = GameplayManager.Singleton.minHp + SumAddStats.addHp;
            return total;
        }
    }

    public int TotalAttack
    {
        get
        {
            var total = GameplayManager.Singleton.minAttack + SumAddStats.addAttack;
            return total;
        }
    }

    public int TotalDefend
    {
        get
        {
            var total = GameplayManager.Singleton.minDefend + SumAddStats.addDefend;
            return total;
        }
    }

    public int TotalMoveSpeed
    {
        get
        {
            var total = GameplayManager.Singleton.minMoveSpeed + SumAddStats.addMoveSpeed;
            return total;
        }
    }

    public float TotalExpRate
    {
        get
        {
            var total = 1 + SumAddStats.addExpRate;
            return total;
        }
    }

    public float TotalScoreRate
    {
        get
        {
            var total = 1 + SumAddStats.addScoreRate;
            return total;
        }
    }

    public float TotalHpRecoveryRate
    {
        get
        {
            var total = 1 + SumAddStats.addHpRecoveryRate;
            return total;
        }
    }

    public float TotalDamageRateLeechHp
    {
        get
        {
            var total = SumAddStats.addDamageRateLeechHp;
            return total;
        }
    }

    public int TotalSpreadDamages
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

    protected override void Init()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        base.Init();
        hp = 0;
        exp = 0;
        level = 1;
        statPoint = 0;
        watchAdsCount = 0;
        selectCharacter = "";
        selectHead = "";
        selectWeapon = "";
        isInvincible = false;
        attackingActionId = -1;
        addStats = new CharacterStats();
        extra = "";
    }

    protected override void Awake()
    {
        base.Awake();
        gameObject.layer = GameInstance.Singleton.characterLayer;
        if (damageLaunchTransform == null)
            damageLaunchTransform = TempTransform;
        if (effectTransform == null)
            effectTransform = TempTransform;
        if (characterModelTransform == null)
            characterModelTransform = TempTransform;
        foreach (var localPlayerObject in localPlayerObjects)
        {
            localPlayerObject.SetActive(false);
        }
        deathTime = Time.unscaledTime;
    }

    protected override void OnStartLocalPlayer()
    {
        if (photonView.isMine)
        {
            var followCam = FindObjectOfType<FollowCamera>();
            followCam.target = TempTransform;
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

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        base.OnPhotonPlayerConnected(newPlayer);
        if (!PhotonNetwork.isMasterClient)
            return;
        photonView.RPC("RpcUpdateHp", newPlayer, hp);
        photonView.RPC("RpcUpdateExp", newPlayer, exp);
        photonView.RPC("RpcUpdateLevel", newPlayer, level);
        photonView.RPC("RpcUpdateStatPoint", newPlayer, statPoint);
        photonView.RPC("RpcUpdateWatchAdsCount", newPlayer, watchAdsCount);
        photonView.RPC("RpcUpdateSelectCharacter", newPlayer, selectCharacter);
        photonView.RPC("RpcUpdateSelectHead", newPlayer, selectHead);
        photonView.RPC("RpcUpdateSelectWeapon", newPlayer, selectWeapon);
        photonView.RPC("RpcUpdateIsInvincible", newPlayer, isInvincible);
        photonView.RPC("RpcUpdateAttackingActionId", newPlayer, attackingActionId);
        photonView.RPC("RpcUpdateAddStats", newPlayer, JsonUtility.ToJson(addStats));
        photonView.RPC("RpcUpdateExtra", newPlayer, extra);
    }

    protected override void Update()
    {
        base.Update();
        if (NetworkManager != null && NetworkManager.IsMatchEnded)
            return;
        
        if (Hp <= 0)
        {
            if (!PhotonNetwork.isMasterClient && photonView.isMine && Time.unscaledTime - deathTime >= DISCONNECT_WHEN_NOT_RESPAWN_DURATION)
                GameNetworkManager.Singleton.LeaveRoom();

            if (PhotonNetwork.isMasterClient)
                attackingActionId = -1;
        }

        if (PhotonNetwork.isMasterClient && isInvincible && Time.unscaledTime - invincibleTime >= GameplayManager.Singleton.invincibleDuration)
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
    }

    private void FixedUpdate()
    {
        if (!previousPosition.HasValue)
            previousPosition = TempTransform.position;
        var currentMove = TempTransform.position - previousPosition.Value;
        currentVelocity = currentMove / Time.deltaTime;
        previousPosition = TempTransform.position;

        if (NetworkManager != null && NetworkManager.IsMatchEnded)
            return;

        UpdateMovements();
    }

    protected virtual void UpdateInput()
    {
        if (!photonView.isMine || Hp <= 0)
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

        var canAttack = Application.isMobilePlatform || !EventSystem.current.IsPointerOverGameObject();
        inputMove = Vector2.zero;
        inputDirection = Vector2.zero;
        inputAttack = false;
        if (canControl)
        {
            inputMove = new Vector2(InputManager.GetAxis("Horizontal", false), InputManager.GetAxis("Vertical", false));
            if (!inputJump)
                inputJump = InputManager.GetButtonDown("Jump") && isGround;
            if (isMobileInput)
            {
                inputDirection = new Vector2(InputManager.GetAxis("Mouse X", false), InputManager.GetAxis("Mouse Y", false));
                if (canAttack)
                    inputAttack = inputDirection.magnitude != 0;
            }
            else
            {
                inputDirection = (InputManager.MousePosition() - targetCamera.WorldToScreenPoint(TempTransform.position)).normalized;
                if (canAttack)
                    inputAttack = InputManager.GetButton("Fire1");
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
        }

        if (attackingActionId >= 0 && !isPlayingAttackAnim)
            StartCoroutine(AttackRoutine(attackingActionId));
    }

    protected virtual float GetMoveSpeed()
    {
        return TotalMoveSpeed * GameplayManager.REAL_MOVE_SPEED_RATE;
    }

    protected virtual void Move(Vector3 direction)
    {
        if (direction.magnitude != 0)
        {
            if (direction.magnitude > 1)
                direction = direction.normalized;

            var targetSpeed = GetMoveSpeed();
            var targetVelocity = direction * targetSpeed;

            // Apply a force that attempts to reach our target velocity
            Vector3 velocity = TempRigidbody.velocity;
            Vector3 velocityChange = (targetVelocity - velocity);
            velocityChange.x = Mathf.Clamp(velocityChange.x, -targetSpeed, targetSpeed);
            velocityChange.y = 0;
            velocityChange.z = Mathf.Clamp(velocityChange.z, -targetSpeed, targetSpeed);
            TempRigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
        }
    }

    protected virtual void UpdateMovements()
    {
        if (!photonView.isMine || Hp <= 0)
            return;

        var moveDirection = new Vector3(inputMove.x, 0, inputMove.y);
        Move(moveDirection);
        Rotate(inputDirection);
        if (inputAttack)
            Attack();
        else
            StopAttack();

        var velocity = TempRigidbody.velocity;
        if (isGround && inputJump)
        {
            TempRigidbody.velocity = new Vector3(velocity.x, CalculateJumpVerticalSpeed(), velocity.z);
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
        if (direction.magnitude != 0)
        {
            int newRotation = (int)(Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y + targetCamera.transform.eulerAngles.y);
            Quaternion targetRotation = Quaternion.Euler(0, newRotation, 0);
            TempTransform.rotation = targetRotation;
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
        if (attackingActionId < 0 && photonView.isMine)
            CmdAttack();
    }

    protected void StopAttack()
    {
        if (attackingActionId >= 0 && photonView.isMine)
            CmdStopAttack();
    }

    IEnumerator AttackRoutine(int actionId)
    {
        if (!isPlayingAttackAnim && 
            Hp > 0 &&
            characterModel != null &&
            characterModel.TempAnimator != null)
        {
            isPlayingAttackAnim = true;
            var animator = characterModel.TempAnimator;
            AttackAnimation attackAnimation;
            if (weaponData != null &&
                weaponData.AttackAnimations.TryGetValue(actionId, out attackAnimation))
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

                // Launch damage entity on server only
                if (PhotonNetwork.isMasterClient)
                    weaponData.Launch(this, attackAnimation.isAnimationForLeftHandWeapon);

                // Random play shoot sounds
                if (weaponData.attackFx != null && weaponData.attackFx.Length > 0 && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(weaponData.attackFx[Random.Range(0, weaponData.attackFx.Length - 1)], TempTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);

                // Wait till animation end
                yield return new WaitForSeconds((animationDuration - launchDuration) / speed);
            }
            // If player still attacking, random new attacking action id
            if (PhotonNetwork.isMasterClient && attackingActionId >= 0 && weaponData != null)
                attackingActionId = weaponData.GetRandomAttackAnimation().actionId;
            yield return new WaitForEndOfFrame();

            // Attack animation ended
            animator.SetBool("DoAction", false);
            isPlayingAttackAnim = false;
        }
    }
    
    public void ReceiveDamage(CharacterEntity attacker, int damage)
    {
        var gameplayManager = GameplayManager.Singleton;
        if (Hp <= 0 || isInvincible || !gameplayManager.CanReceiveDamage(this))
            return;

        photonView.RPC("RpcEffect", PhotonTargets.All, attacker.photonView.viewID, RPC_EFFECT_DAMAGE_HIT);
        int reduceHp = damage - TotalDefend;
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
                if (onDead != null)
                    onDead.Invoke();
                attacker.KilledTarget(this);
                ++dieCount;
            }
        }
    }
    
    public void KilledTarget(CharacterEntity target)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        var gameplayManager = GameplayManager.Singleton;
        var targetLevel = target.level;
        var maxLevel = gameplayManager.maxLevel;
        Exp += Mathf.CeilToInt(gameplayManager.GetRewardExp(targetLevel) * TotalExpRate);
        score += Mathf.CeilToInt(gameplayManager.GetKillScore(targetLevel) * TotalScoreRate);
        foreach (var rewardCurrency in gameplayManager.rewardCurrencies)
        {
            var currencyId = rewardCurrency.currencyId;
            var amount = rewardCurrency.amount.Calculate(targetLevel, maxLevel);
            photonView.RPC("RpcTargetRewardCurrency", photonView.owner, currencyId, amount);
        }
        ++killCount;
        GameNetworkManager.Singleton.SendKillNotify(playerName, target.playerName, weaponData == null ? string.Empty : weaponData.GetId());
    }
    
    public void Heal(int amount)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        if (Hp <= 0)
            return;

        Hp += amount;
    }

    public float GetAttackRange()
    {
        if (weaponData == null || weaponData.damagePrefab == null)
            return 0;
        return weaponData.damagePrefab.GetAttackRange();
    }

    public void ChangeWeapon(WeaponData weaponData)
    {
        if (weaponData == null)
            return;
        selectWeapon = weaponData.GetId();
    }
    
    public void UpdateCharacterModelHiddingState()
    {
        if (characterModel == null)
            return;
        var renderers = characterModel.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
            renderer.enabled = !IsHidding;
    }

    public virtual void OnSpawn() { }
    
    public void ServerInvincible()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        invincibleTime = Time.unscaledTime;
        isInvincible = true;
    }
    
    public void ServerSpawn(bool isWatchedAds)
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        if (Respawn(isWatchedAds))
        {
            var gameplayManager = GameplayManager.Singleton;
            ServerInvincible();
            OnSpawn();
            var position = gameplayManager.GetCharacterSpawnPosition(this);
            TempTransform.position = position;
            photonView.RPC("RpcTargetSpawn", photonView.owner, position.x, position.y, position.z);
            ServerRevive();
        }
    }
    
    public void ServerRespawn(bool isWatchedAds)
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        if (CanRespawn(isWatchedAds))
            ServerSpawn(isWatchedAds);
    }
    
    public void ServerRevive()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        isPlayingAttackAnim = false;
        isDead = false;
        Hp = TotalHp;
    }

    public void CmdInit(string selectHead, string selectCharacter, string selectWeapon, string extra)
    {
        photonView.RPC("RpcServerInit", PhotonTargets.MasterClient, selectHead, selectCharacter, selectWeapon, extra);
    }

    [PunRPC]
    protected void RpcServerInit(string selectHead, string selectCharacter, string selectWeapon, string extra)
    {
        Hp = TotalHp;
        this.selectHead = selectHead;
        this.selectCharacter = selectCharacter;
        this.selectWeapon = selectWeapon;
        this.extra = extra;
        var networkManager = BaseNetworkGameManager.Singleton;
        if (networkManager != null)
            networkManager.RegisterCharacter(this);
    }
    
    public void CmdReady()
    {
        photonView.RPC("RpcServerReady", PhotonTargets.MasterClient);
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
        photonView.RPC("RpcServerRespawn", PhotonTargets.MasterClient, isWatchedAds);
    }

    [PunRPC]
    protected void RpcServerRespawn(bool isWatchedAds)
    {
        ServerRespawn(isWatchedAds);
    }
    
    public void CmdAttack()
    {
        photonView.RPC("RpcServerAttack", PhotonTargets.MasterClient);
    }

    [PunRPC]
    protected void RpcServerAttack()
    {
        if (weaponData != null)
            attackingActionId = weaponData.GetRandomAttackAnimation().actionId;
        else
            attackingActionId = -1;
    }
    
    public void CmdStopAttack()
    {
        photonView.RPC("RpcServerStopAttack", PhotonTargets.MasterClient);
    }

    [PunRPC]
    protected void RpcServerStopAttack()
    {
        attackingActionId = -1;
    }
    
    public void CmdAddAttribute(string name)
    {
        photonView.RPC("RpcServerAddAttribute", PhotonTargets.MasterClient, name);
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
    protected void RpcEffect(int triggerViewId, byte effectType)
    {
        var triggerObject = PhotonView.Find(triggerViewId);

        if (triggerObject != null)
        {
            if (effectType == RPC_EFFECT_DAMAGE_SPAWN || effectType == RPC_EFFECT_DAMAGE_HIT)
            {
                var attacker = triggerObject.GetComponent<CharacterEntity>();
                if (attacker != null &&
                    attacker.weaponData != null &&
                    attacker.weaponData.damagePrefab != null)
                {
                    var damagePrefab = attacker.weaponData.damagePrefab;
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
            else if (effectType == RPC_EFFECT_TRAP_HIT)
            {
                var trap = triggerObject.GetComponent<TrapEntity>();
                if (trap != null)
                    EffectEntity.PlayEffect(trap.hitEffectPrefab, effectTransform);
            }
        }
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
    protected virtual void RpcUpdateSelectCharacter(string selectCharacter)
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
        characterModel.gameObject.SetActive(true);
        UpdateCharacterModelHiddingState();
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectHead(string selectHead)
    {
        _selectHead = selectHead;
        headData = GameInstance.GetHead(selectHead);
        if (characterModel != null && headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        UpdateCharacterModelHiddingState();
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectWeapon(string selectWeapon)
    {
        _selectWeapon = selectWeapon;
        weaponData = GameInstance.GetWeapon(selectWeapon);
        if (characterModel != null && weaponData != null)
            characterModel.SetWeaponModel(weaponData.rightHandObject, weaponData.leftHandObject, weaponData.shieldObject);
        UpdateCharacterModelHiddingState();
    }
    [PunRPC]
    protected virtual void RpcUpdateIsInvincible(bool isInvincible)
    {
        _isInvincible = isInvincible;
    }
    [PunRPC]
    protected virtual void RpcUpdateAttackingActionId(int attackingActionId)
    {
        _attackingActionId = attackingActionId;
    }
    [PunRPC]
    protected virtual void RpcUpdateAddStats(string json)
    {
        _addStats = JsonUtility.FromJson<CharacterStats>(json);
    }
    [PunRPC]
    protected virtual void RpcUpdateExtra(string extra)
    {
        _extra = extra;
    }
    #endregion
}
