using Views.UI.Animations;

namespace Views.UI;

public sealed class AnimatedWindow : MonoBehaviour
{
    [SerializeField] private WindowAnimation[] _animations;
    
    private Sequence _sequence;
    private TweenCallback _cachedShowCallback;
    private TweenCallback _cachedHideCallback;
    private Action _pendingCallback;

    public void Show(Action onComplete = null)
    {
        KillSequence();
        this.SetActiveSafe(true);
        
        for (var i = 0; i < _animations.Length; i++)
            _animations[i].PrepareShow();
        
        _sequence = DOTween.Sequence();
        for (var i = 0; i < _animations.Length; i++)
            _animations[i].AddShow(_sequence);
        
        _pendingCallback = onComplete;
        _cachedShowCallback ??= OnShowCompleteWithCallback;
        _sequence.SetUpdate(true).SetLink(gameObject).OnComplete(_cachedShowCallback);
    }

    public void Hide(Action onComplete = null)
    {
        KillSequence();
        
        for (var i = 0; i < _animations.Length; i++)
            _animations[i].PrepareHide();
        
        _sequence = DOTween.Sequence();
        for (var i = 0; i < _animations.Length; i++)
            _animations[i].AddHide(_sequence);
        
        _pendingCallback = onComplete;
        _cachedHideCallback ??= OnHideCompleteWithCallback;
        _sequence.SetUpdate(true).SetLink(gameObject).OnComplete(_cachedHideCallback);
    }

    private void OnDestroy() => KillSequence();

    private void OnShowCompleteWithCallback()
    {
        for (var i = 0; i < _animations.Length; i++)
            _animations[i].CompleteShow();
        var cb = _pendingCallback;
        _pendingCallback = null;
        cb?.Invoke();
    }

    private void OnHideCompleteWithCallback()
    {
        this.SetActiveSafe(false);
        var cb = _pendingCallback;
        _pendingCallback = null;
        cb?.Invoke();
    }

    private void KillSequence()
    {
        _sequence?.Kill();
        _sequence = null;
    }
}