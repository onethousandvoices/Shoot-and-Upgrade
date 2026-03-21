using System.Collections;

namespace Utilities;

public sealed class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (_instance)
            Destroy(_instance.gameObject);
        _instance = null;
    }

    private static CoroutineRunner Instance
    {
        get
        {
            if (_instance) return _instance;
            var go = new GameObject("[CoroutineRunner]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CoroutineRunner>();
            return _instance;
        }
    }

    public static Coroutine Run(IEnumerator routine) => Instance.StartCoroutine(routine);

    public static void Stop(Coroutine coroutine)
    {
        if (_instance)
            _instance.StopCoroutine(coroutine);
    }
}