using System.IO;
using MessagePack;
using MessagePack.Resolvers;

namespace Save;

public static class SaveSystem
{
    private static readonly Dictionary<string, ISave> _saves = new();
    private static readonly Dictionary<string, byte[]> _loadedData = new();
    
    private static string _savePath;
    private static string _tempSavePath;
    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _saves.Clear();
        _loadedData.Clear();
        _savePath = null;
        _tempSavePath = null;
        _initialized = false;
        
        MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithCompression(MessagePackCompression.Lz4BlockArray);
    }

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;
        _savePath = Path.Combine(Application.persistentDataPath, "save.dat");
        _tempSavePath = _savePath + ".tmp";
        LoadFromFile(_savePath);
    }

    public static void Register(ISave save)
    {
        if (_loadedData.TryGetValue(save.Key, out var data))
            save.Deserialize(data);
        _saves[save.Key] = save;
    }

    public static void SaveSync()
    {
        try
        {
            SerializeAll();
            SaveAllToFile(_tempSavePath);
            
            if (File.Exists(_savePath))
                File.Replace(_tempSavePath, _savePath, null);
            else
                File.Move(_tempSavePath, _savePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error on save: {e}");
        }
    }

    public static void Wipe()
    {
        foreach (var save in _saves.Values)
            save.Clear();
        
        _loadedData.Clear();
        SaveSync();
    }

    private static void SerializeAll()
    {
        foreach (var (key, value) in _saves)
            try
            {
                _loadedData[key] = value.Serialize();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to serialize key '{key}': {e}");
            }
    }

    private static void SaveAllToFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536);
        using var writer = new BinaryWriter(stream);
        writer.Write(_loadedData.Count);
        
        foreach (var (key, data) in _loadedData)
        {
            writer.Write(key);
            writer.Write(data.Length);
            writer.Write(data);
        }
        
        stream.Flush(true);
    }

    private static void LoadFromFile(string path)
    {
        _loadedData.Clear();
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        
        try
        {
            using var stream = new FileStream(path, FileMode.Open);
            using var reader = new BinaryReader(stream);
            var count = reader.ReadInt32();
            
            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var length = reader.ReadInt32();
                var data = reader.ReadBytes(length);
                _loadedData[key] = data;
            }
        }
        catch (Exception e)
        {
            _loadedData.Clear();
            Debug.LogError($"Error loading save: {e.Message}");
        }
    }
}