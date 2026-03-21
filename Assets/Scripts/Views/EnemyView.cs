using Pathfinding;
using Random = UnityEngine.Random;

namespace Views;

public sealed class EnemyView : MonoBehaviour, IDamageable, IResetable
{
    public event Action<EnemyView> Died;
    
    [SerializeField] private Transform _transform;
    [SerializeField] private AIPath _aiPath;
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private Collider _hitCollider;
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private ParticleSystem _muzzleFlash;
    
    private float _maxHealth;
    private float _currentHealth;
    private bool _hpDirty;
    private bool _hitDirty;
    private int _index;
    private Sequence _deathSequence;

    public Transform Transform => _transform;
    public Team Team => Team.Enemy;
    public AIPath AiPath => _aiPath;
    public MeshRenderer MeshRenderer => _meshRenderer;
    public Collider HitCollider => _hitCollider;
    public Transform ShootPoint => _shootPoint;
    public AudioSource AudioSource => _audioSource;
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public int Index => _index;

    public bool ConsumeHpDirty() => ConsumeFlag(ref _hpDirty);

    public bool ConsumeHitDirty() => ConsumeFlag(ref _hitDirty);

    public void PlayMuzzleFlash() => _muzzleFlash.Play(true);

    public void Init(float maxHealth, float speed, int index)
    {
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;
        _hpDirty = false;
        _hitDirty = false;
        _index = index;
        _aiPath.maxSpeed = speed;
    }

    public void TakeDamage(float damage)
    {
        if (_currentHealth <= 0f) return;
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        _hpDirty = true;
        _hitDirty = true;
        if (_currentHealth <= 0f)
            Died?.Invoke(this);
    }

    public void PlayDeathAnimation(Vector3 playerPos, Action onComplete)
    {
        _deathSequence?.Kill();
        
        _aiPath.enabled = false;
        _hitCollider.enabled = false;
        
        var pos = _transform.position;
        var rot = _transform.rotation;
        
        var away = pos - playerPos;
        var awayFromPlayer = away.sqrMagnitude > 0.001f ? away.normalized : _transform.forward;
        var fallAngle = Random.Range(-30f, 30f);
        var fallDir = Quaternion.AngleAxis(fallAngle, Vector3.up) * awayFromPlayer;
        var right = Vector3.Cross(Vector3.up, fallDir).normalized;
        var twist = Random.Range(-15f, 15f);
        var fallRotation = Quaternion.AngleAxis(twist, fallDir) * Quaternion.AngleAxis(90f, right) * rot;
        var hitOffset = Random.Range(0.3f, 0.6f);
        var hitPos = new Vector3(pos.x + fallDir.x * hitOffset, 1.15f, pos.z + fallDir.z * hitOffset);
        var groundPos = new Vector3(hitPos.x + fallDir.x * 0.2f, 0.5f, hitPos.z + fallDir.z * 0.2f);
        var bounceRotation = Quaternion.AngleAxis(Random.Range(3f, 8f), right) * fallRotation;
        
        _deathSequence = DOTween.Sequence()
            .Append(_transform.DOMove(hitPos, 0.15f).SetEase(Ease.OutQuad))
            .Join(_transform.DORotateQuaternion(fallRotation, 0.15f).SetEase(Ease.OutQuad))
            .Append(_transform.DOMove(groundPos, 0.25f).SetEase(Ease.InQuad))
            .Join(_transform.DORotateQuaternion(bounceRotation, 0.25f).SetEase(Ease.InQuad))
            .Append(_transform.DOMoveY(0.4f, 0.1f).SetEase(Ease.OutQuad))
            .Join(_transform.DORotateQuaternion(fallRotation, 0.1f).SetEase(Ease.InOutQuad))
            .Append(_transform.DOMoveY(0.5f, 0.06f).SetEase(Ease.InQuad))
            .AppendInterval(0.7f)
            .Append(_transform.DOMoveY(-1.5f, 1f).SetEase(Ease.InCubic))
            .OnComplete(onComplete.Invoke)
            .SetLink(gameObject);
    }

    public void ResetView()
    {
        _deathSequence?.Kill();
        _deathSequence = null;
        _currentHealth = _maxHealth;
        _hpDirty = false;
        _hitDirty = false;
        _hitCollider.enabled = true;
        _transform.rotation = Quaternion.identity;
        _aiPath.enabled = true;
        _aiPath.isStopped = true;
        _aiPath.canSearch = false;
        this.SetActiveSafe(false);
    }

    private static bool ConsumeFlag(ref bool flag)
    {
        if (!flag) return false;
        flag = false;
        return true;
    }

    private void OnValidate() => _transform ??= transform;
}