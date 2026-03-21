namespace Views.UI
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private AnimatedWindow _window;
        [SerializeField] private Button _play;
        [SerializeField] private Button _upgrade;
        [SerializeField] private Button _exit;

        public AnimatedWindow Window => _window;
        public Button PlayButton => _play;
        public Button UpgradeButton => _upgrade;
        public Button ExitButton => _exit;
    }
}