
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Block : MonoBehaviour
{
    [Header("Juice")]
    [SerializeField] private float highlightScale = 1.12f;
    [SerializeField] private float actionPopScale = 1.15f;
    [SerializeField, Range(0f, 0.5f)] private float landSquash = 0.18f;
    [SerializeField] private float landSquashDuration = 0.15f;

    [Header("Outline")]
    // Tebal pinggiran, relatif terhadap ukuran blok.
    [SerializeField, Range(0.02f, 0.3f)] private float outlineThickness = 0.08f;
    // Seberapa gelap pinggiran dibanding warna blok (0 = sama, 1 = hitam).
    [SerializeField, Range(0f, 1f)] private float outlineDarken = 0.35f;

    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public int ColorIndex { get; private set; }

    // Renderer utama = outline (gelap), fillRenderer = isi (warna asli).
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer fillRenderer;
    private Coroutine moveRoutine;
    private Coroutine scaleRoutine;

    // Skala asli prefab (misal 0.9) dikalikan efek animasi.
    private Vector3 baseScale;
    private Vector3 effectScale = Vector3.one;
    private bool isHighlighted;
    private bool isClearing;
    private int baseSortingOrder;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseSortingOrder = spriteRenderer.sortingOrder;

        CreateFill();
        SetSortingOrder(baseSortingOrder);
    }

    // Membuat child "Fill" yang sedikit lebih kecil di atas sprite utama.
    // Sprite utama yang lebih gelap jadi terlihat sebagai outline.
    private void CreateFill()
    {
        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(transform, false);
        fillObject.transform.localScale =
            Vector3.one * (1f - outlineThickness * 2f);

        fillRenderer = fillObject.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = spriteRenderer.sprite;
        fillRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
        fillRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
    }

    // Fill selalu satu tingkat di atas outline-nya sendiri.
    private void SetSortingOrder(int order)
    {
        spriteRenderer.sortingOrder = order;
        fillRenderer.sortingOrder = order + 1;
    }

    private Color GetOutlineColor(Color color)
    {
        // Gelapkan lewat HSV supaya warnanya tetap "merah gelap",
        // bukan merah keabu-abuan.
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);

        Color outline = Color.HSVToRGB(hue, saturation, value * (1f - outlineDarken));
        outline.a = color.a;

        return outline;
    }

    // Semua efek skala digabung di sini, jadi highlight dan
    // squash/pop tidak saling menimpa.
    private void LateUpdate()
    {
        float highlight = isHighlighted ? highlightScale : 1f;
        transform.localScale = Vector3.Scale(baseScale * highlight, effectScale);
    }

    public void SetData(int x, int y, int colorIndex, Color color)
    {
        GridX = x;
        GridY = y;
        ColorIndex = colorIndex;

        fillRenderer.color = color;
        spriteRenderer.color = GetOutlineColor(color);
    }
    public void SetGridPosition(int x, int y)
    {
        GridX = x;
        GridY = y;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isClearing || isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;

        // Squash kecil saat disentuh, lalu memantul ke ukuran highlight.
        if (highlighted)
        {
            PlayScalePunch(new Vector3(0.85f, 1.1f, 1f), landSquashDuration);
        }
    }

    // Swap: geser dengan sedikit kelewatan lalu kembali (ease-out-back).
    public void MoveTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;

        PlayScalePunch(Vector3.one * actionPopScale, duration);
        StartMotion(
            t => Vector3.LerpUnclamped(start, target, EaseOutBack(t)),
            target,
            duration,
            false
        );
    }

    // Rotate: bergerak melengkung mengelilingi titik pusat 2x2.
    public void OrbitTo(Vector3 pivot, float angle, Vector3 target, float duration)
    {
        Vector3 offset = transform.position - pivot;

        PlayScalePunch(Vector3.one * actionPopScale, duration);
        StartMotion(
            t => pivot + Quaternion.Euler(0f, 0f, angle * EaseOutBack(t)) * offset,
            target,
            duration,
            false
        );
    }

    // Gravity: jatuh makin cepat, lalu squash saat mendarat.
    public void FallTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;

        StartMotion(
            t => Vector3.LerpUnclamped(start, target, EaseInQuad(t)),
            target,
            duration,
            true
        );
    }

    // Hancur: membesar sebentar (pop), lalu mengecil dan memudar.
    public void PlayClear(float duration)
    {
        isClearing = true;
        isHighlighted = false;

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
        }

        scaleRoutine = StartCoroutine(ClearRoutine(duration));
    }

    private void StartMotion(
        System.Func<float, Vector3> getPosition,
        Vector3 target,
        float duration,
        bool squashOnLand)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(
            MotionRoutine(getPosition, target, duration, squashOnLand)
        );
    }

    private IEnumerator MotionRoutine(
        System.Func<float, Vector3> getPosition,
        Vector3 target,
        float duration,
        bool squashOnLand)
    {
        // Tampil di atas blok lain selama bergerak.
        // +2 karena blok diam memakai 2 tingkat (outline + fill).
        SetSortingOrder(baseSortingOrder + 2);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            transform.position = getPosition(t);

            yield return null; // lanjut di frame berikutnya
        }

        transform.position = target;
        SetSortingOrder(baseSortingOrder);
        moveRoutine = null;

        if (squashOnLand)
        {
            PlayScalePunch(
                new Vector3(1f + landSquash, 1f - landSquash, 1f),
                landSquashDuration
            );
        }
    }

    // Mulai dari skala "from", lalu memantul kembali ke normal.
    private void PlayScalePunch(Vector3 from, float duration)
    {
        if (isClearing)
        {
            return;
        }

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
        }

        scaleRoutine = StartCoroutine(ScalePunchRoutine(from, duration));
    }

    private IEnumerator ScalePunchRoutine(Vector3 from, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = EaseOutBack(Mathf.Clamp01(time / duration));
            effectScale = Vector3.LerpUnclamped(from, Vector3.one, t);

            yield return null;
        }

        effectScale = Vector3.one;
        scaleRoutine = null;
    }

    private IEnumerator ClearRoutine(float duration)
    {
        SetSortingOrder(baseSortingOrder + 2);

        const float popScale = 1.25f;
        float popTime = duration * 0.3f;
        Color startOutline = spriteRenderer.color;
        Color startFill = fillRenderer.color;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            if (time < popTime)
            {
                float t = time / popTime;
                effectScale = Vector3.one * Mathf.Lerp(1f, popScale, EaseOutQuad(t));
            }
            else
            {
                float t = Mathf.Clamp01((time - popTime) / (duration - popTime));
                effectScale = Vector3.one * Mathf.Lerp(popScale, 0f, EaseInQuad(t));

                // Outline dan fill memudar bersamaan.
                spriteRenderer.color = WithAlpha(startOutline, 1f - t);
                fillRenderer.color = WithAlpha(startFill, 1f - t);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    // Kurva easing: mengubah t (0..1) linear jadi gerakan yang lebih hidup.
    private static float EaseInQuad(float t)
    {
        return t * t;
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }
}
