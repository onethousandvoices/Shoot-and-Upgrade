using Controllers;
using TMPro;

namespace Views.UI;

[RequireComponent(typeof(TMP_Text))]
public sealed class TextLocalization : MonoBehaviour
{
    [SerializeField] private string _key;
    [SerializeField] private TMP_Text _text;

    private void Awake() => _text.text = LocalizationController.Get(_key);

    private void OnValidate() => _text ??= GetComponent<TMP_Text>();
}