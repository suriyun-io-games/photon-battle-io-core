using Photon.Pun;

public class SyncStatPointRpcComponent : BaseSyncVarRpcComponent<int>
{
    [PunRPC]
    protected virtual void RpcUpdateStatPoint(int value)
    {
        _value = value;
    }
}
