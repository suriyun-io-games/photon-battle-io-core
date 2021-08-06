using Photon.Pun;

public class SyncAttributeAmountsRpcComponent : BaseSyncVarRpcComponent<AttributeAmounts>
{
    private CharacterEntity entity;
    protected override void Awake()
    {
        base.Awake();
        entity = GetComponent<CharacterEntity>();
        onValueChange.AddListener(OnValueChange);
    }

    void OnValueChange(AttributeAmounts value)
    {
        entity.OnUpdateAttributeAmounts();
    }

    public override bool HasChanges(AttributeAmounts value)
    {
        return true;
    }

    [PunRPC]
    protected virtual void RpcUpdateAttributeAmounts(AttributeAmounts value)
    {
        _value = value;
        entity.OnUpdateAttributeAmounts();
    }
}
