namespace Views.UI;

public sealed class MainCanvasView : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;

    public Canvas Canvas => _canvas;
}