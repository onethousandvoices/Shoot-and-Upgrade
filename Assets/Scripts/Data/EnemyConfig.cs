namespace Data;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Config/Enemy")]
public sealed class EnemyConfig : ScriptableObject
{
    [Header("Population")]
    [SerializeField] private int _totalCount = 50;
    
    [Header("Stats")]
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private int _minHealthShots = 1;
    [SerializeField] private int _maxHealthShots = 10;
    [SerializeField] private float _damage = 5f;
    [SerializeField] private float _fireRate = 1f;
    [SerializeField] private float _projectileSpeed = 20f;
    
    [Header("Spawn")]
    [SerializeField] private float _spawnRadiusMin = 30f;
    [SerializeField] private float _spawnRadiusMax = 120f;
    [SerializeField] private float _respawnRadiusMin = 40f;
    
    [Header("Behavior")]
    [SerializeField] private float _attackRadius = 20f;
    [SerializeField] private float _visibilityRange = 40f;
    [SerializeField] private float _wanderDistance = 15f;
    
    [Header("Flee")]
    [SerializeField] private float _fleeHealthThreshold = 0.25f;
    [SerializeField] private float _fleeSpeed = 6f;
    [SerializeField] private float _fleeRepositionDistance = 30f;

    public int TotalCount => _totalCount;
    public float MoveSpeed => _moveSpeed;
    public int MinHealthShots => _minHealthShots;
    public int MaxHealthShots => _maxHealthShots;
    public float Damage => _damage;
    public float FireRate => _fireRate;
    public float ProjectileSpeed => _projectileSpeed;
    public float SpawnRadiusMin => _spawnRadiusMin;
    public float SpawnRadiusMax => _spawnRadiusMax;
    public float RespawnRadiusMin => _respawnRadiusMin;
    public float AttackRadius => _attackRadius;
    public float VisibilityRange => _visibilityRange;
    public float WanderDistance => _wanderDistance;
    public float FleeHealthThreshold => _fleeHealthThreshold;
    public float FleeSpeed => _fleeSpeed;
    public float FleeRepositionDistance => _fleeRepositionDistance;
}