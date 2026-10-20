using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Satu item di Objective Tracker: ikon warna + sisa target.
public class ObjectiveGoalItem : MonoBehaviour
{
    [SerializeField] private Image colorIcon;
    [SerializeField] private TMP_Text remainingText;
    [SerializeField] private Color completeTextColor = new Color(0.45f, 1f, 0.45f);
    [SerializeField, Range(0f, 1f)] private float completeIconAlpha = 0.4f;

    public BlockColor GoalColor { get; private set; }
    public string RemainingLabel => remainingText != null ? remainingText.text : string.Empty;

    private Color textBaseColor = Color.white;
    private bool hasBaseColor;

    public void Setup(BlockColor color, Color displayColor)
    {
        GoalColor = color;

        if (remainingText != null && !hasBaseColor)
        {
            textBaseColor = remainingText.color;
            hasBaseColor = true;
        }

        if (colorIcon != null)
        {
            colorIcon.color = displayColor;
        }
    }

    public void SetRemaining(int remaining)
    {
        bool isComplete = remaining <= 0;

        if (remainingText != null)
        {
            remainingText.text = isComplete ? "0" : remaining.ToString();
            remainingText.color = isComplete ? completeTextColor : textBaseColor;
        }

        if (colorIcon != null)
        {
            Color color = colorIcon.color;
            color.a = isComplete ? completeIconAlpha : 1f;
            colorIcon.color = color;
        }
    }
}
