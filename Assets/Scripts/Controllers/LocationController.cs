using System.Collections;

namespace Controllers;

public sealed class LocationController
{
    private static readonly WaitForSeconds _scanDelay = new(0.1f);
    
    private MainLocationView _view;
    private GameObject _wallsGo;

    public float Size => _view ? _view.Size : 0f;

    public void Init(Action onScanComplete = null)
    {
        if (_view) return;
        var prefab = ResourcesLoader.GetLocation();
        _view = Object.Instantiate(prefab);
        
        SetupGridGraph(_view);
        
        _wallsGo = new("Walls");
        var wallGenerator = new WallGenerator(_wallsGo.transform);
        wallGenerator.Generate(_view.Size);
        
        CoroutineRunner.Run(ScanDelayed(onScanComplete));
    }

    private IEnumerator ScanDelayed(Action onComplete)
    {
        yield return _scanDelay;
        if (_view)
            _view.AstarPath.Scan();
        onComplete?.Invoke();
    }

    private static void SetupGridGraph(MainLocationView view)
    {
        var grid = view.AstarPath.data.gridGraph;
        if (grid == null) return;
        
        var nodes = Mathf.RoundToInt(view.Size / grid.nodeSize);
        grid.SetDimensions(nodes, nodes, grid.nodeSize);
    }
}