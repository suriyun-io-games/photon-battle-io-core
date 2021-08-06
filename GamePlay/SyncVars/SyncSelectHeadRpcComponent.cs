using Photon.Pun;

public class SyncSelectHeadRpcComponent : BaseSyncVarRpcComponent<int>
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
        entity.OnUpdateSelectHead(value);
    }

    [PunRPC]
    protected virtual void RpcUpdateSelectHead(int value)
    {
        _value = value;
        entity.OnUpdateSelectHead(value);
    }
}
