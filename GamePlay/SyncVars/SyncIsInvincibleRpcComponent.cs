using Photon.Pun;

public class SyncIsInvincibleRpcComponent : BaseSyncVarRpcComponent<bool>
{
    [PunRPC]
    protected virtual void RpcUpdateIsInvincible(bool value)
    {
        _value = value;
    }
}
