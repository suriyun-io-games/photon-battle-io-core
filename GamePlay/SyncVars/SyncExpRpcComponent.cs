using Photon.Pun;

public class SyncExpRpcComponent : BaseSyncVarRpcComponent<int>
{
    [PunRPC]
    protected virtual void RpcUpdateExp(int value)
    {
        _value = value;
    }
}
