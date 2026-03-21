namespace Controllers;

public static class LocalizationController
{
    private static readonly Dictionary<string, string> _translations = new(32);
    
    private static bool _initialized;

    public static void Init(LocalizationConfig config)
    {
        if (_initialized) return;
        
        _translations.Clear();
        
        var language = config.Language;
        var entries = config.Entries;
        for (var i = 0; i < entries.Length; i++)
        {
            var value = language switch
            {
                SystemLanguage.Russian => entries[i].Ru,
                _                      => entries[i].En
            };
            _translations[entries[i].Key] = value;
        }
        
        _initialized = true;
    }

    public static string Get(string key)
    {
        if (_translations.TryGetValue(key, out var value))
            return value;
        
        Debug.LogWarning($"Localization key not found: {key}");
        return key;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _translations.Clear();
        _initialized = false;
    }
}