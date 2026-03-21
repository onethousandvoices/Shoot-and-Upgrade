namespace Data
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Config/Camera")]
    public sealed class CameraConfig : ScriptableObject
    {
        [Header("Menu")]
        [SerializeField] private float _menuHeight = 40f;
        [SerializeField] private float _menuLookAngle = 90f;
        [SerializeField] private float _menuDriftSpeed = 8f;
        [SerializeField] private float _menuDriftMargin = 0.4f;
    
        [Header("Shake")]
        [SerializeField] private float _shakeDuration = 0.3f;
        [SerializeField] private float _shakeStrength = 0.15f;
    
        [Header("Transitions")]
        [SerializeField] private float _flyInDuration = 1.5f;
        [SerializeField] private float _flyOutDuration = 1.5f;
        [SerializeField] private float _flyOutHeight = 50f;
        [SerializeField] private float _spawnViewHeight = 5f;
        [SerializeField] private float _spawnViewOffset = 10f;
        [SerializeField] private float _trackingSmoothing = 3f;
        [SerializeField] private float _attachSmoothing = 8f;

        public float ShakeDuration => _shakeDuration;
        public float ShakeStrength => _shakeStrength;
        public float MenuHeight => _menuHeight;
        public float MenuLookAngle => _menuLookAngle;
        public float MenuDriftSpeed => _menuDriftSpeed;
        public float MenuDriftMargin => _menuDriftMargin;
        public float FlyInDuration => _flyInDuration;
        public float FlyOutDuration => _flyOutDuration;
        public float FlyOutHeight => _flyOutHeight;
        public float SpawnViewHeight => _spawnViewHeight;
        public float SpawnViewOffset => _spawnViewOffset;
        public float TrackingSmoothing => _trackingSmoothing;
        public float AttachSmoothing => _attachSmoothing;
    }
}