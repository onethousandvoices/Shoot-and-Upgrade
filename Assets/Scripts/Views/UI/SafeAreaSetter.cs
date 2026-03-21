namespace Views.UI
{
    public sealed class SafeAreaSetter : MonoBehaviour
    {
        private void Awake()
        {
            var safeArea = Screen.safeArea;
            var rt = (RectTransform)transform;

            var anchorMin = new Vector2(safeArea.x / Screen.width, safeArea.y / Screen.height);
            var anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

#if UNITY_IOS
        anchorMax.y = Mathf.Min(anchorMax.y, (Screen.height - 30f) / Screen.height);
#endif

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}