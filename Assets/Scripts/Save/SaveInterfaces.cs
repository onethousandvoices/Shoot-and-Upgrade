namespace Save;

public interface ISave
{
    string Key { get; }
    byte[] Serialize();
    void Deserialize(byte[] rawData);
    void Clear();
}

public interface IClearSave
{
    void Clear();
}