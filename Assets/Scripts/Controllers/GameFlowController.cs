using System.Collections;
using Models;

namespace Controllers;

public interface IGameFlow
{
    bool IsPlaying { get; }
    bool InputBlocked { get; }
}

public sealed class GameFlowController : IGameFlow, IStartable, IDisposable
{
    private const float LOST_MENU_DELAY = 1f;
    
    private static readonly WaitForSeconds _lostMenuWait = new(LOST_MENU_DELAY);
    
    [Inject] private readonly MainCanvasView _canvasView;
    [Inject] private readonly IInput _input;
    [Inject] private readonly UpgradeController _upgradeController;
    [Inject] private readonly ICameraController _camera;
    [Inject] private readonly CameraConfig _cameraConfig;
    [Inject] private readonly IProjectilePool _projectiles;
    [Inject] private readonly PlayerModel _model;
    [LateInject] private readonly LocationController _locationController;
    [LateInject] private readonly PlayerController _playerController;
    [LateInject] private readonly EnemyController _enemyController;
    [Inject] private readonly ScorePopupController _scorePopup;
    
    private MainMenuView _mainMenu;
    private PauseMenuView _pauseMenu;
    private PauseMenuButton _pauseButton;
    private LostMenuView _lostMenu;
    private Coroutine _lostCoroutine;
    private bool _isPlaying;
    private bool _isPaused;
    private bool _isTransitioning;
    private bool _playerSubscribed;

    public bool IsPlaying => _isPlaying;
    public bool InputBlocked => _isTransitioning || _isPaused;

    public void Start()
    {
        var safeArea = _canvasView.SafeArea;
        _mainMenu = Object.Instantiate(ResourcesLoader.GetMainMenu(), safeArea);
        _pauseMenu = Object.Instantiate(ResourcesLoader.GetPauseMenu(), safeArea);
        _lostMenu = Object.Instantiate(ResourcesLoader.GetLostMenu(), safeArea);

        _mainMenu.PlayButton.onClick.AddListener(Play);
        _mainMenu.UpgradeButton.onClick.AddListener(OpenUpgrades);
        _mainMenu.ExitButton.onClick.AddListener(Exit);

        _pauseMenu.ReturnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        _pauseMenu.UpgradeButton.onClick.AddListener(OpenUpgrades);
        _pauseMenu.CloseButton.onClick.AddListener(Resume);

        _pauseButton = Object.Instantiate(ResourcesLoader.GetPauseButton(), safeArea);
        _pauseButton.PauseButton.onClick.AddListener(OnPausePressed);
        
        _lostMenu.ReturnToMainMenuButton.onClick.AddListener(ReturnToMainMenuFromLost);
        _lostMenu.ExitButton.onClick.AddListener(Exit);
        
        _input.PausePressed += OnPausePressed;
        
        _pauseMenu.SetActiveSafe(false);
        _lostMenu.SetActiveSafe(false);
        _pauseButton.SetActiveSafe(false);
        
        _scorePopup.Init();
        _locationController.Init(_enemyController.Init);
        
        _camera.StartMenuMode(_locationController.Size);
        _camera.FlyInComplete += OnCameraFlyInComplete;
        
        SetCursorFree();
        _mainMenu.Window.Show();
    }

    public void Dispose()
    {
        _model.FlushSave();
        StopLostCoroutine();
        _input.PausePressed -= OnPausePressed;
        _camera.FlyInComplete -= OnCameraFlyInComplete;
        UnsubscribePlayer();
        
        _mainMenu.PlayButton.onClick.RemoveListener(Play);
        _mainMenu.UpgradeButton.onClick.RemoveListener(OpenUpgrades);
        _mainMenu.ExitButton.onClick.RemoveListener(Exit);
        
        _pauseMenu.ReturnToMainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        _pauseMenu.UpgradeButton.onClick.RemoveListener(OpenUpgrades);
        _pauseMenu.CloseButton.onClick.RemoveListener(Resume);
        
        _pauseButton.PauseButton.onClick.RemoveListener(OnPausePressed);
        
        _lostMenu.ReturnToMainMenuButton.onClick.RemoveListener(ReturnToMainMenuFromLost);
        _lostMenu.ExitButton.onClick.RemoveListener(Exit);
    }

    private void Play()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        LockCursor();
        
        _mainMenu.Window.Hide(() =>
        {
            var camHeight = PlayerController.SPAWN_HEIGHT + _cameraConfig.SpawnViewHeight;
            var spawnPos = new Vector3(0f, camHeight, -_cameraConfig.SpawnViewOffset);
            var spawnRot = Quaternion.Euler(_playerController.SpawnCameraPitch, 0f, 0f);
            _camera.FlyToPlayer(spawnPos, spawnRot);
        });
    }

    private void OnCameraFlyInComplete()
    {
        _isPlaying = true;
        _playerController.Init();
        SubscribePlayer();
        _enemyController.SetPlayerView(_playerController.View);
        _playerController.StartDrop();
    }

    private void OnPlayerLanded()
    {
        _isTransitioning = false;
        _pauseButton.SetActiveSafe(true);
    }

    private void OnPlayerDied()
    {
        _isPlaying = false;
        _pauseButton.SetActiveSafe(false);
        StopLostCoroutine();
        _lostCoroutine = CoroutineRunner.Run(ShowLostMenuDelayed());
    }

    private IEnumerator ShowLostMenuDelayed()
    {
        yield return _lostMenuWait;
        SetCursorFree();
        _lostMenu.Window.Show();
    }

    private void OpenUpgrades() => _upgradeController.Open();

    private static void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnPausePressed()
    {
        if (!_isPlaying || _isTransitioning) return;
        if (!_playerController.IsAlive) return;
        
        if (_isPaused)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        _isPaused = true;
        _isTransitioning = true;
        _pauseButton.SetActiveSafe(false);
        Time.timeScale = 0f;
        SetCursorFree();
        _pauseMenu.Window.Show(() => _isTransitioning = false);
    }

    private void Resume()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        LockCursor();
        
        _pauseMenu.Window.Hide(() =>
        {
            _isPaused = false;
            _isTransitioning = false;
            Time.timeScale = 1f;
            _pauseButton.SetActiveSafe(true);
        });
    }

    private void ReturnToMainMenu()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        
        _pauseMenu.Window.Hide(() =>
        {
            _isPaused = false;
            Time.timeScale = 1f;
            CleanupPlayer();
            InitGameSystems();
            ShowMainMenu();
        });
    }

    private void ReturnToMainMenuFromLost()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        
        StopLostCoroutine();
        
        _lostMenu.Window.Hide(() =>
        {
            CleanupPlayer();
            InitGameSystems();
            ShowMainMenu();
        });
    }

    private void CleanupPlayer()
    {
        _pauseButton.SetActiveSafe(false);
        _model.FlushSave();
        UnsubscribePlayer();
        _playerController.Cleanup();
        _projectiles.ReturnAll();
        _scorePopup.Cleanup();
        _enemyController.Cleanup();
    }

    private void ShowMainMenu()
    {
        _isPlaying = false;
        _camera.StartMenuMode(_locationController.Size);
        SetCursorFree();
        _mainMenu.Window.Show(() => _isTransitioning = false);
    }

    private void InitGameSystems()
    {
        _scorePopup.Init();
        _enemyController.Init();
    }

    private void SubscribePlayer()
    {
        if (_playerSubscribed) return;
        _playerSubscribed = true;
        _playerController.PlayerDied += OnPlayerDied;
        _playerController.PlayerLanded += OnPlayerLanded;
    }

    private void UnsubscribePlayer()
    {
        if (!_playerSubscribed) return;
        _playerSubscribed = false;
        _playerController.PlayerDied -= OnPlayerDied;
        _playerController.PlayerLanded -= OnPlayerLanded;
    }

    private void StopLostCoroutine()
    {
        if (_lostCoroutine == null) return;
        CoroutineRunner.Stop(_lostCoroutine);
        _lostCoroutine = null;
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void SetCursorFree()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}