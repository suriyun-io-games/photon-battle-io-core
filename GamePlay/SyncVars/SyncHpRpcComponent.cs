using Photon.Pun;

public class SyncHpRpcComponent : BaseSyncVarRpcComponent<int>
{
    [PunRPC]
    protected virtual void RpcUpdateHp(int value)
    {
        _value = value;
    }
}
