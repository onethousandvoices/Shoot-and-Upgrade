using static Controllers.AudioController;

namespace Controllers;

public interface IProjectilePool
{
    void RegisterPool(Team team, ProjectileView prefab, int prewarm);
    void Spawn(Vector3 position, Vector3 direction, float speed, float damage, Team team);
    void ReturnAll();
}

public interface IDamageableRegistry
{
    void Register(Collider collider, IDamageable damageable);
    void Unregister(Collider collider);
}

public sealed class ProjectileController : IProjectilePool, IDamageableRegistry, ITickable, IDisposable
{
    private const float CAST_RADIUS = 0.3f;
    private const float MAX_LIFETIME = 5f;
    private const int TEAM_COUNT = 2;
    private const int VFX_PREWARM = 10;
    private const int HIT_MASK = 1 << 0 | Layers.OBSTACLE_MASK;
    
    private static readonly RaycastHit[] _hitBuffer = new RaycastHit[8];
    
    [Inject] private readonly AudioConfig _audioConfig;
    
    private readonly Dictionary<Collider, IDamageable> _damageableCache = new();
    private readonly GameObjectPool<ProjectileView>[] _pools = new GameObjectPool<ProjectileView>[TEAM_COUNT];
    private readonly List<ProjectileView> _active = new();
    private readonly List<float> _spawnTimes = new();
    private readonly List<VfxView> _activeVfx = new();
    
    private GameObjectPool<VfxView> _hitVfxPool;
    private Transform _holder;

    public void RegisterPool(Team team, ProjectileView prefab, int prewarm)
    {
        if (_pools[(int)team] != null) return;
        _holder ??= new GameObject("Projectiles").transform;
        var pool = new GameObjectPool<ProjectileView>(prefab, _holder);
        pool.Prewarm(prewarm);
        _pools[(int)team] = pool;
        
        if (_hitVfxPool != null) return;
        var vfxPrefab = ResourcesLoader.GetHitImpact();
        if (!vfxPrefab) return;
        _hitVfxPool = new(vfxPrefab, _holder);
        _hitVfxPool.Prewarm(VFX_PREWARM);
    }

    public void Spawn(Vector3 position, Vector3 direction, float speed, float damage, Team team)
    {
        var projectile = _pools[(int)team].Get();
        var t = projectile.Transform;
        t.position = position;
        t.rotation = Quaternion.LookRotation(direction);
        projectile.Init(direction, speed, damage, team);
        _active.Add(projectile);
        _spawnTimes.Add(Time.time);
    }

    public void Register(Collider collider, IDamageable damageable) => _damageableCache[collider] = damageable;

    public void Unregister(Collider collider) => _damageableCache.Remove(collider);

    public void Dispose()
    {
        ReturnAll();
        _damageableCache.Clear();
    }

    public void Tick()
    {
        var time = Time.time;
        var dt = Time.deltaTime;
        
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var p = _active[i];
            if (!p)
            {
                SwapRemove(i);
                continue;
            }
            
            if (time - _spawnTimes[i] >= MAX_LIFETIME)
            {
                ReturnProjectile(i, p);
                continue;
            }
            
            var t = p.Transform;
            var oldPos = t.position;
            var moveDistance = p.Speed * dt;
            
            var hitCount = Physics.SphereCastNonAlloc(oldPos, CAST_RADIUS, p.Direction, _hitBuffer, moveDistance, HIT_MASK);
            
            if (ProcessHits(p, hitCount, out var hitInfo))
            {
                t.position = hitInfo.point;
                SpawnHitVfx(hitInfo);
                ReturnProjectile(i, p);
            }
            else
                t.position = oldPos + p.Direction * moveDistance;
        }
        
        ReturnFinishedVfx();
    }

    private bool ProcessHits(ProjectileView projectile, int hitCount, out RaycastHit closestHit)
    {
        closestHit = default;
        var closestDist = float.MaxValue;
        var closestIndex = -1;
        
        for (var j = 0; j < hitCount; j++)
        {
            ref var hit = ref _hitBuffer[j];
            if (hit.distance <= 0f && hit.point == Vector3.zero) continue;
            if (hit.distance >= closestDist) continue;
            
            var col = hit.collider;
            if (col.gameObject.layer == Layers.OBSTACLE)
            {
                closestDist = hit.distance;
                closestHit = hit;
                closestIndex = j;
                continue;
            }
            
            if (!_damageableCache.TryGetValue(col, out var target)) continue;
            if (target.Team == projectile.Team) continue;
            
            closestDist = hit.distance;
            closestHit = hit;
            closestIndex = j;
        }
        
        if (closestIndex < 0) return false;
        
        var closestCol = closestHit.collider;
        if (closestCol.gameObject.layer != Layers.OBSTACLE
            && _damageableCache.TryGetValue(closestCol, out var closestTarget))
            closestTarget.TakeDamage(projectile.Damage);
        
        return true;
    }

    private void SpawnHitVfx(RaycastHit hit)
    {
        if (_hitVfxPool == null) return;
        
        var position = hit.point;
        var normal = hit.normal;
        var rotation = normal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;
        
        var vfx = _hitVfxPool.Get();
        var t = vfx.Transform;
        t.SetParent(_holder);
        t.SetPositionAndRotation(position, rotation);
        t.localScale = Vector3.one;
        vfx.Play();
        Trigger(_audioConfig.HitClip, vfx.AudioSource);
        _activeVfx.Add(vfx);
    }

    private void ReturnFinishedVfx()
    {
        for (var i = _activeVfx.Count - 1; i >= 0; i--)
        {
            var vfx = _activeVfx[i];
            
            if (!vfx)
            {
                _activeVfx.SwapRemoveAt(i);
                continue;
            }
            
            if (vfx.ParticleSystem.isPlaying) continue;
            ReturnVfx(vfx);
            _activeVfx.SwapRemoveAt(i);
        }
    }

    private void ReturnVfx(VfxView vfx) => _hitVfxPool.Return(vfx);

    private void ReturnProjectile(int index, ProjectileView projectile)
    {
        SwapRemove(index);
        _pools[(int)projectile.Team].Return(projectile);
    }

    public void ReturnAll()
    {
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var p = _active[i];
            if (p)
                _pools[(int)p.Team].Return(p);
        }
        
        _active.Clear();
        _spawnTimes.Clear();
        
        if (_hitVfxPool == null) return;
        for (var i = _activeVfx.Count - 1; i >= 0; i--)
            if (_activeVfx[i])
                ReturnVfx(_activeVfx[i]);
        _activeVfx.Clear();
    }

    private void SwapRemove(int index)
    {
        var last = _active.Count - 1;
        if (index < last)
        {
            _active[index] = _active[last];
            _spawnTimes[index] = _spawnTimes[last];
        }
        
        _active.RemoveAt(last);
        _spawnTimes.RemoveAt(last);
    }
}