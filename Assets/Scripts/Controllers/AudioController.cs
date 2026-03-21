namespace Controllers;

public static class AudioController
{
    private static readonly Dictionary<AudioClip, ThrottleState> _throttleStates = new();
    
    private static AudioConfig _config;
    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _throttleStates.Clear();
        _config = null;
        _initialized = false;
    }

    public static void Init(AudioConfig config)
    {
        if (_initialized) return;
        _config = config;
        _initialized = true;
    }

    public static void Trigger(AudioClip clip, AudioSource source)
    {
        if (!_initialized || !clip || !source) return;
        if (!CanPlay(clip)) return;
        source.PlayOneShot(clip);
    }

    private static bool CanPlay(AudioClip clip)
    {
        var time = Time.time;
        var frame = Time.frameCount;
        
        if (_throttleStates.TryGetValue(clip, out var state))
        {
            if (time - state.LastTime < _config.ThrottleCooldown)
                return false;
            
            if (state.LastFrame != frame)
            {
                state.FrameCount = 0;
                state.LastFrame = frame;
            }
            
            if (state.FrameCount >= _config.MaxPerFrame) return false;
            
            state.LastTime = time;
            state.FrameCount++;
            _throttleStates[clip] = state;
            return true;
        }
        
        _throttleStates[clip] = new()
        {
            LastTime = time,
            LastFrame = frame,
            FrameCount = 1
        };
        return true;
    }

    private struct ThrottleState
    {
        public float LastTime;
        public int LastFrame;
        public int FrameCount;
    }
}