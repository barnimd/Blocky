using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionTurnSlot : MonoBehaviour
{
    [Header("Action")]
    [SerializeField] private ActionType actionType;

    [Header("References")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    [Header("Empty State")]
    [SerializeField, Range(0f, 1f)] private float emptyAlpha = 0.35f;

    public ActionType ActionType => actionType;

    public void SetCount(int turns)
    {
        if (icon == null || countText == null)
        {
            Debug.LogError(
                $"ActionTurnSlot ({name}): Icon atau Count Text belum diisi.",
                this
            );
            return;
        }

        countText.text = turns.ToString();

        // Pudarkan slot jika jatah turn sudah habis.
        float alpha = turns > 0 ? 1f : emptyAlpha;

        SetAlpha(icon, alpha);
        SetAlpha(countText, alpha);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}