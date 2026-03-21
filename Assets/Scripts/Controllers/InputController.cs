using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;

namespace Controllers;

public interface IInput
{
    Vector2 Move { get; }
    Vector2 Look { get; }
    bool Shoot { get; }
    event Action PausePressed;
}

public sealed class InputController : IInput, IStartable, ITickable, IDisposable
{
    public event Action PausePressed;
    
    private readonly InputActions _actions = new();
    private readonly List<GameObject> _mobileControls = new();
    
    [Inject] private readonly MainCanvasView _canvasView;
    
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _shootAction;
    private Vector2 _move;
    private Vector2 _look;
    private bool _shoot;

    public Vector2 Move => _move;
    public Vector2 Look => _look;
    public bool Shoot => _shoot;

    public void Start()
    {
        _moveAction = _actions.Main.Move;
        _lookAction = _actions.Main.Look;
        _shootAction = _actions.Main.Shoot;
        _actions.Main.Pause.performed += OnPausePerformed;
        _actions.Main.Enable();

#if UNITY_ANDROID || UNITY_IOS
        CreateMobileControls();
#endif
    }

    public void Tick()
    {
        _move = _moveAction.ReadValue<Vector2>();
        _look = _lookAction.ReadValue<Vector2>();
        _shoot = _shootAction.IsPressed();
    }

    public void Dispose()
    {
        _actions.Main.Pause.performed -= OnPausePerformed;
        _actions.Dispose();
        
        for (var i = 0; i < _mobileControls.Count; i++)
            if (_mobileControls[i])
                Object.Destroy(_mobileControls[i]);
        _mobileControls.Clear();
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx) => PausePressed?.Invoke();

    private void CreateMobileControls()
    {
        var safeArea = _canvasView.SafeArea;
        var canvasRect = safeArea.rect;
        var halfWidth = canvasRect.width * 0.5f;
        var bottomY = -canvasRect.height * 0.5f + 200f;

        _mobileControls.Add(CreateStick(safeArea, "<Gamepad>/leftStick", new(-halfWidth + 200f, bottomY)));
        _mobileControls.Add(CreateStick(safeArea, "<Gamepad>/rightStick", new(halfWidth - 200f, bottomY)));
        _mobileControls.Add(CreateShootButton(safeArea, new(halfWidth - 200f, bottomY + 250f)));
    }

    private static GameObject CreateStick(Transform parent, string controlPath, Vector2 position)
    {
        var stickGo = new GameObject("OnScreenStick", typeof(RectTransform));
        var stickRect = (RectTransform)stickGo.transform;
        stickRect.SetParent(parent, false);
        stickRect.anchoredPosition = position;
        stickRect.sizeDelta = new(200f, 200f);
        
        var bgImage = stickGo.AddComponent<Image>();
        bgImage.color = new(1f, 1f, 1f, 0.3f);
        
        var handleGo = new GameObject("Handle", typeof(RectTransform));
        var handleRect = (RectTransform)handleGo.transform;
        handleRect.SetParent(stickRect, false);
        handleRect.sizeDelta = new(80f, 80f);
        
        var handleImage = handleGo.AddComponent<Image>();
        handleImage.color = new(1f, 1f, 1f, 0.6f);
        
        var stick = handleGo.AddComponent<OnScreenStick>();
        stick.controlPath = controlPath;
        stick.movementRange = 20f;
        
        return stickGo;
    }

    private static GameObject CreateShootButton(Transform parent, Vector2 position)
    {
        var buttonGo = new GameObject("ShootButton", typeof(RectTransform));
        var buttonRect = (RectTransform)buttonGo.transform;
        buttonRect.SetParent(parent, false);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new(120f, 120f);
        
        var image = buttonGo.AddComponent<Image>();
        image.color = new(1f, 0.3f, 0.3f, 0.5f);
        
        var button = buttonGo.AddComponent<OnScreenButton>();
        button.controlPath = "<Gamepad>/rightTrigger";
        
        return buttonGo;
    }
}