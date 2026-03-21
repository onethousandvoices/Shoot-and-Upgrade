namespace Data
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Config/Player")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Stats")]
        [SerializeField] private float _moveSpeed = 8f;
        [SerializeField] private float _lookSensitivity = 2f;
        [SerializeField] private float _initialCameraPitch = 15f;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _fireRate = 5f;
        [SerializeField] private float _projectileSpeed = 30f;
    
        [Header("Upgrade stats")]
        [SerializeField] private float _speedPerLevel = 1f;
        [SerializeField] private float _healthPerLevel = 20f;
        [SerializeField] private float _damagePerLevel = 5f;
    
        [Header("Upgrade caps")]
        [SerializeField] private int _maxSpeedLevel = 20;
        [SerializeField] private int _maxHealthLevel = 10;
        [SerializeField] private int _maxDamageLevel = 15;

        public float MoveSpeed => _moveSpeed;
        public float LookSensitivity => _lookSensitivity;
        public float InitialCameraPitch => _initialCameraPitch;
        public float MaxHealth => _maxHealth;
        public float Damage => _damage;
        public float FireRate => _fireRate;
        public float ProjectileSpeed => _projectileSpeed;
        public float SpeedPerLevel => _speedPerLevel;
        public float HealthPerLevel => _healthPerLevel;
        public float DamagePerLevel => _damagePerLevel;
        public int MaxSpeedLevel => _maxSpeedLevel;
        public int MaxHealthLevel => _maxHealthLevel;
        public int MaxDamageLevel => _maxDamageLevel;
    }
}