namespace Views.UI.Animations;

public sealed class ScaleAnimation : WindowAnimation
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _duration = 0.3f;
    [SerializeField] private Ease _showEase = Ease.OutBack;
    [SerializeField] private Ease _hideEase = Ease.InBack;

    public override void PrepareShow() => _target.localScale = Vector3.zero;

    public override void PrepareHide() => _target.localScale = Vector3.one;

    public override void AddShow(Sequence sequence) =>
        sequence.Join(_target.DOScale(1f, _duration).SetEase(_showEase));
    
    public override void AddHide(Sequence sequence) =>
        sequence.Join(_target.DOScale(0f, _duration).SetEase(_hideEase));
    
    public override void ResetImmediate() => _target.localScale = Vector3.one;
}