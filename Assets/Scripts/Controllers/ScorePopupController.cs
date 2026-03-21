namespace Controllers;

public interface IScorePopup
{
    void Show(Vector3 position, int points);
}

public sealed class ScorePopupController : IScorePopup, ITickable
{
    private const int PREWARM = 10;
    private const float POPUP_OFFSET_Y = 1.8f;
    
    [Inject] private readonly WorldCanvasView _worldCanvas;
    [Inject] private readonly MainCameraView _cameraView;
    
    private readonly List<ScorePopupView> _active = new(PREWARM);
    
    private GameObjectPool<ScorePopupView> _pool;
    private Transform _cameraTransform;
    private bool _initialized;

    public void Init()
    {
        if (_initialized) return;
        var prefab = ResourcesLoader.GetScorePopup();
        _pool = new(prefab, _worldCanvas.transform);
        _pool.Prewarm(PREWARM);
        _cameraTransform = _cameraView.transform;
        _initialized = true;
    }

    public void Show(Vector3 position, int points)
    {
        if (!_initialized) return;
        var popup = _pool.Get();
        popup.Transform.position = new(position.x, position.y + POPUP_OFFSET_Y, position.z);
        popup.SetBillboard(_cameraTransform.rotation);
        popup.Show(points);
        _active.Add(popup);
    }

    public void Tick()
    {
        if (!_initialized || _active.Count == 0) return;
        
        var camRotation = _cameraTransform.rotation;
        
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var popup = _active[i];
            
            if (popup.IsFinished)
            {
                _pool.Return(popup);
                _active.SwapRemoveAt(i);
                continue;
            }
            
            popup.SetBillboard(camRotation);
        }
    }

    public void Cleanup()
    {
        if (!_initialized) return;
        
        for (var i = 0; i < _active.Count; i++)
            _pool.Return(_active[i]);
        
        _active.Clear();
        _pool.DestroyAll();
        _pool = null;
        _initialized = false;
    }
}