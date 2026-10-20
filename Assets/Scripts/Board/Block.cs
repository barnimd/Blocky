
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Block : MonoBehaviour
{
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public int ColorIndex { get; private set; }

    private SpriteRenderer spriteRenderer;
    private Coroutine moveRoutine;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetData(int x, int y, int colorIndex, Color color)
    {
        GridX = x;
        GridY = y;
        ColorIndex = colorIndex;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        spriteRenderer.color = color;
    }
    public void SetGridPosition(int x, int y)
    {
        GridX = x;
        GridY = y;
    }
    // Gerakkan blok ke posisi tujuan secara halus.
    public void MoveTo(Vector3 target, float duration)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveRoutine(target, duration));
    }

    private IEnumerator MoveRoutine(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = 1f - (1f - t) * (1f - t); // ease-out: cepat lalu melambat

            transform.position = Vector3.Lerp(start, target, t);
            yield return null; // lanjut di frame berikutnya
        }

        transform.position = target;
        moveRoutine = null;
    }
}
