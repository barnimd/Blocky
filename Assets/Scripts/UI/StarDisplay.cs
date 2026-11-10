using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Menampilkan 0-3 bintang: bintang yang didapat menyala, sisanya redup.
// Dipakai di top bar (preview), panel hasil, dan level selector.
public class StarDisplay : MonoBehaviour
{
    [SerializeField] private Image[] stars;
    [SerializeField] private Color litColor = new Color(1f, 0.82f, 0.1f);
    [SerializeField] private Color dimColor = new Color(1f, 1f, 1f, 0.2f);

    [Header("Reveal Animation")]
    [SerializeField] private float revealDelay = 0.35f;
    [SerializeField] private float revealInterval = 0.25f;
    [SerializeField] private float popDuration = 0.3f;
    [SerializeField] private float popOvershoot = 1.35f;

    public int ShownStars { get; private set; }

    public void SetStars(int count)
    {
        StopAllCoroutines();
        ShownStars = Mathf.Clamp(count, 0, stars.Length);

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].color = i < ShownStars ? litColor : dimColor;
                stars[i].transform.localScale = Vector3.one;
            }
        }
    }

    // Panel hasil: semua bintang mulai redup, lalu yang didapat
    // menyala satu per satu dengan efek "pop" (membesar lalu kembali).
    public void PlayReveal(int count)
    {
        SetStars(0);
        ShownStars = Mathf.Clamp(count, 0, stars.Length);

        if (isActiveAndEnabled)
        {
            StartCoroutine(RevealRoutine());
        }
        else
        {
            SetStars(count);
        }
    }

    private IEnumerator RevealRoutine()
    {
        // Pakai waktu "unscaled" supaya tetap jalan walau game di-pause.
        yield return new WaitForSecondsRealtime(revealDelay);

        for (int i = 0; i < ShownStars; i++)
        {
            Image star = stars[i];

            if (star == null)
            {
                continue;
            }

            star.color = litColor;
            float time = 0f;

            while (time < popDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / popDuration);

                // 0 → overshoot (setengah awal) → 1 (setengah akhir).
                float scale = t < 0.5f
                    ? Mathf.Lerp(0.2f, popOvershoot, t * 2f)
                    : Mathf.Lerp(popOvershoot, 1f, (t - 0.5f) * 2f);

                star.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            star.transform.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(revealInterval);
        }
    }
}
