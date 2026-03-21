using Pathfinding;
using Random = UnityEngine.Random;

namespace Controllers.Enemy;

public readonly record struct RadiusRange(float Min, float Max, float MinSqr, float MaxSqr);

public sealed class EnemySpawner
{
    private const float SPAWN_HEIGHT = 1f;
    private const float EXCLUSION_RADIUS = 20f;
    
    private readonly EnemyConfig _config;
    private readonly PlayerConfig _playerConfig;
    private readonly IDamageableRegistry _damageableRegistry;
    private readonly Transform _worldCanvasTransform;
    private readonly GameObjectPool<EnemyView> _pool;
    private readonly HpBarView _hpBarPrefab;
    private readonly Material _greyMaterial;
    private readonly RadiusRange _spawnRange;
    private readonly RadiusRange _respawnRange;

    public EnemySpawner(
        EnemyConfig config,
        PlayerConfig playerConfig,
        IDamageableRegistry damageableRegistry,
        Transform worldCanvasTransform,
        GameObjectPool<EnemyView> pool,
        HpBarView hpBarPrefab,
        Material greyMaterial)
    {
        _config = config;
        _playerConfig = playerConfig;
        _damageableRegistry = damageableRegistry;
        _worldCanvasTransform = worldCanvasTransform;
        _pool = pool;
        _hpBarPrefab = hpBarPrefab;
        _greyMaterial = greyMaterial;
        
        var maxSqr = config.SpawnRadiusMax * config.SpawnRadiusMax;
        _spawnRange = new(config.SpawnRadiusMin, config.SpawnRadiusMax, config.SpawnRadiusMin * config.SpawnRadiusMin, maxSqr);
        _respawnRange = new(config.RespawnRadiusMin, config.SpawnRadiusMax, config.RespawnRadiusMin * config.RespawnRadiusMin, maxSqr);
    }

    public EnemyState[] SpawnAll(EnemyAI ai, Action<EnemyView> onDied)
    {
        var count = _config.TotalCount;
        var enemies = new EnemyState[count];
        
        for (var i = 0; i < count; i++)
        {
            var enemy = _pool.Get();
            enemy.Transform.position = FindWalkablePosition(Vector3.zero, _spawnRange);
            enemy.Init(RollHealth(), _config.MoveSpeed, i);
            _damageableRegistry.Register(enemy.HitCollider, enemy);
            
            var hpBar = Object.Instantiate(_hpBarPrefab, _worldCanvasTransform);
            hpBar.SetActiveSafe(false);
            
            enemies[i] = new()
            {
                View = enemy,
                HpBar = hpBar,
                HpBarTransform = hpBar.transform
            };
            
            enemy.AiPath.canSearch = false;
            enemy.AiPath.isStopped = false;
            enemy.MeshRenderer.sharedMaterial = _greyMaterial;
            ai.StartWander(enemy);
            
            enemy.Died += onDied;
        }
        
        return enemies;
    }

    public void Respawn(ref EnemyState state, int index, EnemyView view, Vector3 center, EnemyAI ai)
    {
        view.ResetView();
        
        var pos = FindWalkablePosition(center, _respawnRange);
        
        view.Transform.position = pos;
        view.SetActiveSafe(true);
        view.Init(RollHealth(), _config.MoveSpeed, index);
        _damageableRegistry.Register(view.HitCollider, view);
        
        state.IsActive = false;
        state.IsFleeing = false;
        state.IsDying = false;
        state.SeesPlayer = false;
        state.NextFireTime = 0f;
        state.StuckTime = 0f;
        
        view.AiPath.Teleport(pos, false);
        view.AiPath.isStopped = false;
        view.MeshRenderer.sharedMaterial = _greyMaterial;
        state.HpBar.SetActiveSafe(false);
        state.HpBarVisible = false;
        ai.StartWander(view);
    }

    public static void SetExclusionZone(bool walkable)
    {
        if (!AstarPath.active) return;
        var bounds = new Bounds(Vector3.zero, new(EXCLUSION_RADIUS * 2f, 10f, EXCLUSION_RADIUS * 2f));
        var guo = new GraphUpdateObject(bounds) { modifyWalkability = true, setWalkability = walkable };
        AstarPath.active.UpdateGraphs(guo);
        AstarPath.active.FlushWorkItems();
    }

    private float RollHealth() => Random.Range(_config.MinHealthShots, _config.MaxHealthShots + 1) * _playerConfig.Damage;

    private static Vector3 FindWalkablePosition(Vector3 awayFrom, RadiusRange range)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var dist = Random.Range(range.Min, range.Max);
            var candidate = new Vector3(
                awayFrom.x + Mathf.Cos(angle) * dist,
                0f,
                awayFrom.z + Mathf.Sin(angle) * dist
            );
            
            var info = AstarPath.active.GetNearest(candidate, NearestNodeConstraint.Walkable);
            if (info.node is not { Walkable: true }) continue;
            
            var pos = (Vector3)info.node.position;
            pos.y = SPAWN_HEIGHT;
            var offsetSqr = (pos - awayFrom).sqrMagnitude;
            if (offsetSqr >= range.MinSqr && offsetSqr <= range.MaxSqr)
                return pos;
        }
        
        var fallback = AstarPath.active.GetNearest(awayFrom + Random.insideUnitSphere * range.Max, NearestNodeConstraint.Walkable);
        if (fallback.node == null) return awayFrom;
        var fallbackPos = (Vector3)fallback.node.position;
        fallbackPos.y = SPAWN_HEIGHT;
        return fallbackPos;
    }
}