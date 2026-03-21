namespace Views.UI
{
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private AnimatedWindow _window;
        [SerializeField] private Button _returnToMainMenuButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _closeButton;

        public AnimatedWindow Window => _window;
        public Button ReturnToMainMenuButton => _returnToMainMenuButton;
        public Button UpgradeButton => _upgradeButton;
        public Button CloseButton => _closeButton;
    }
}