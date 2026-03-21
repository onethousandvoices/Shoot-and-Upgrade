using JetBrains.Annotations;

namespace Utilities
{
    public static class CodeUtilities
    {
        public static void SetActiveSafe(this GameObject go, bool state)
        {
            if (!go) return;
            if (go.activeSelf != state)
                go.SetActive(state);
        }

        public static void SetActiveSafe(this Component co, bool state)
        {
            if (co) co.gameObject.SetActiveSafe(state);
        }

        public static void SwapRemoveAt<T>(this List<T> list, int index)
        {
            var last = list.Count - 1;
            if (index < last)
                list[index] = list[last];
            list.RemoveAt(last);
        }
    }
}

//required for "record" to work
namespace System.Runtime.CompilerServices
{
    [UsedImplicitly]
    internal static class IsExternalInit { }
}