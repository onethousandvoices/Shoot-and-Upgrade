using Save;

namespace Models;

public sealed class PlayerModel
{
    public event Action Changed;
    public event Action Died;
    
    private readonly PlayerConfig _config;
    private readonly PlayerSnapshot _snapshot;
    private float _currentHealth;
    private bool _saveDirty;

    public int UpgradePoints => _snapshot.UpgradePoints;
    public float CurrentHealth => _currentHealth;
    public float MoveSpeed => _config.MoveSpeed + _snapshot.SpeedLevel * _config.SpeedPerLevel;
    public float MaxHealth => _config.MaxHealth + _snapshot.HealthLevel * _config.HealthPerLevel;
    public float Damage => _config.Damage + _snapshot.DamageLevel * _config.DamagePerLevel;

    public PlayerModel(PlayerConfig config)
    {
        _config = config;
        SaveSystem.Init();
        _snapshot = new Save<PlayerSnapshot>("player", new()).Data;
        _currentHealth = MaxHealth;
    }

    public void ResetHealth()
    {
        _currentHealth = MaxHealth;
        Changed?.Invoke();
    }

    public int GetLevel(UpgradeType type) => type switch
    {
        UpgradeType.Speed  => _snapshot.SpeedLevel,
        UpgradeType.Health => _snapshot.HealthLevel,
        UpgradeType.Damage => _snapshot.DamageLevel,
        _                  => 0
    };

    public int GetMaxLevel(UpgradeType type) => type switch
    {
        UpgradeType.Speed  => _config.MaxSpeedLevel,
        UpgradeType.Health => _config.MaxHealthLevel,
        UpgradeType.Damage => _config.MaxDamageLevel,
        _                  => 0
    };

    public void Heal(float amount)
    {
        if (_currentHealth <= 0f) return;
        _currentHealth = Mathf.Min(_currentHealth + amount, MaxHealth);
        Changed?.Invoke();
    }

    public void TakeDamage(float damage)
    {
        if (_currentHealth <= 0f) return;
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        Changed?.Invoke();
        if (_currentHealth <= 0f)
            Died?.Invoke();
    }

    public void AddUpgradePoint() => AddUpgradePoints(1);

    public void AddUpgradePoints(int count)
    {
        _snapshot.UpgradePoints += count;
        _saveDirty = true;
    }

    public void FlushSave()
    {
        if (!_saveDirty) return;
        _saveDirty = false;
        SaveSystem.SaveSync();
    }

    public void ApplyUpgrades(int[] deltas)
    {
        var totalCost = 0;
        for (var i = 0; i < deltas.Length; i++)
            totalCost += deltas[i];
        
        if (totalCost > UpgradePoints) return;
        
        _snapshot.UpgradePoints -= totalCost;
        _snapshot.SpeedLevel += deltas[(int)UpgradeType.Speed];
        _snapshot.HealthLevel += deltas[(int)UpgradeType.Health];
        _snapshot.DamageLevel += deltas[(int)UpgradeType.Damage];
        
        var healthDelta = deltas[(int)UpgradeType.Health];
        if (healthDelta > 0)
            _currentHealth = Mathf.Min(_currentHealth + healthDelta * _config.HealthPerLevel, MaxHealth);
        
        _saveDirty = true;
        FlushSave();
        Changed?.Invoke();
    }
}