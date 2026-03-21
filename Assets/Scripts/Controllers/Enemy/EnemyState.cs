namespace Controllers.Enemy;

public struct EnemyState
{
    public EnemyView View;
    public HpBarView HpBar;
    public Transform HpBarTransform;
    public Vector3 CachedPosition;
    public bool IsActive;
    public bool IsFleeing;
    public bool IsDying;
    public bool SeesPlayer;
    public bool HpBarVisible;
    public float DistanceSqrToPlayer;
    public float NextFireTime;
    public float StuckTime;
}