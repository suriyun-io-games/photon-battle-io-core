using ExitGames.Client.Photon;
using System.Collections.Generic;

[System.Serializable]
public class AttributeAmounts
{
    private Dictionary<int, short> attributeAmounts = new Dictionary<int, short>();
    public Dictionary<int, short> Dict { get { return attributeAmounts; } }

    private const int IntSize = sizeof(int);
    private const int ShortSize = sizeof(short);

    public AttributeAmounts Increase(int id, short value)
    {
        if (attributeAmounts.ContainsKey(id))
            attributeAmounts[id] = (short)(attributeAmounts[id] + value);
        else
            attributeAmounts.Add(id, value);
        return this;
    }

    public static byte[] SerializeMethod(object customobject)
    {
        AttributeAmounts data = (AttributeAmounts)customobject;
        short length = (short)(data.attributeAmounts == null ? 0 : data.attributeAmounts.Count);
        byte[] writeBytes = new byte[((IntSize + ShortSize) * length) + ShortSize];
        int index = 0;
        Protocol.Serialize(length, writeBytes, ref index);
        if (length > 0)
        {
            foreach (var attributeAmount in data.Dict)
            {
                Protocol.Serialize(attributeAmount.Key, writeBytes, ref index);
                Protocol.Serialize(attributeAmount.Value, writeBytes, ref index);
            }
        }
        return writeBytes;
    }

    public static object DeserializeMethod(byte[] readBytes)
    {
        AttributeAmounts data = new AttributeAmounts();
        Dictionary<int, short> attributeAmounts = new Dictionary<int, short>();
        int index = 0;
        int tempInt;
        short tempShort;
        Protocol.Deserialize(out tempShort, readBytes, ref index);
        short length = tempShort;
        if (length > 0)
        {
            Protocol.Deserialize(out tempInt, readBytes, ref index);
            Protocol.Deserialize(out tempShort, readBytes, ref index);
            attributeAmounts.Add(tempInt, tempShort);
            data.attributeAmounts = attributeAmounts;
        }
        return data;
    }
}