using System.Reflection;
using Controllers;
using Models;

namespace MainScene;

public sealed class MainSceneLifetimeScope : LifetimeScope
{
    private const string CONFIGS_PATH = "Configs";
    private const BindingFlags INSTANCE_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    
    private static readonly Type _lateInjectAttribute = typeof(LateInjectAttribute);
    private static readonly Dictionary<Type, FieldInfo[]> _lateInjectCache = new();
    
    private readonly List<Type> _registeredTypes = new(16);
    
    [SerializeField] private MainCameraView _mainCameraView;
    [SerializeField] private MainCanvasView _mainCanvasView;
    [SerializeField] private WorldCanvasView _worldCanvasView;

    protected override void Configure(IContainerBuilder builder)
    {
        _registeredTypes.Clear();
        
        builder.RegisterComponent(_mainCameraView);
        builder.RegisterComponent(_mainCanvasView);
        builder.RegisterComponent(_worldCanvasView);
        
        RegisterConfigs(builder);
        
        builder.Register<PlayerModel>(Lifetime.Singleton);
        
        RegisterSingleton<InputController>(builder);
        RegisterSingleton<ProjectileController>(builder);
        RegisterSingleton<UpgradeController>(builder);
        RegisterSingleton<LocationController>(builder);
        RegisterSingleton<CameraController>(builder);
        RegisterSingleton<PlayerController>(builder);
        RegisterSingleton<ScorePopupController>(builder);
        RegisterSingleton<EnemyController>(builder);
        RegisterSingleton<GameFlowController>(builder);
        
        builder.RegisterBuildCallback(InitStaticSystems);
        builder.RegisterBuildCallback(LateInjectDependencies);
    }

    private void RegisterSingleton<T>(IContainerBuilder builder) where T : class
    {
        _registeredTypes.Add(typeof(T));
        builder.Register<T>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
    }

    private static void RegisterConfigs(IContainerBuilder builder)
    {
        var configs = Resources.LoadAll<ScriptableObject>(CONFIGS_PATH);
        for (var i = 0; i < configs.Length; i++)
            builder.RegisterInstance(configs[i], configs[i].GetType());
    }

    private static void InitStaticSystems(IObjectResolver resolver)
    {
        LocalizationController.Init(resolver.Resolve<LocalizationConfig>());
        AudioController.Init(resolver.Resolve<AudioConfig>());
    }

    private void LateInjectDependencies(IObjectResolver resolver)
    {
        var dependencyCache = new Dictionary<Type, object>();
        
        foreach (var type in _registeredTypes)
        {
            if (!_lateInjectCache.TryGetValue(type, out var lateInjectFields))
            {
                var fields = type.GetFields(INSTANCE_BINDING_FLAGS);
                List<FieldInfo> lateInjects = null;
                
                for (var i = 0; i < fields.Length; i++)
                {
                    if (!fields[i].IsDefined(_lateInjectAttribute, true)) continue;
                    lateInjects ??= new(4);
                    lateInjects.Add(fields[i]);
                }
                
                lateInjectFields = lateInjects?.ToArray();
                _lateInjectCache[type] = lateInjectFields;
            }
            
            if (lateInjectFields == null) continue;
            if (!resolver.TryResolve(type, out var instance)) continue;
            
            for (var i = 0; i < lateInjectFields.Length; i++)
            {
                var fieldType = lateInjectFields[i].FieldType;
                if (!dependencyCache.TryGetValue(fieldType, out var dependency))
                {
                    dependency = resolver.Resolve(fieldType);
                    dependencyCache.Add(fieldType, dependency);
                }
                
                lateInjectFields[i].SetValue(instance, dependency);
            }
        }
    }
}