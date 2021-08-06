using Photon.Pun;

public class SyncSelectWeaponRpcComponent : BaseSyncVarRpcComponent<int>
{
    private CharacterEntity entity;
    protected override void Awake()
    {
        base.Awake();
        entity = GetComponent<CharacterEntity>();
        onValueChange.AddListener(OnValueChange);
    }

    void OnValueChange(int value)
    {
        entity.OnUpdateSelectWeapon(value);
    }

    [PunRPC]
    protected virtual void RpcUpdateSelectWeapon(int value)
    {
        _value = value;
        entity.OnUpdateSelectWeapon(value);
    }
}
