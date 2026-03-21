using Models;
using static Controllers.AudioController;

namespace Controllers;

public interface IPlayerView
{
    PlayerView View { get; }
    bool IsAlive { get; }
}

public interface IPlayerKillReward
{
    void OnEnemyKilled();
}

public sealed class PlayerController : IPlayerView, IPlayerKillReward, ITickable, IDisposable
{
    public event Action PlayerDied;
    public event Action PlayerLanded;
    
    public const float SPAWN_HEIGHT = 10f;
    
    private const int PROJECTILE_PREWARM = 25;
    private const float GROUND_HEIGHT = 1f;
    private const float DROP_DURATION = 1f;
    
    [Inject] private readonly IGameFlow _gameFlow;
    [Inject] private readonly IInput _input;
    [Inject] private readonly IProjectilePool _projectiles;
    [Inject] private readonly IDamageableRegistry _damageableRegistry;
    [Inject] private readonly PlayerConfig _config;
    [Inject] private readonly AudioConfig _audioConfig;
    [Inject] private readonly PlayerModel _model;
    [Inject] private readonly ICameraController _camera;
    [Inject] private readonly MainCanvasView _canvasView;
    
    private PlayerView _view;
    private HpBarView _hpBar;
    private Tween _dropTween;
    private float _fireInterval;
    private float _nextFireTime;
    private bool _skipFirstLook;
    private bool _initialized;
    private bool _controllable;

    public PlayerView View => _view;
    public bool IsAlive => _initialized && _view && !_view.IsDead;
    public float SpawnCameraPitch => _config.InitialCameraPitch;

    public void Init()
    {
        _view = Object.Instantiate(ResourcesLoader.GetPlayer());
        _view.Transform.position = new(0f, SPAWN_HEIGHT, 0f);
        _view.Transform.localScale = Vector3.zero;
        _view.CharacterController.enabled = false;
        
        _hpBar = Object.Instantiate(ResourcesLoader.GetHpBar(), _canvasView.Canvas.transform);
        
        _projectiles.RegisterPool(Team.Player, ResourcesLoader.GetPlayerProjectile(), PROJECTILE_PREWARM);
        
        _fireInterval = 1f / _config.FireRate;
        
        _model.ResetHealth();
        _view.DamageReceived += _model.TakeDamage;
        _view.DamageReceived += OnDamageReceived;
        _model.Changed += UpdateHpBar;
        _model.Died += OnPlayerDied;
        UpdateHpBar();
        
        _controllable = false;
        _skipFirstLook = true;
        _initialized = true;
    }

    public void StartDrop()
    {
        _camera.TrackPlayer(_view, _config.InitialCameraPitch);
        
        var t = _view.Transform;
        var go = _view.gameObject;
        
        _dropTween = DOTween.Sequence()
            .Append(t.DOScale(Vector3.one, DROP_DURATION * 0.4f).SetEase(Ease.OutBack))
            .Append(t.DOMoveY(GROUND_HEIGHT, DROP_DURATION).SetEase(Ease.OutBounce))
            .OnComplete(() =>
            {
                _camera.AttachToPlayer();
                _view.CharacterController.enabled = true;
                _damageableRegistry.Register(_view.HitCollider, _view);
                _controllable = true;
                PlayerLanded?.Invoke();
            })
            .SetLink(go);
    }

    public void Cleanup()
    {
        if (!_initialized) return;
        
        KillDropTween();
        
        if (_view)
        {
            _view.DamageReceived -= _model.TakeDamage;
            _view.DamageReceived -= OnDamageReceived;
            if (!_view.IsDead)
                _damageableRegistry.Unregister(_view.HitCollider);
            Object.Destroy(_view.gameObject);
        }
        
        if (_hpBar)
            Object.Destroy(_hpBar.gameObject);
        
        _model.Changed -= UpdateHpBar;
        _model.Died -= OnPlayerDied;
        _controllable = false;
        _initialized = false;
    }

    public void Tick()
    {
        if (!_gameFlow.IsPlaying || !_initialized || !_controllable || _gameFlow.InputBlocked) return;
        if (_view.IsDead) return;
        
        Move();
        Look();
        Shoot();
    }

    public void OnEnemyKilled() => _model.AddUpgradePoint();

    public void Dispose() => Cleanup();

    private void Move()
    {
        var input = _input.Move;
        if (input.sqrMagnitude < 0.01f) return;
        
        var t = _view.Transform;
        var direction = t.forward * input.y + t.right * input.x;
        direction.y = 0f;
        direction.Normalize();
        
        _view.CharacterController.Move(direction * (_model.MoveSpeed * Time.deltaTime));
    }

    private void Look()
    {
        var look = _input.Look;
        if (look.sqrMagnitude < 0.01f) return;
        
        if (_skipFirstLook)
        {
            _skipFirstLook = false;
            return;
        }
        
        _view.Transform.Rotate(0f, look.x * _config.LookSensitivity * Time.deltaTime, 0f);
    }

    private void Shoot()
    {
        if (!_input.Shoot) return;
        var time = Time.time;
        if (time < _nextFireTime) return;
        
        _nextFireTime = time + _fireInterval;
        
        var shootPoint = _view.ShootPoint;
        _projectiles.Spawn(shootPoint.position, shootPoint.forward, _config.ProjectileSpeed, _model.Damage, Team.Player);
        Trigger(_audioConfig.ShootClip, _view.AudioSource);
        _view.PlayMuzzleFlash();
    }

    private void OnPlayerDied()
    {
        _damageableRegistry.Unregister(_view.HitCollider);
        _view.PlayDeathAnimation();
        _camera.DetachAndFlyOut();
        PlayerDied?.Invoke();
    }

    private void OnDamageReceived(float _) => _camera.Shake();

    private void UpdateHpBar() => _hpBar.Set(_model.CurrentHealth, _model.MaxHealth);

    private void KillDropTween()
    {
        _dropTween?.Kill();
        _dropTween = null;
    }
}