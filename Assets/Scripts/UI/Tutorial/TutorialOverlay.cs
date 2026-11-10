using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Overlay tutorial gaya "spotlight":
// layar diredupkan KECUALI area target, target diberi bingkai berdenyut,
// dan kartu berisi judul + penjelasan + tombol NEXT / SKIP.
//
// Redupnya dibuat dari 4 potongan (atas, bawah, kiri, kanan) yang
// mengelilingi target, jadi target terlihat terang di "lubang" tengah.
[RequireComponent(typeof(CanvasGroup))]
public class TutorialOverlay : MonoBehaviour
{
    [Header("Spotlight")]
    [SerializeField] private RectTransform dimTop;
    [SerializeField] private RectTransform dimBottom;
    [SerializeField] private RectTransform dimLeft;
    [SerializeField] private RectTransform dimRight;
    [SerializeField] private RectTransform highlightFrame;
    [SerializeField] private float spotlightPadding = 16f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 10f;

    [Header("Card")]
    [SerializeField] private RectTransform card;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text stepText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextLabel;
    [SerializeField] private Button skipButton;
    [SerializeField] private float cardMargin = 40f;

    private readonly Vector3[] corners = new Vector3[4];
    private RectTransform root;
    private CanvasGroup canvasGroup;
    private IReadOnlyList<TutorialStep> steps;
    private string finishLabel;
    private System.Action<bool> onFinished;

    public bool IsPlaying { get; private set; }
    public int StepIndex { get; private set; }
    public string CurrentTitle => titleText != null ? titleText.text : string.Empty;

    private void Awake()
    {
        root = (RectTransform)transform;
        canvasGroup = GetComponent<CanvasGroup>();
        SetVisible(false);

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(Next);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(Skip);
        }
    }

    // finished(true) = pemain menekan tombol terakhir,
    // finished(false) = pemain menekan SKIP.
    public void Play(IReadOnlyList<TutorialStep> tutorialSteps, string lastButtonLabel,
        System.Action<bool> finished)
    {
        if (tutorialSteps == null || tutorialSteps.Count == 0)
        {
            finished?.Invoke(true);
            return;
        }

        steps = tutorialSteps;
        finishLabel = lastButtonLabel;
        onFinished = finished;
        IsPlaying = true;

        transform.SetAsLastSibling();
        SetVisible(true);
        ShowStep(0);
    }

    public void Next()
    {
        if (!IsPlaying)
        {
            return;
        }

        if (StepIndex + 1 < steps.Count)
        {
            ShowStep(StepIndex + 1);
        }
        else
        {
            Finish(true);
        }
    }

    public void Skip()
    {
        if (IsPlaying)
        {
            Finish(false);
        }
    }

    private void Finish(bool completed)
    {
        IsPlaying = false;
        SetVisible(false);

        System.Action<bool> callback = onFinished;
        onFinished = null;
        callback?.Invoke(completed);
    }

    private void ShowStep(int index)
    {
        StepIndex = index;
        TutorialStep step = steps[index];
        bool isLast = index == steps.Count - 1;

        SetText(titleText, step.title);
        SetText(bodyText, step.body);
        SetText(stepText, $"{index + 1} / {steps.Count}");
        SetText(nextLabel, isLast ? finishLabel : "NEXT");

        // Di langkah terakhir SKIP tidak perlu lagi.
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(!isLast);
        }

        UpdateLayout();
    }

    // Diperbarui tiap frame: posisi target bisa berubah
    // (misal layout baru selesai dihitung di frame pertama).
    private void LateUpdate()
    {
        if (IsPlaying)
        {
            UpdateLayout();
        }
    }

    private void UpdateLayout()
    {
        TutorialStep step = steps[StepIndex];
        Rect full = root.rect;
        bool hasTarget = step.target != null && step.target.gameObject.activeInHierarchy;
        Rect hole = hasTarget ? GetLocalRect(step.target) : Rect.zero;

        if (hasTarget)
        {
            // Sedikit lebih besar dari target, tapi tetap di dalam layar.
            hole.xMin = Mathf.Max(full.xMin, hole.xMin - spotlightPadding);
            hole.yMin = Mathf.Max(full.yMin, hole.yMin - spotlightPadding);
            hole.xMax = Mathf.Min(full.xMax, hole.xMax + spotlightPadding);
            hole.yMax = Mathf.Min(full.yMax, hole.yMax + spotlightPadding);

            SetArea(dimTop, new Rect(full.xMin, hole.yMax, full.width, full.yMax - hole.yMax));
            SetArea(dimBottom, new Rect(full.xMin, full.yMin, full.width, hole.yMin - full.yMin));
            SetArea(dimLeft, new Rect(full.xMin, hole.yMin, hole.xMin - full.xMin, hole.height));
            SetArea(dimRight, new Rect(hole.xMax, hole.yMin, full.xMax - hole.xMax, hole.height));

            float pulse = (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed)) * pulseAmount;
            Rect frame = new Rect(hole.x - pulse, hole.y - pulse,
                hole.width + pulse * 2f, hole.height + pulse * 2f);
            SetArea(highlightFrame, frame);
        }
        else
        {
            // Tanpa target: satu potongan menutupi seluruh layar.
            SetArea(dimTop, full);
            SetArea(dimBottom, Rect.zero);
            SetArea(dimLeft, Rect.zero);
            SetArea(dimRight, Rect.zero);
        }

        if (highlightFrame != null)
        {
            highlightFrame.gameObject.SetActive(hasTarget);
        }

        PlaceCard(step.placement, full, hasTarget, hole);
    }

    private void PlaceCard(TutorialCardPlacement placement, Rect full, bool hasTarget, Rect hole)
    {
        if (card == null)
        {
            return;
        }

        float half = card.rect.height * 0.5f;
        float minY = full.yMin + cardMargin + half;
        float maxY = full.yMax - cardMargin - half;
        float y;

        switch (placement)
        {
            case TutorialCardPlacement.Top:
                y = maxY;
                break;

            case TutorialCardPlacement.Bottom:
                y = minY;
                break;

            case TutorialCardPlacement.Center:
                y = 0f;
                break;

            default:
                if (!hasTarget)
                {
                    y = 0f;
                }
                else if (hole.center.y > full.center.y)
                {
                    y = hole.yMin - cardMargin - half;   // Target di atas → kartu di bawahnya.
                }
                else
                {
                    y = hole.yMax + cardMargin + half;   // Target di bawah → kartu di atasnya.
                }
                break;
        }

        card.anchoredPosition = new Vector2(0f, Mathf.Clamp(y, minY, maxY));
    }

    // Sudut target (world) → koordinat lokal overlay (titik tengah = 0,0).
    private Rect GetLocalRect(RectTransform target)
    {
        target.GetWorldCorners(corners);
        Vector3 min = root.InverseTransformPoint(corners[0]);
        Vector3 max = root.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // Potongan memakai anchor & pivot di tengah-bawah-kiri supaya
    // posisi = pojok kiri bawah area, ukuran = lebar x tinggi area.
    private static void SetArea(RectTransform rect, Rect area)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = area.position;
        rect.sizeDelta = new Vector2(Mathf.Max(0f, area.width), Mathf.Max(0f, area.height));
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
