namespace Views.UI
{
    public sealed class MainCanvasView : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _safeArea;

        public Canvas Canvas => _canvas;
        public RectTransform SafeArea => _safeArea;
    }
}