namespace Views.UI;

public sealed class PauseMenuButton : MonoBehaviour
{
    [SerializeField] private Button _pauseButton;

    public Button PauseButton => _pauseButton;
}