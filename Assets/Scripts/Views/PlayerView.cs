namespace Views;

public sealed class PlayerView : MonoBehaviour, IDamageable
{
    public event Action<float> DamageReceived;
    
    [SerializeField] private Transform _transform;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Collider _hitCollider;
    [SerializeField] private Transform _cameraPoint;
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private ParticleSystem _muzzleFlash;
    
    private Sequence _deathSequence;

    public Transform Transform => _transform;
    public Team Team => Team.Player;
    public CharacterController CharacterController => _characterController;
    public Collider HitCollider => _hitCollider;
    public Transform CameraPoint => _cameraPoint;
    public Transform ShootPoint => _shootPoint;
    public AudioSource AudioSource => _audioSource;
    public bool IsDead { get; private set; }

    public void TakeDamage(float damage) => DamageReceived?.Invoke(damage);

    public void PlayMuzzleFlash() => _muzzleFlash.Play(true);

    public void PlayDeathAnimation()
    {
        IsDead = true;
        _characterController.enabled = false;
        _hitCollider.enabled = false;
        
        _deathSequence?.Kill();
        var fallRotation = _transform.rotation * Quaternion.Euler(90f, 0f, 0f);
        _deathSequence = DOTween.Sequence()
            .Append(_transform.DORotateQuaternion(fallRotation, 0.5f).SetEase(Ease.OutBounce))
            .AppendInterval(0.7f)
            .Append(_transform.DOMoveY(-1.5f, 1f).SetEase(Ease.InCubic))
            .SetLink(gameObject);
    }

    private void OnValidate() => _transform ??= transform;
}