using TMPro;

namespace Views.UI
{
    public sealed class ScorePopupView : MonoBehaviour, IResetable
    {
        private const float DURATION = 2f;
        private const float RISE_HEIGHT = 1.5f;
    
        [SerializeField] private Transform _transform;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _canvasGroup;
    
        private Sequence _sequence;

        public Transform Transform => _transform;
        public bool IsFinished => _sequence == null || !_sequence.IsActive();

        public void Show(int points)
        {
            _text.SetText("+{0}", points);
            _canvasGroup.alpha = 1f;
            this.SetActiveSafe(true);
        
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_transform.DOMoveY(_transform.position.y + RISE_HEIGHT, DURATION).SetEase(Ease.OutCubic))
                .Join(_canvasGroup.DOFade(0f, DURATION).SetEase(Ease.InQuad))
                .SetLink(gameObject);
        }

        public void SetBillboard(Quaternion cameraRotation) => _transform.rotation = cameraRotation;

        public void ResetView()
        {
            _sequence?.Kill();
            _sequence = null;
            _canvasGroup.alpha = 0f;
            this.SetActiveSafe(false);
        }

        private void OnValidate() => _transform ??= transform;
    }
}