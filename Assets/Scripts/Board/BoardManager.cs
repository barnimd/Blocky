
using UnityEngine;
using System.Collections.Generic;
public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private float cellSpacing = 1f;

    [Header("References")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform boardContainer;

    [Header("Block Colors")]
    [SerializeField]
    private Color[] blockColors = new Color[]
    {
        new Color(1f, 0.25f, 0.35f),   // Red
        new Color(0.15f, 0.75f, 1f),   // Blue
        new Color(0.55f, 0.9f, 0.2f), // Green
        new Color(1f, 0.8f, 0.15f),   // Yellow
        new Color(0.7f, 0.3f, 1f)     // Purple
    };

    private Block[,] grid;
    private MatchFinder matchFinder;
    public int Width => width;
    public int Height => height;

    private void Start()
    {
        if (!ValidateSetup())
        {
            return;
        }

        matchFinder = new MatchFinder(this);
        GenerateBoard();

        // Board awal seharusnya tidak punya match sama sekali.
        Debug.Log($"Match di board awal: {FindMatches().Count} blok.");
    }

    private bool ValidateSetup()
    {
        if (blockPrefab == null)
        {
            Debug.LogError(
                "BoardManager: Block Prefab belum diisi.",
                this
            );
            return false;
        }

        if (boardContainer == null)
        {
            Debug.LogError(
                "BoardManager: Board Container belum diisi.",
                this
            );
            return false;
        }

        if (blockColors == null || blockColors.Length < 3)
        {
            Debug.LogError(
                "BoardManager: Minimal 3 warna diperlukan.",
                this
            );
            return false;
        }

        if (width < 3 || height < 3 || cellSpacing <= 0f)
        {
            Debug.LogError(
                "BoardManager: Ukuran board atau spacing tidak valid.",
                this
            );
            return false;
        }

        if (blockPrefab.GetComponent<Block>() == null)
        {
            Debug.LogError(
                "BoardManager: Prefab harus memiliki komponen Block.",
                this
            );
            return false;
        }

        return true;
    }

    private void GenerateBoard()
    {
        grid = new Block[width, height];

        // Bersihkan blok lama jika board dibuat ulang.
        for (int i = boardContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(boardContainer.GetChild(i).gameObject);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int colorIndex = GetRandomValidColor(x, y);
                Color color = blockColors[colorIndex];

                Vector3 position = new Vector3(
                    (x - (width - 1) / 2f) * cellSpacing,
                    (y - (height - 1) / 2f) * cellSpacing,
                    0f
                );

                GameObject blockObject = Instantiate(
                    blockPrefab,
                    position,
                    Quaternion.identity,
                    boardContainer
                );

                blockObject.name = $"Block_{x}_{y}";

                Block block = blockObject.GetComponent<Block>();
                block.SetData(x, y, colorIndex, color);

                grid[x, y] = block;
            }
        }

        Debug.Log(
            $"Board berhasil dibuat: {width} x {height} " +
            $"= {width * height} blok."
        );
    }

    private int GetRandomValidColor(int x, int y)
    {
        int selectedColor;

        // Hindari match awal tiga blok horizontal atau vertikal.
        // Dengan minimal tiga warna, selalu ada pilihan valid.
        do
        {
            selectedColor = Random.Range(0, blockColors.Length);

        } while (
            (x >= 2 &&
             grid[x - 1, y] != null &&
             grid[x - 2, y] != null &&
             IsSameColor(
                 selectedColor,
                 grid[x - 1, y],
                 grid[x - 2, y]
             ))
            ||
            (y >= 2 &&
             grid[x, y - 1] != null &&
             grid[x, y - 2] != null &&
             IsSameColor(
                 selectedColor,
                 grid[x, y - 1],
                 grid[x, y - 2]
             ))
        );

        return selectedColor;
    }

    private bool IsSameColor(
    int colorIndex,
    Block first,
    Block second)
    {
        return first != null
            && second != null
            && first.ColorIndex == colorIndex
            && second.ColorIndex == colorIndex;
    }

    public Block GetBlock(int x, int y)
    {
        if (grid == null ||
            x < 0 || x >= width ||
            y < 0 || y >= height)
        {
            return null;
        }

        return grid[x, y];
    }

    public HashSet<Block> FindMatches()
    {
        return matchFinder.FindAllMatches();
    }
    public bool TrySwap(Block first, Block second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        int dx = Mathf.Abs(first.GridX - second.GridX);
        int dy = Mathf.Abs(first.GridY - second.GridY);

        // Hanya izinkan dua blok yang bersebelahan.
        if (dx + dy != 1)
        {
            return false;
        }

        int firstX = first.GridX;
        int firstY = first.GridY;

        int secondX = second.GridX;
        int secondY = second.GridY;

        // Tukar data di dalam grid.
        grid[firstX, firstY] = second;
        grid[secondX, secondY] = first;

        // Tukar posisi visual.
        Vector3 firstPosition = first.transform.position;
        Vector3 secondPosition = second.transform.position;

        first.transform.position = secondPosition;
        second.transform.position = firstPosition;

        // Perbarui koordinat masing-masing blok.
        first.SetGridPosition(secondX, secondY);
        second.SetGridPosition(firstX, firstY);

        Debug.Log(
            $"Swap berhasil: ({firstX},{firstY}) " +
            $"dengan ({secondX},{secondY})."
        );

        return true;
    }

}
