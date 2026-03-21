namespace Views;

public sealed class ProjectileView : MonoBehaviour, IResetable
{
    [SerializeField] private Transform _transform;
    
    private Vector3 _direction;
    private float _speed;
    private float _damage;
    private Team _team;

    public Transform Transform => _transform;
    public Vector3 Direction => _direction;
    public float Speed => _speed;
    public float Damage => _damage;
    public Team Team => _team;

    public void Init(Vector3 direction, float speed, float damage, Team team)
    {
        _direction = direction;
        _speed = speed;
        _damage = damage;
        _team = team;
        this.SetActiveSafe(true);
    }

    public void ResetView() => this.SetActiveSafe(false);

    private void OnValidate() => _transform ??= transform;
}