using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Satu baris penjelasan bintang di panel hasil, misal:
// [★★★]  6+ turns left
// Baris yang sesuai hasil pemain diberi highlight.
public class StarRuleRow : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private StarDisplay stars;
    [SerializeField] private TMP_Text label;

    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.06f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.82f, 0.1f, 0.3f);

    public string Text => label != null ? label.text : string.Empty;
    public bool IsHighlighted { get; private set; }

    public void Setup(int starCount, string text, bool highlighted)
    {
        IsHighlighted = highlighted;

        if (stars != null)
        {
            stars.SetStars(starCount);
        }

        if (label != null)
        {
            label.text = text;
            label.alpha = highlighted ? 1f : 0.6f;
        }

        if (background != null)
        {
            background.color = highlighted ? highlightColor : normalColor;
        }
    }
}
