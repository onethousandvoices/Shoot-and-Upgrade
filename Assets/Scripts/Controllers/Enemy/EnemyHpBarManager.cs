namespace Controllers.Enemy;

public sealed class EnemyHpBarManager
{
    private const float HP_BAR_OFFSET_Y = 1.5f;
    
    private readonly Transform _cameraTransform;

    public EnemyHpBarManager(Transform cameraTransform) => _cameraTransform = cameraTransform;

    public void Update(EnemyState[] enemies, int count)
    {
        var camRotation = _cameraTransform.rotation;
        
        for (var i = 0; i < count; i++)
        {
            ref var state = ref enemies[i];
            if (state.IsDying) continue;
            var view = state.View;
            if (!view) continue;
            
            if (view.ConsumeHpDirty())
            {
                state.HpBar.SetActiveSafe(true);
                state.HpBarVisible = true;
                state.HpBar.Set(view.CurrentHealth, view.MaxHealth);
            }
            
            if (!state.HpBarVisible) continue;
            
            var pos = state.CachedPosition;
            pos.y += HP_BAR_OFFSET_Y;
            state.HpBarTransform.SetPositionAndRotation(pos, camRotation);
        }
    }
}