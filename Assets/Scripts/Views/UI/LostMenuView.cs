namespace Views.UI;

public sealed class LostMenuView : MonoBehaviour
{
    [SerializeField] private AnimatedWindow _window;
    [SerializeField] private Button _returnToMainMenu;
    [SerializeField] private Button _exit;

    public AnimatedWindow Window => _window;
    public Button ReturnToMainMenuButton => _returnToMainMenu;
    public Button ExitButton => _exit;
}