using Controllers;
using TMPro;

namespace Views.UI
{
    public sealed class StatRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Button _plusButton;

        public Button PlusButton => _plusButton;

        public void Init(UpgradeType type)
        {
            var key = type switch
            {
                UpgradeType.Speed  => "upgrade_speed",
                UpgradeType.Health => "upgrade_health",
                UpgradeType.Damage => "upgrade_damage",
                _                  => ""
            };
            _nameText.text = LocalizationController.Get(key);
            this.SetActiveSafe(true);
        }

        public void SetLevel(int level, int maxLevel) => _levelText.SetText("{0}/{1}", level, maxLevel);

        public void SetPlusInteractable(bool state) => _plusButton.interactable = state;
    }
}