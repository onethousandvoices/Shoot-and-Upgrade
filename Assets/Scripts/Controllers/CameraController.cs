using DG.Tweening.Core;
using Random = UnityEngine.Random;

namespace Controllers;

public interface ICameraController
{
    event Action FlyInComplete;
    void StartMenuMode(float locationSize);
    void FlyToPlayer(Vector3 targetPosition, Quaternion targetRotation);
    void TrackPlayer(PlayerView player, float initialPitch);
    void AttachToPlayer();
    void DetachAndFlyOut();
    void Shake();
}

public sealed class CameraController : ICameraController, ITickable, IDisposable
{
    public event Action FlyInComplete;
    
    [Inject] private readonly MainCameraView _cameraView;
    [Inject] private readonly CameraConfig _config;
    
    private DOGetter<Vector3> _shakeGetter;
    private DOSetter<Vector3> _shakeSetter;
    private Transform _cameraTransform;
    private PlayerView _attachedPlayer;
    private float _cameraPitch;
    private float _halfDriftArea;
    private float _currentSmoothing;
    private Tween _flyTween;
    private Tween _driftTween;
    private Tween _shakeTween;
    private Vector3 _shakeOffset;
    private CameraMode _mode;

    public void StartMenuMode(float locationSize)
    {
        _cameraTransform = _cameraView.transform;
        _halfDriftArea = locationSize * _config.MenuDriftMargin;
        
        if (_mode == CameraMode.Menu)
            return;
        
        KillFlyTween();
        KillDriftTween();
        _attachedPlayer = null;
        
        var menuPos = new Vector3(0f, _config.MenuHeight, 0f);
        var menuRot = Quaternion.Euler(_config.MenuLookAngle, 0f, 0f);
        
        if (_mode == CameraMode.None)
        {
            _cameraTransform.SetPositionAndRotation(menuPos, menuRot);
            _mode = CameraMode.Menu;
            DriftToNextWaypoint();
            return;
        }
        
        _mode = CameraMode.FlyingOut;
        _flyTween = DOTween.Sequence()
            .Append(_cameraTransform.DOMove(menuPos, _config.FlyOutDuration).SetEase(Ease.InOutQuad))
            .Join(_cameraTransform.DORotateQuaternion(menuRot, _config.FlyOutDuration).SetEase(Ease.InOutQuad))
            .OnComplete(() =>
            {
                _mode = CameraMode.Menu;
                DriftToNextWaypoint();
            })
            .SetLink(_cameraView.gameObject);
    }

    public void FlyToPlayer(Vector3 targetPosition, Quaternion targetRotation)
    {
        KillDriftTween();
        KillFlyTween();
        _mode = CameraMode.FlyingIn;
        
        var duration = _config.FlyInDuration;
        _flyTween = DOTween.Sequence()
            .Append(_cameraTransform.DOMove(targetPosition, duration).SetEase(Ease.InOutQuad))
            .Join(_cameraTransform.DORotateQuaternion(targetRotation, duration).SetEase(Ease.InOutQuad))
            .OnComplete(() =>
            {
                _mode = CameraMode.Idle;
                FlyInComplete?.Invoke();
            })
            .SetLink(_cameraView.gameObject);
    }

    public void TrackPlayer(PlayerView player, float initialPitch)
    {
        _attachedPlayer = player;
        _cameraPitch = initialPitch;
        _currentSmoothing = _config.TrackingSmoothing;
        _mode = CameraMode.Tracking;
    }

    public void AttachToPlayer() => _currentSmoothing = _config.AttachSmoothing;

    public void DetachAndFlyOut()
    {
        _attachedPlayer = null;
        KillFlyTween();
        _mode = CameraMode.FlyingOut;
        
        var currentPos = _cameraTransform.position;
        var targetPos = new Vector3(currentPos.x, _config.FlyOutHeight, currentPos.z);
        var duration = _config.FlyOutDuration;
        var lookAngle = _config.MenuLookAngle;
        
        _flyTween = DOTween.Sequence()
            .Append(_cameraTransform.DOMove(targetPos, duration).SetEase(Ease.InOutQuad))
            .Join(_cameraTransform.DORotateQuaternion(Quaternion.Euler(lookAngle, 0f, 0f), duration).SetEase(Ease.InOutQuad))
            .OnComplete(() =>
            {
                _mode = CameraMode.Menu;
                DriftToNextWaypoint();
            })
            .SetLink(_cameraView.gameObject);
    }

    public void Tick()
    {
        if (_mode == CameraMode.Tracking)
            UpdateTracking();
    }

    public void Dispose()
    {
        KillFlyTween();
        KillDriftTween();
        KillShakeTween();
    }

    public void Shake()
    {
        KillShakeTween();
        _shakeOffset = Vector3.zero;
        _shakeGetter ??= () => _shakeOffset;
        _shakeSetter ??= x => _shakeOffset = x;
        _shakeTween = DOTween.Shake(_shakeGetter, _shakeSetter,
                _config.ShakeDuration, _config.ShakeStrength, fadeOut: true)
            .SetLink(_cameraView.gameObject);
    }

    private void DriftToNextWaypoint()
    {
        KillDriftTween();
        
        var target = new Vector3(
            Random.Range(-_halfDriftArea, _halfDriftArea),
            _config.MenuHeight,
            Random.Range(-_halfDriftArea, _halfDriftArea)
        );
        
        var offset = target - _cameraTransform.position;
        var duration = Mathf.Sqrt(offset.sqrMagnitude) / _config.MenuDriftSpeed;
        
        _driftTween = _cameraTransform.DOMove(target, duration)
            .SetEase(Ease.InOutSine)
            .OnComplete(DriftToNextWaypoint)
            .SetLink(_cameraView.gameObject);
    }

    private void UpdateTracking()
    {
        if (!_attachedPlayer) return;
        var targetPos = _attachedPlayer.CameraPoint.position;
        var yaw = _attachedPlayer.Transform.eulerAngles.y;
        var targetRot = Quaternion.Euler(_cameraPitch, yaw, 0f);
        var t = Mathf.Min(_currentSmoothing * Time.deltaTime, 1f);
        var currentPos = _cameraTransform.position;
        var currentRot = _cameraTransform.rotation;
        _cameraTransform.SetPositionAndRotation(
            Vector3.Lerp(currentPos, targetPos, t) + _shakeOffset,
            Quaternion.Slerp(currentRot, targetRot, t));
    }

    private void KillFlyTween()
    {
        _flyTween?.Kill();
        _flyTween = null;
    }

    private void KillDriftTween()
    {
        _driftTween?.Kill();
        _driftTween = null;
    }

    private void KillShakeTween()
    {
        _shakeTween?.Kill();
        _shakeTween = null;
        _shakeOffset = Vector3.zero;
    }

    private enum CameraMode : byte
    {
        None,
        Menu,
        FlyingIn,
        Tracking,
        FlyingOut,
        Idle
    }
}