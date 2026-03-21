namespace Data
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/Audio")]
    public sealed class AudioConfig : ScriptableObject
    {
        [Header("Clips")]
        [SerializeField] private AudioClip _shootClip;
        [SerializeField] private AudioClip _hitClip;
    
        [Header("Throttle")]
        [SerializeField] private float _throttleCooldown = 0.05f;
        [SerializeField] private int _maxPerFrame = 2;

        public AudioClip ShootClip => _shootClip;
        public AudioClip HitClip => _hitClip;
        public float ThrottleCooldown => _throttleCooldown;
        public int MaxPerFrame => _maxPerFrame;
    }
}