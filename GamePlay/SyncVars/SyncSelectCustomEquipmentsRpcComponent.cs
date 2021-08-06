using Photon.Pun;

public class SyncSelectCustomEquipmentsRpcComponent : BaseSyncVarRpcComponent<int[]>
{
    private CharacterEntity entity;
    protected override void Awake()
    {
        base.Awake();
        entity = GetComponent<CharacterEntity>();
        onValueChange.AddListener(OnValueChange);
    }

    void OnValueChange(int[] value)
    {
        entity.OnUpdateSelectCustomEquipments(value);
    }

    public override bool HasChanges(int[] value)
    {
        return true;
    }

    [PunRPC]
    protected virtual void RpcUpdateSelectCustomEquipments(int[] value)
    {
        _value = value;
        entity.OnUpdateSelectCustomEquipments(value);
    }
}
