using System.Collections;
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

    [Header("Denied Feedback")]
    [SerializeField] private Color deniedColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float deniedDuration = 0.3f;
    [SerializeField] private float deniedShakeDistance = 14f;

    public ActionType ActionType => actionType;

    private Coroutine deniedRoutine;
    private Vector2 iconRestPosition;
    private Vector2 textRestPosition;
    private Color textBaseColor = Color.white;
    private float currentAlpha = 1f;

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
        currentAlpha = turns > 0 ? 1f : emptyAlpha;

        SetAlpha(icon, currentAlpha);
        SetAlpha(countText, currentAlpha);
    }

    // Slot bergetar & angka berkedip merah saat jatahnya habis.
    public void PlayDenied()
    {
        if (icon == null || countText == null || !isActiveAndEnabled)
        {
            return;
        }

        if (deniedRoutine == null)
        {
            iconRestPosition = icon.rectTransform.anchoredPosition;
            textRestPosition = countText.rectTransform.anchoredPosition;
            textBaseColor = countText.color;
        }
        else
        {
            StopCoroutine(deniedRoutine);
        }

        deniedRoutine = StartCoroutine(DeniedRoutine());
    }

    public bool IsPlayingDenied => deniedRoutine != null;

    private IEnumerator DeniedRoutine()
    {
        const float shakeCycles = 3f;
        float time = 0f;

        while (time < deniedDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / deniedDuration);
            float offset = Mathf.Sin(t * shakeCycles * Mathf.PI * 2f)
                * deniedShakeDistance * (1f - t);

            icon.rectTransform.anchoredPosition = iconRestPosition + new Vector2(offset, 0f);
            countText.rectTransform.anchoredPosition = textRestPosition + new Vector2(offset, 0f);

            Color tint = Color.Lerp(textBaseColor, deniedColor, 1f - t);
            tint.a = currentAlpha;
            countText.color = tint;

            yield return null;
        }

        icon.rectTransform.anchoredPosition = iconRestPosition;
        countText.rectTransform.anchoredPosition = textRestPosition;

        Color restColor = textBaseColor;
        restColor.a = currentAlpha;
        countText.color = restColor;

        deniedRoutine = null;
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
