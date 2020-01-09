using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PowerUpEntity : MonoBehaviourPunCallbacks
{
    public const float DestroyDelay = 1f;
    // We're going to respawn this power up so I decide to keep its prefab name to spawning when character triggered
    protected string _prefabName;
    public virtual string prefabName
    {
        get { return _prefabName; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != prefabName)
            {
                _prefabName = value;
                photonView.RPC("RpcUpdatePrefabName", RpcTarget.Others, value);
            }
        }
    }
    [Header("Recovery / Currencies")]
    public int hp;
    public int exp;
    public WeaponData changingWeapon;
    public InGameCurrency[] currencies;
    [Header("Effect")]
    public EffectEntity powerUpEffect;

    private bool isDead;

    private void Awake()
    {
        gameObject.layer = Physics.IgnoreRaycastLayer;
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (!PhotonNetwork.IsMasterClient)
            return;
        photonView.RPC("RpcUpdatePrefabName", newPlayer, prefabName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;

        if (other.gameObject.layer == Physics.IgnoreRaycastLayer)
            return;

        var character = other.GetComponent<CharacterEntity>();
        if (character != null && character.Hp > 0)
        {
            isDead = true;
            EffectEntity.PlayEffect(powerUpEffect, character.effectTransform);
            if (PhotonNetwork.IsMasterClient)
            {
                character.Hp += Mathf.CeilToInt(hp * character.TotalHpRecoveryRate);
                character.Exp += Mathf.CeilToInt(exp * character.TotalExpRate);
                if (changingWeapon != null)
                    character.ChangeWeapon(changingWeapon);
            }
            if (currencies != null && currencies.Length > 0 && 
                character.photonView.IsMine && 
                !(character is BotEntity) && 
                !(character is MonsterEntity))
            {
                foreach (var currency in currencies)
                {
                    MonetizationManager.Save.AddCurrency(currency.id, currency.amount);
                }
            }
            StartCoroutine(DestroyRoutine());
        }
    }

    IEnumerator DestroyRoutine()
    {
        var renderers = gameObject.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }
        }
        yield return new WaitForSeconds(DestroyDelay);
        // Destroy this on all clients
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
            GameplayManager.Singleton.SpawnPowerUp(prefabName);
        }
    }

    [PunRPC]
    protected virtual void RpcUpdatePrefabName(string prefabName)
    {
        _prefabName = prefabName;
    }
}
