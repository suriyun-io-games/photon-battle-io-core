using ExitGames.Client.Photon;

[System.Serializable]
public struct CharacterStats
{
    public int addHp;
    public int addAttack;
    public int addDefend;
    public int addMoveSpeed;
    public float addExpRate;
    public float addScoreRate;
    public float addHpRecoveryRate;
    public float addBlockReduceDamageRate;
    public float addDamageRateLeechHp;
    public int addSpreadDamages;
    public float increaseDamageRate;
    public float reduceReceiveDamageRate;

    public static CharacterStats operator +(CharacterStats a, CharacterStats b)
    {
        var result = new CharacterStats();
        result.addHp = a.addHp + b.addHp;
        result.addAttack = a.addAttack + b.addAttack;
        result.addDefend = a.addDefend + b.addDefend;
        result.addMoveSpeed = a.addMoveSpeed + b.addMoveSpeed;
        result.addExpRate = a.addExpRate + b.addExpRate;
        result.addScoreRate = a.addScoreRate + b.addScoreRate;
        result.addHpRecoveryRate = a.addHpRecoveryRate + b.addHpRecoveryRate;
        result.addBlockReduceDamageRate = a.addBlockReduceDamageRate + b.addBlockReduceDamageRate;
        result.addDamageRateLeechHp = a.addDamageRateLeechHp + b.addDamageRateLeechHp;
        result.addSpreadDamages = a.addSpreadDamages + b.addSpreadDamages;
        result.increaseDamageRate = a.increaseDamageRate + b.increaseDamageRate;
        result.reduceReceiveDamageRate = a.reduceReceiveDamageRate + b.reduceReceiveDamageRate;
        return result;
    }

    public static CharacterStats operator -(CharacterStats a, CharacterStats b)
    {
        var result = new CharacterStats();
        result.addHp = a.addHp - b.addHp;
        result.addAttack = a.addAttack - b.addAttack;
        result.addDefend = a.addDefend - b.addDefend;
        result.addMoveSpeed = a.addMoveSpeed - b.addMoveSpeed;
        result.addExpRate = a.addExpRate - b.addExpRate;
        result.addScoreRate = a.addScoreRate - b.addScoreRate;
        result.addHpRecoveryRate = a.addHpRecoveryRate - b.addHpRecoveryRate;
        result.addBlockReduceDamageRate = a.addBlockReduceDamageRate - b.addBlockReduceDamageRate;
        result.addDamageRateLeechHp = a.addDamageRateLeechHp - b.addDamageRateLeechHp;
        result.addSpreadDamages = a.addSpreadDamages - b.addSpreadDamages;
        result.increaseDamageRate = a.increaseDamageRate - b.increaseDamageRate;
        result.reduceReceiveDamageRate = a.reduceReceiveDamageRate - b.reduceReceiveDamageRate;
        return result;
    }

    public static CharacterStats operator *(CharacterStats a, short b)
    {
        var result = new CharacterStats();
        result.addHp = a.addHp * b;
        result.addAttack = a.addAttack * b;
        result.addDefend = a.addDefend * b;
        result.addMoveSpeed = a.addMoveSpeed * b;
        result.addExpRate = a.addExpRate * b;
        result.addScoreRate = a.addScoreRate * b;
        result.addHpRecoveryRate = a.addHpRecoveryRate * b;
        result.addBlockReduceDamageRate = a.addBlockReduceDamageRate * b;
        result.addDamageRateLeechHp = a.addDamageRateLeechHp * b;
        result.addSpreadDamages = a.addSpreadDamages * b;
        result.increaseDamageRate = a.increaseDamageRate * b;
        result.reduceReceiveDamageRate = a.reduceReceiveDamageRate * b;
        return result;
    }

    private const int IntSize = sizeof(int);
    private const int FloatSize = sizeof(float);
    private const int writeBytesSize = (IntSize * 5) + (FloatSize * 7);
    private static readonly byte[] writeBytes = new byte[writeBytesSize];
    public static byte[] SerializeMethod(object customobject)
    {
        CharacterStats data = (CharacterStats)customobject;
        int index = 0;
        Protocol.Serialize(data.addHp, writeBytes, ref index);
        Protocol.Serialize(data.addAttack, writeBytes, ref index);
        Protocol.Serialize(data.addDefend, writeBytes, ref index);
        Protocol.Serialize(data.addMoveSpeed, writeBytes, ref index);
        Protocol.Serialize(data.addExpRate, writeBytes, ref index);
        Protocol.Serialize(data.addScoreRate, writeBytes, ref index);
        Protocol.Serialize(data.addHpRecoveryRate, writeBytes, ref index);
        Protocol.Serialize(data.addBlockReduceDamageRate, writeBytes, ref index);
        Protocol.Serialize(data.addDamageRateLeechHp, writeBytes, ref index);
        Protocol.Serialize(data.addSpreadDamages, writeBytes, ref index);
        Protocol.Serialize(data.increaseDamageRate, writeBytes, ref index);
        Protocol.Serialize(data.reduceReceiveDamageRate, writeBytes, ref index);
        return writeBytes;
    }

    public static object DeserializeMethod(byte[] readBytes)
    {
        CharacterStats data = new CharacterStats();
        int index = 0;
        int tempInt = 0;
        float tempFloat = 0f;
        Protocol.Deserialize(out tempInt, readBytes, ref index);
        data.addHp = tempInt;
        Protocol.Deserialize(out tempInt, readBytes, ref index);
        data.addAttack = tempInt;
        Protocol.Deserialize(out tempInt, readBytes, ref index);
        data.addDefend = tempInt;
        Protocol.Deserialize(out tempInt, readBytes, ref index);
        data.addMoveSpeed = tempInt;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addExpRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addScoreRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addHpRecoveryRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addBlockReduceDamageRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addDamageRateLeechHp = tempFloat;
        Protocol.Deserialize(out tempInt, readBytes, ref index);
        data.addSpreadDamages = tempInt;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.increaseDamageRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.reduceReceiveDamageRate = tempFloat;
        return data;
    }
}
