using Photon.Pun;

public class SyncDefaultSelectWeaponRpcComponent : BaseSyncVarRpcComponent<int>
{
    [PunRPC]
    protected virtual void RpcUpdateDefaultSelectWeapon(int value)
    {
        _value = value;
    }
}
