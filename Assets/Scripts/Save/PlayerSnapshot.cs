using MessagePack;

namespace Save;

[MessagePackObject(true)]
public sealed class PlayerSnapshot : IClearSave
{
    public int SpeedLevel;
    public int HealthLevel;
    public int DamageLevel;
    public int UpgradePoints;

    public void Clear()
    {
        SpeedLevel = 0;
        HealthLevel = 0;
        DamageLevel = 0;
        UpgradePoints = 0;
    }
}