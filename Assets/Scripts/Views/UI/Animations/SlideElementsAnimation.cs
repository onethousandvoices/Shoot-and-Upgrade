namespace Views.UI.Animations;

public sealed class SlideElementsAnimation : WindowAnimation
{
    [SerializeField] private RectTransform[] _elements;
    [SerializeField] private float _offset = 200f;
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private float _stagger = 0.1f;
    [SerializeField] private Ease _ease = Ease.OutBack;
    
    private float[] _originalY;

    public override void PrepareShow()
    {
        CacheOriginalPositions();
        for (var i = 0; i < _elements.Length; i++)
        {
            var pos = _elements[i].anchoredPosition;
            _elements[i].anchoredPosition = new(pos.x, _originalY[i] - _offset);
        }
    }

    public override void AddShow(Sequence sequence)
    {
        for (var i = 0; i < _elements.Length; i++)
            sequence.Insert(i * _stagger,
                _elements[i].DOAnchorPosY(_originalY[i], _duration).SetEase(_ease));
    }

    public override void ResetImmediate()
    {
        CacheOriginalPositions();
        for (var i = 0; i < _elements.Length; i++)
        {
            var pos = _elements[i].anchoredPosition;
            _elements[i].anchoredPosition = new(pos.x, _originalY[i]);
        }
    }

    private void CacheOriginalPositions()
    {
        if (_originalY != null) return;
        _originalY = new float[_elements.Length];
        for (var i = 0; i < _elements.Length; i++)
            _originalY[i] = _elements[i].anchoredPosition.y;
    }
}