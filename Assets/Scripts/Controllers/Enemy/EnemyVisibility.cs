using Unity.Collections;
using Unity.Jobs;

namespace Controllers.Enemy;

public sealed class EnemyVisibility
{
    private static readonly QueryParameters _query = new(Layers.OBSTACLE_MASK);
    private static readonly RaycastCommand _noop = new(Vector3.zero, Vector3.forward, _query, 0f);
    
    private readonly float _rangeSqr;
    
    private NativeArray<RaycastCommand> _commands;
    private NativeArray<RaycastHit> _results;
    private JobHandle _handle;
    private int _scheduledCount;

    public EnemyVisibility(float visibilityRange) => _rangeSqr = visibilityRange * visibilityRange;

    public void Init(int count)
    {
        _commands = new(count, Allocator.Persistent);
        _results = new(count, Allocator.Persistent);
    }

    public void Schedule(EnemyState[] enemies, int count, Vector3 playerPos)
    {
        _scheduledCount = count;
        var commands = _commands.GetSubArray(0, count);
        
        for (var i = 0; i < count; i++)
        {
            ref var state = ref enemies[i];
            var view = state.View;
            if (state.IsDying || !view)
            {
                commands[i] = _noop;
                state.DistanceSqrToPlayer = float.MaxValue;
                continue;
            }
            
            var enemyPos = view.Transform.position;
            var direction = playerPos - enemyPos;
            var sqrMag = direction.sqrMagnitude;
            state.DistanceSqrToPlayer = sqrMag;
            
            if (sqrMag > _rangeSqr || sqrMag < 0.000001f)
            {
                commands[i] = _noop;
                continue;
            }
            
            var distance = Mathf.Sqrt(sqrMag);
            commands[i] = new(
                new(enemyPos.x, enemyPos.y + 1f, enemyPos.z),
                direction / distance,
                _query,
                distance
            );
        }
        
        _handle = RaycastCommand.ScheduleBatch(commands, _results.GetSubArray(0, count), 32);
    }

    public void Complete(EnemyState[] enemies)
    {
        _handle.Complete();
        var results = _results.GetSubArray(0, _scheduledCount);
        
        for (var i = 0; i < _scheduledCount; i++)
        {
            ref var state = ref enemies[i];
            if (state.IsDying) continue;
            state.SeesPlayer = state.DistanceSqrToPlayer <= _rangeSqr && !results[i].collider;
        }
    }

    public void Dispose()
    {
        _handle.Complete();
        if (_commands.IsCreated) _commands.Dispose();
        if (_results.IsCreated) _results.Dispose();
    }
}