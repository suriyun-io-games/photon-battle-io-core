using Photon.Pun;

public class SyncSelectCharacterRpcComponent : BaseSyncVarRpcComponent<int>
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
        entity.OnUpdateSelectCharacter(value);
    }

    [PunRPC]
    protected virtual void RpcUpdateSelectCharacter(int value)
    {
        _value = value;
        entity.OnUpdateSelectCharacter(value);
    }
}
