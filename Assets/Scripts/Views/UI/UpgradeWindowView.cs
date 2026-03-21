using TMPro;

namespace Views.UI
{
    public sealed class UpgradeWindowView : MonoBehaviour
    {
        [SerializeField] private AnimatedWindow _window;
        [SerializeField] private TMP_Text _pointsText;
        [SerializeField] private Transform _statsContainer;
        [SerializeField] private StatRowView _statRowPrefab;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _closeButton;

        public AnimatedWindow Window => _window;
        public Transform StatsContainer => _statsContainer;
        public StatRowView StatRowPrefab => _statRowPrefab;
        public Button ApplyButton => _applyButton;
        public Button CloseButton => _closeButton;

        public void SetPoints(int points) => _pointsText.SetText("{0}", points);
    }
}