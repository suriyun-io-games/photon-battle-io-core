using Photon.Pun;

public class SyncWatchAdsCountRpcComponent : BaseSyncVarRpcComponent<byte>
{
    [PunRPC]
    protected virtual void RpcUpdateWatchAdsCount(byte value)
    {
        _value = value;
    }
}
