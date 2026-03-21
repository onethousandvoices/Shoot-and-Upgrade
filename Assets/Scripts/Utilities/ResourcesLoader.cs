namespace Utilities;

public static class ResourcesLoader
{
    private const string PREFABS = "Prefabs/";
    private const string MATERIALS = "Materials/";
    
    private static MainLocationView _location;
    private static PlayerView _player;
    private static ProjectileView _playerProjectile;
    private static ProjectileView _enemyProjectile;
    private static EnemyView _enemy;
    private static HpBarView _hpBar;
    private static UpgradeWindowView _upgradeWindow;
    private static MainMenuView _mainMenu;
    private static PauseMenuView _pauseMenu;
    private static LostMenuView _lostMenu;
    private static PauseMenuButton _pauseButton;
    private static Material _redMaterial;
    private static Material _greyMaterial;
    private static Material _yellowMaterial;
    private static VfxView _hitImpact;
    private static ScorePopupView _scorePopup;
    private static Material _whiteMaterial;
    private static Texture2D[] _wallPatterns;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _location = null;
        _player = null;
        _playerProjectile = null;
        _enemyProjectile = null;
        _enemy = null;
        _hpBar = null;
        _upgradeWindow = null;
        _mainMenu = null;
        _pauseMenu = null;
        _lostMenu = null;
        _pauseButton = null;
        _scorePopup = null;
        _hitImpact = null;
        _redMaterial = null;
        _greyMaterial = null;
        _yellowMaterial = null;
        _whiteMaterial = null;
        _wallPatterns = null;
    }

    public static MainLocationView GetLocation() => _location ??= Resources.Load<MainLocationView>(PREFABS + "Location/Location");
    public static PlayerView GetPlayer() => _player ??= Resources.Load<PlayerView>(PREFABS + "Player/PlayerView");
    public static ProjectileView GetPlayerProjectile() => _playerProjectile ??= Resources.Load<ProjectileView>(PREFABS + "Player/PlayerProjectile");
    public static ProjectileView GetEnemyProjectile() => _enemyProjectile ??= Resources.Load<ProjectileView>(PREFABS + "Enemies/EnemyProjectile");
    public static EnemyView GetEnemy() => _enemy ??= Resources.Load<EnemyView>(PREFABS + "Enemies/EnemyView");
    public static HpBarView GetHpBar() => _hpBar ??= Resources.Load<HpBarView>(PREFABS + "UI/HpBar");
    public static UpgradeWindowView GetUpgradeWindow() => _upgradeWindow ??= Resources.Load<UpgradeWindowView>(PREFABS + "UI/UpgradeWindow");
    public static MainMenuView GetMainMenu() => _mainMenu ??= Resources.Load<MainMenuView>(PREFABS + "UI/MainMenu");
    public static PauseMenuView GetPauseMenu() => _pauseMenu ??= Resources.Load<PauseMenuView>(PREFABS + "UI/PauseMenu");
    public static LostMenuView GetLostMenu() => _lostMenu ??= Resources.Load<LostMenuView>(PREFABS + "UI/LostMenu");
    public static PauseMenuButton GetPauseButton() => _pauseButton ??= Resources.Load<PauseMenuButton>(PREFABS + "UI/PauseButton");
    public static ScorePopupView GetScorePopup() => _scorePopup ??= Resources.Load<ScorePopupView>(PREFABS + "UI/ScorePopup");
    public static VfxView GetHitImpact() => _hitImpact ??= Resources.Load<VfxView>(PREFABS + "VFX/HitImpact");
    public static Material GetRedMaterial() => _redMaterial ??= Resources.Load<Material>(MATERIALS + "Red");
    public static Material GetGreyMaterial() => _greyMaterial ??= Resources.Load<Material>(MATERIALS + "Grey");
    public static Material GetYellowMaterial() => _yellowMaterial ??= Resources.Load<Material>(MATERIALS + "Yellow");
    public static Material GetWhiteMaterial() => _whiteMaterial ??= Resources.Load<Material>(MATERIALS + "White");
    public static Texture2D[] GetWallPatterns() => _wallPatterns ??= Resources.LoadAll<Texture2D>("WallsPatterns");
}