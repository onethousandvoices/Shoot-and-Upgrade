using TMPro;

namespace Views.UI;

public sealed class HpBarView : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private TMP_Text _hpText;

    public void Set(float current, float max)
    {
        _fillImage.fillAmount = max > 0f ? current / max : 0f;
        _hpText.SetText("{0}/{1}", Mathf.CeilToInt(current), Mathf.CeilToInt(max));
    }
}