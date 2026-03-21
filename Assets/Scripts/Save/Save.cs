using MessagePack;

namespace Save;

public sealed class Save<T> : ISave where T : IClearSave
{
    public string Key { get; }
    public T Data { get; private set; }

    public Save(string key, T initial = default)
    {
        Key = key;
        Data = initial;
        SaveSystem.Register(this);
    }

    public byte[] Serialize() => MessagePackSerializer.Serialize(Data);

    public void Deserialize(byte[] rawData) => Data = MessagePackSerializer.Deserialize<T>(rawData);

    public void Clear() => Data.Clear();
}