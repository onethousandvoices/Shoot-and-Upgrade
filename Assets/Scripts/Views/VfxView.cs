namespace Views
{
    public sealed class VfxView : MonoBehaviour, IResetable
    {
        [SerializeField] private Transform _transform;
        [SerializeField] private ParticleSystem _particleSystem;
        [SerializeField] private AudioSource _audioSource;

        public Transform Transform => _transform;
        public ParticleSystem ParticleSystem => _particleSystem;
        public AudioSource AudioSource => _audioSource;

        public void Play()
        {
            this.SetActiveSafe(true);
            _particleSystem.Play(true);
        }

        public void ResetView()
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            this.SetActiveSafe(false);
        }

        private void OnValidate()
        {
            _transform ??= transform;
            _particleSystem ??= GetComponent<ParticleSystem>();
        }
    }
}