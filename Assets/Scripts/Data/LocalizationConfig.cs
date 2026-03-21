namespace Data;

[CreateAssetMenu(fileName = "LocalizationConfig", menuName = "Config/Localization")]
public sealed class LocalizationConfig : ScriptableObject
{
    [SerializeField] private SystemLanguage _language = SystemLanguage.English;
    [SerializeField] private LocalizationEntry[] _entries;

    public SystemLanguage Language => _language;
    public LocalizationEntry[] Entries => _entries;
}

[Serializable]
public struct LocalizationEntry
{
    public string Key;
    public string En;
    public string Ru;
}