using Pathfinding;
using static Controllers.AudioController;

namespace Controllers.Enemy;

public sealed class EnemyAI
{
    private const float AIM_DOT_THRESHOLD = 0.98f;
    private const float AIM_ROTATION_SPEED = 720f;
    private const float STUCK_SPEED_THRESHOLD_SQR = 0.25f;
    private const float STUCK_TIME_LIMIT = 1.5f;
    
    private readonly EnemyConfig _config;
    private readonly AudioConfig _audioConfig;
    private readonly IProjectilePool _projectiles;
    private readonly Material _greyMaterial;
    private readonly Material _redMaterial;
    private readonly Material _yellowMaterial;
    private readonly float _attackRadiusSqr;
    private readonly float _fireInterval;
    private readonly float _fleeRepositionDistance;

    public EnemyAI(
        EnemyConfig config,
        AudioConfig audioConfig,
        IProjectilePool projectiles,
        Material greyMaterial,
        Material redMaterial,
        Material yellowMaterial)
    {
        _config = config;
        _audioConfig = audioConfig;
        _projectiles = projectiles;
        _greyMaterial = greyMaterial;
        _redMaterial = redMaterial;
        _yellowMaterial = yellowMaterial;
        _attackRadiusSqr = config.AttackRadius * config.AttackRadius;
        _fireInterval = 1f / config.FireRate;
        _fleeRepositionDistance = config.FleeRepositionDistance;
    }

    public void Update(EnemyState[] enemies, int count, Vector3 playerPos)
    {
        var time = Time.time;
        var dt = Time.deltaTime;
        var fleeThreshold = _config.FleeHealthThreshold;
        
        for (var i = 0; i < count; i++)
        {
            ref var state = ref enemies[i];
            if (state.IsDying) continue;
            var view = state.View;
            if (!view) continue;
            
            var enemyPos = view.Transform.position;
            state.CachedPosition = enemyPos;
            var toPlayer = playerPos - enemyPos;
            var distSqr = state.DistanceSqrToPlayer;
            
            var ai = view.AiPath;
            
            if (!state.IsFleeing && view.CurrentHealth <= view.MaxHealth * fleeThreshold)
            {
                state.IsFleeing = true;
                state.IsActive = false;
                state.StuckTime = 0f;
                view.MeshRenderer.sharedMaterial = _yellowMaterial;
                ai.maxSpeed = _config.FleeSpeed;
                ai.canSearch = true;
                ai.isStopped = false;
            }
            
            if (state.IsFleeing)
            {
                UpdateFlee(ai, enemyPos, playerPos);
                continue;
            }
            
            var wasHit = view.ConsumeHitDirty();
            
            if (!state.IsActive && (wasHit || state.SeesPlayer && distSqr <= _attackRadiusSqr))
            {
                state.IsActive = true;
                state.StuckTime = 0f;
                view.MeshRenderer.sharedMaterial = _redMaterial;
                ai.canSearch = true;
                ai.destination = playerPos;
            }
            
            if (state.IsActive)
            {
                var inRange = distSqr <= _attackRadiusSqr && state.SeesPlayer;
                
                if (inRange)
                {
                    ai.isStopped = true;
                    
                    if (!RotateToward(view, toPlayer, dt, out var flatDir) || time < state.NextFireTime)
                        continue;
                    Shoot(view, flatDir);
                    state.NextFireTime = time + _fireInterval;
                }
                else
                {
                    ai.isStopped = false;
                    if ((ai.destination - playerPos).sqrMagnitude > 9f)
                        ai.destination = playerPos;
                }
            }
            else
                UpdateWander(ref state, view, dt);
        }
    }

    public void UpdatePatrol(EnemyState[] enemies, int count, float dt)
    {
        for (var i = 0; i < count; i++)
        {
            ref var state = ref enemies[i];
            if (state.IsDying) continue;
            var view = state.View;
            if (!view) continue;
            
            UpdateWander(ref state, view, dt);
        }
    }

    public void ResetAllToWander(EnemyState[] enemies, int count)
    {
        for (var i = 0; i < count; i++)
        {
            ref var state = ref enemies[i];
            if (state.IsDying) continue;
            var view = state.View;
            if (!view) continue;
            
            state.IsActive = false;
            state.IsFleeing = false;
            state.SeesPlayer = false;
            view.MeshRenderer.sharedMaterial = _greyMaterial;
            view.AiPath.maxSpeed = _config.MoveSpeed;
            view.AiPath.isStopped = false;
            StartWander(view);
        }
    }

    public void StartWander(EnemyView view)
    {
        var searchLength = Mathf.RoundToInt(_config.WanderDistance * 1000f);
        var path = RandomPath.Construct(view.Transform.position, searchLength);
        path.spread = 5000;
        view.AiPath.canSearch = false;
        view.AiPath.SetPath(path);
    }

    private void UpdateFlee(AIPath ai, Vector3 enemyPos, Vector3 playerPos)
    {
        var away = enemyPos - playerPos;
        var sqrMag = away.sqrMagnitude;
        
        if (sqrMag <= 0.001f)
            return;
        var fleeTarget = enemyPos + away * (_fleeRepositionDistance / Mathf.Sqrt(sqrMag));
        if ((ai.destination - fleeTarget).sqrMagnitude > 1f)
            ai.destination = fleeTarget;
    }

    private void UpdateWander(ref EnemyState state, EnemyView view, float dt)
    {
        var ai = view.AiPath;
        
        if (!ai.pathPending && (ai.reachedEndOfPath || ai.isStopped || !ai.hasPath))
        {
            state.StuckTime = 0f;
            StartWander(view);
            return;
        }
        
        if (ai.velocity.sqrMagnitude < STUCK_SPEED_THRESHOLD_SQR)
        {
            state.StuckTime += dt;
            if (state.StuckTime < STUCK_TIME_LIMIT)
                return;
            state.StuckTime = 0f;
            StartWander(view);
        }
        else
            state.StuckTime = 0f;
    }

    private void Shoot(EnemyView view, Vector3 direction)
    {
        _projectiles.Spawn(view.ShootPoint.position, direction, _config.ProjectileSpeed, _config.Damage, Team.Enemy);
        Trigger(_audioConfig.ShootClip, view.AudioSource);
        view.PlayMuzzleFlash();
    }

    private static bool RotateToward(EnemyView view, Vector3 toPlayer, float dt, out Vector3 flatDir)
    {
        var t = view.Transform;
        flatDir = new(toPlayer.x, 0f, toPlayer.z);
        var sqr = flatDir.sqrMagnitude;
        if (sqr < 0.001f)
        {
            flatDir = t.forward;
            return true;
        }
        
        flatDir /= Mathf.Sqrt(sqr);
        t.rotation = Quaternion.RotateTowards(t.rotation, Quaternion.LookRotation(flatDir), AIM_ROTATION_SPEED * dt);
        
        return Vector3.Dot(t.forward, flatDir) >= AIM_DOT_THRESHOLD;
    }
}