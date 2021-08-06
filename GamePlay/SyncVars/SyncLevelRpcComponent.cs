using Photon.Pun;

public class SyncLevelRpcComponent : BaseSyncVarRpcComponent<int>
{
    [PunRPC]
    protected virtual void RpcUpdateLevel(int value)
    {
        _value = value;
    }
}
