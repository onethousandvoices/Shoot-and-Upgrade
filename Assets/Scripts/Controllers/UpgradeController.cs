using Models;

namespace Controllers;

public sealed class UpgradeController : IStartable, IDisposable
{
    private static readonly UpgradeType[] _types = (UpgradeType[])Enum.GetValues(typeof(UpgradeType));
    
    [Inject] private readonly PlayerModel _model;
    [Inject] private readonly MainCanvasView _canvasView;
    
    private UpgradeWindowView _windowView;
    private StatRowView[] _rows;
    private UnityEngine.Events.UnityAction[] _plusActions;
    private int[] _pending;
    private int _pendingSpent;

    private int AvailablePoints => _model.UpgradePoints - _pendingSpent;

    public void Start()
    {
        var prefab = ResourcesLoader.GetUpgradeWindow();
        _windowView = Object.Instantiate(prefab, _canvasView.SafeArea);
        
        _pending = new int[_types.Length];
        _rows = new StatRowView[_types.Length];
        _plusActions = new UnityEngine.Events.UnityAction[_types.Length];
        
        for (var i = 0; i < _types.Length; i++)
        {
            var row = Object.Instantiate(_windowView.StatRowPrefab, _windowView.StatsContainer);
            row.Init(_types[i]);
            var index = i;
            _plusActions[i] = () => AddPending(index);
            row.PlusButton.onClick.AddListener(_plusActions[i]);
            _rows[i] = row;
        }
        
        _windowView.ApplyButton.onClick.AddListener(OnApply);
        _windowView.CloseButton.onClick.AddListener(Close);
        _windowView.SetActiveSafe(false);
    }

    public void Dispose()
    {
        if (_rows == null) return;
        
        for (var i = 0; i < _rows.Length; i++)
            _rows[i].PlusButton.onClick.RemoveListener(_plusActions[i]);
        
        _windowView.ApplyButton.onClick.RemoveListener(OnApply);
        _windowView.CloseButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        ResetPending();
        RefreshView();
        _windowView.transform.SetAsLastSibling();
        _windowView.Window.Show();
    }

    private void AddPending(int index)
    {
        if (AvailablePoints <= 0) return;
        if (_model.GetLevel(_types[index]) + _pending[index] >= _model.GetMaxLevel(_types[index])) return;
        _pending[index]++;
        _pendingSpent++;
        RefreshView();
    }

    private void OnApply()
    {
        _model.ApplyUpgrades(_pending);
        Close();
    }

    private void Close()
    {
        ResetPending();
        _windowView.Window.Hide();
    }

    private void ResetPending()
    {
        Array.Clear(_pending, 0, _pending.Length);
        _pendingSpent = 0;
    }

    private void RefreshView()
    {
        var points = AvailablePoints;
        _windowView.SetPoints(points);
        var interactable = points > 0;
        
        for (var i = 0; i < _rows.Length; i++)
        {
            var type = _types[i];
            var maxLevel = _model.GetMaxLevel(type);
            var currentLevel = _model.GetLevel(type) + _pending[i];
            var row = _rows[i];
            row.SetLevel(currentLevel, maxLevel);
            row.SetPlusInteractable(interactable && currentLevel < maxLevel);
        }
    }
}