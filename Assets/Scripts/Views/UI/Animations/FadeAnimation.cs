namespace Views.UI.Animations;

public sealed class FadeAnimation : WindowAnimation
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _duration = 0.25f;

    public override void PrepareShow()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
    }

    public override void PrepareHide() => _canvasGroup.interactable = false;

    public override void CompleteShow() => _canvasGroup.interactable = true;

    public override void AddShow(Sequence sequence) => sequence.Join(_canvasGroup.DOFade(1f, _duration));

    public override void AddHide(Sequence sequence) => sequence.Join(_canvasGroup.DOFade(0f, _duration));

    public override void ResetImmediate()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
    }

    private void OnValidate() => _canvasGroup ??= GetComponent<CanvasGroup>();
}