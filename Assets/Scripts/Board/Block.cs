
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Block : MonoBehaviour
{
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public int ColorIndex { get; private set; }

    private SpriteRenderer spriteRenderer;

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
    // SEMENTARA: penanda visual untuk testing MatchFinder.
    public void MarkAsMatched()
    {
        spriteRenderer.color = Color.white;
    }
}
