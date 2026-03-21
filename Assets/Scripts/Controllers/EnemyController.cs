using Controllers.Enemy;

namespace Controllers;

public sealed class EnemyController : ITickable, IDisposable
{
    private const int PROJECTILE_PREWARM = 50;
    
    [Inject] private readonly IGameFlow _gameFlow;
    [Inject] private readonly EnemyConfig _config;
    [Inject] private readonly PlayerConfig _playerConfig;
    [Inject] private readonly AudioConfig _audioConfig;
    [Inject] private readonly IPlayerKillReward _playerKillReward;
    [Inject] private readonly IProjectilePool _projectiles;
    [Inject] private readonly IDamageableRegistry _damageableRegistry;
    [Inject] private readonly WorldCanvasView _worldCanvas;
    [Inject] private readonly MainCameraView _cameraView;
    [Inject] private readonly IScorePopup _scorePopup;
    
    private EnemyState[] _enemies;
    private Action[] _respawnCallbacks;
    private PlayerView _playerView;
    private EnemySpawner _spawner;
    private EnemyVisibility _visibility;
    private EnemyAI _ai;
    private EnemyHpBarManager _hpBars;
    private GameObject _holderGo;
    private int _enemyCount;
    private bool _initialized;
    private bool _hasPlayer;

    public void Init()
    {
        if (_initialized) return;
        
        var greyMaterial = ResourcesLoader.GetGreyMaterial();
        
        _ai = new(
            _config, _audioConfig, _projectiles,
            greyMaterial, ResourcesLoader.GetRedMaterial(), ResourcesLoader.GetYellowMaterial());
        
        _projectiles.RegisterPool(Team.Enemy, ResourcesLoader.GetEnemyProjectile(), PROJECTILE_PREWARM);
        
        _holderGo = new("Enemies");
        var pool = new GameObjectPool<EnemyView>(ResourcesLoader.GetEnemy(), _holderGo.transform);
        
        _spawner = new(
            _config, _playerConfig, _damageableRegistry,
            _worldCanvas.transform, pool, ResourcesLoader.GetHpBar(), greyMaterial);
        
        _visibility = new(_config.VisibilityRange);
        _hpBars = new(_cameraView.transform);
        
        EnemySpawner.SetExclusionZone(false);
        _enemies = _spawner.SpawnAll(_ai, OnEnemyDied);
        _enemyCount = _enemies.Length;
        _respawnCallbacks = new Action[_enemyCount];
        for (var i = 0; i < _enemyCount; i++)
        {
            var idx = i;
            _respawnCallbacks[i] = () => OnRespawn(idx);
        }
        _visibility.Init(_enemyCount);
        _initialized = true;
    }

    public void SetPlayerView(PlayerView view)
    {
        _playerView = view;
        _hasPlayer = view;
        
        if (_hasPlayer)
            EnemySpawner.SetExclusionZone(true);
        else
        {
            EnemySpawner.SetExclusionZone(false);
            if (_enemies != null)
                _ai.ResetAllToWander(_enemies, _enemyCount);
        }
    }

    public void Tick()
    {
        if (!_initialized) return;
        
        if (!_hasPlayer)
        {
            _ai.UpdatePatrol(_enemies, _enemyCount, Time.deltaTime);
            return;
        }
        
        if (!_gameFlow.IsPlaying) return;
        
        var playerPos = _playerView.Transform.position;
        _visibility.Schedule(_enemies, _enemyCount, playerPos);
        _visibility.Complete(_enemies);
        _ai.Update(_enemies, _enemyCount, playerPos);
        _hpBars.Update(_enemies, _enemyCount);
    }

    public void Cleanup()
    {
        if (!_initialized) return;
        
        _playerView = null;
        _hasPlayer = false;
        EnemySpawner.SetExclusionZone(false);
        _visibility.Dispose();
        
        if (_enemies != null)
        {
            for (var i = 0; i < _enemyCount; i++)
            {
                ref var state = ref _enemies[i];
                var view = state.View;
                if (!view) continue;
                view.Died -= OnEnemyDied;
                if (!state.IsDying)
                    _damageableRegistry.Unregister(view.HitCollider);
                
                if (state.HpBar)
                    Object.Destroy(state.HpBar.gameObject);
            }
            
            _enemies = null;
            _respawnCallbacks = null;
            _enemyCount = 0;
        }
        
        if (_holderGo)
            Object.Destroy(_holderGo);
        
        _holderGo = null;
        _spawner = null;
        _visibility = null;
        _ai = null;
        _hpBars = null;
        _initialized = false;
    }

    public void Dispose()
    {
        Cleanup();
        _visibility?.Dispose();
    }

    private void OnEnemyDied(EnemyView view)
    {
        var index = view.Index;
        ref var state = ref _enemies[index];
        state.IsDying = true;
        state.HpBar.SetActiveSafe(false);
        state.HpBarVisible = false;
        _damageableRegistry.Unregister(view.HitCollider);
        
        if (_hasPlayer)
        {
            _playerKillReward.OnEnemyKilled();
            _scorePopup.Show(view.Transform.position, 1);
        }
        
        var playerPos = _hasPlayer ? _playerView.Transform.position : view.Transform.position;
        view.PlayDeathAnimation(playerPos, _respawnCallbacks[index]);
    }

    private void OnRespawn(int index)
    {
        if (!_initialized || _enemies == null) return;
        
        var view = _enemies[index].View;
        if (!view) return;
        
        var center = _hasPlayer ? _playerView.Transform.position : Vector3.zero;
        _spawner.Respawn(ref _enemies[index], index, view, center, _ai);
    }
}