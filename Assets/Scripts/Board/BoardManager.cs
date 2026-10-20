
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
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
    [Header("Cascade Settings")]
    // Durasi animasi swap/rotate sebelum match dicek.
    [SerializeField] private float actionDuration = 0.22f;
    [SerializeField] private float clearDelay = 0.15f;
    [SerializeField] private float fallDuration = 0.25f;

    private Block[,] grid;
    private MatchFinder matchFinder;
    public int Width => width;
    public int Height => height;
    // True selama board sedang menghancurkan/menjatuhkan blok.
    public bool IsBusy { get; private set; }

    // Dipanggil setiap kali sekelompok blok hancur.
    // int = urutan combo (1 = match pertama, 2+ = chain).
    public event System.Action<HashSet<Block>, int> MatchesCleared;
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
                grid[x, y] = CreateBlock(
                    x, y, colorIndex, GetWorldPosition(x, y)
                );
            }
        }

        Debug.Log(
            $"Board berhasil dibuat: {width} x {height} " +
            $"= {width * height} blok."
        );
    }
    private Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(
            (x - (width - 1) / 2f) * cellSpacing,
            (y - (height - 1) / 2f) * cellSpacing,
            0f
        );
    }

    private Block CreateBlock(
        int x,
        int y,
        int colorIndex,
        Vector3 spawnPosition)
    {
        GameObject blockObject = Instantiate(
            blockPrefab,
            spawnPosition,
            Quaternion.identity,
            boardContainer
        );

        blockObject.name = $"Block_{x}_{y}";

        Block block = blockObject.GetComponent<Block>();
        block.SetData(x, y, colorIndex, blockColors[colorIndex]);

        return block;
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
        if (IsBusy)
        {
            return false;
        }

        if (first == null || second == null)
        {
            return false;
        }

        int dx = Mathf.Abs(first.GridX - second.GridX);
        int dy = Mathf.Abs(first.GridY - second.GridY);

        // Hanya izinkan dua blok yang bersebelahan,
        // termasuk diagonal (dx = 1 dan dy = 1).
        if (dx > 1 || dy > 1 || dx + dy == 0)
        {
            return false;
        }

        int firstX = first.GridX;
        int firstY = first.GridY;

        int secondX = second.GridX;
        int secondY = second.GridY;

        SetCell(first, secondX, secondY);
        SetCell(second, firstX, firstY);

        first.MoveTo(GetWorldPosition(secondX, secondY), actionDuration);
        second.MoveTo(GetWorldPosition(firstX, firstY), actionDuration);

        Debug.Log(
            $"Swap berhasil: ({firstX},{firstY}) " +
            $"dengan ({secondX},{secondY})."
        );

        return true;
    }

    // Memutar kelompok 2x2 dengan pojok kiri-bawah di (x, y).
    public bool TryRotate(int x, int y, bool clockwise)
    {
        if (IsBusy)
        {
            return false;
        }

        if (x < 0 || x >= width - 1 ||
            y < 0 || y >= height - 1)
        {
            return false;
        }

        Block bottomLeft = grid[x, y];
        Block bottomRight = grid[x + 1, y];
        Block topRight = grid[x + 1, y + 1];
        Block topLeft = grid[x, y + 1];

        if (bottomLeft == null || bottomRight == null ||
            topRight == null || topLeft == null)
        {
            return false;
        }

        if (clockwise)
        {
            // Searah jarum jam:
            // kiri-atas → kanan-atas → kanan-bawah → kiri-bawah → kiri-atas
            SetCell(topLeft, x + 1, y + 1);
            SetCell(topRight, x + 1, y);
            SetCell(bottomRight, x, y);
            SetCell(bottomLeft, x, y + 1);
        }
        else
        {
            // Berlawanan jarum jam: kebalikan urutan di atas.
            SetCell(topLeft, x, y);
            SetCell(bottomLeft, x + 1, y);
            SetCell(bottomRight, x + 1, y + 1);
            SetCell(topRight, x, y + 1);
        }

        // Animasi: keempat blok melengkung mengelilingi pusat 2x2.
        // Di Unity (sumbu Y ke atas), sudut negatif = searah jarum jam.
        Vector3 pivot =
            (GetWorldPosition(x, y) + GetWorldPosition(x + 1, y + 1)) / 2f;
        float angle = clockwise ? -90f : 90f;

        foreach (Block block in new[] { bottomLeft, bottomRight, topRight, topLeft })
        {
            block.OrbitTo(
                pivot,
                angle,
                GetWorldPosition(block.GridX, block.GridY),
                actionDuration
            );
        }

        string direction = clockwise ? "CW" : "CCW";
        Debug.Log($"Rotate {direction} berhasil di 2x2 ({x},{y}).");

        return true;
    }

    // Mengubah posisi dunia (jari/mouse) menjadi koordinat sel.
    // Hanya dihitung "masuk" jika cukup dekat ke tengah blok,
    // supaya geseran diagonal yang lewat pojok tidak ikut
    // menyentuh blok di sebelahnya.
    public bool TryGetCellAt(
        Vector2 worldPoint,
        float enterRadius,
        out int x,
        out int y)
    {
        // Pusat blok ada di bilangan bulat (0, 1, 2, ...).
        float cellX = worldPoint.x / cellSpacing + (width - 1) / 2f;
        float cellY = worldPoint.y / cellSpacing + (height - 1) / 2f;

        x = Mathf.RoundToInt(cellX);
        y = Mathf.RoundToInt(cellY);

        if (x < 0 || x >= width ||
            y < 0 || y >= height)
        {
            return false;
        }

        float distance =
            Vector2.Distance(new Vector2(cellX, cellY), new Vector2(x, y)) * cellSpacing;

        return distance <= enterRadius;
    }

    public bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // Memindahkan data blok ke sel (x, y). Visual diatur terpisah.
    private void SetCell(Block block, int x, int y)
    {
        grid[x, y] = block;
        block.SetGridPosition(x, y);
    }

    // Dipanggil setelah swap/rotate berhasil.
    public void ResolveMatches()
    {
        if (IsBusy)
        {
            return;
        }

        StartCoroutine(ResolveRoutine());
    }

    private IEnumerator ResolveRoutine()
    {
        IsBusy = true;

        // Tunggu animasi swap/rotate selesai dulu.
        yield return new WaitForSeconds(actionDuration);

        int combo = 0;
        HashSet<Block> matches = FindMatches();

        // Ulangi selama masih ada match (chain combo).
        while (matches.Count > 0)
        {
            combo++;
            Debug.Log($"Combo x{combo}: {matches.Count} blok hancur.");

            // Kabari sistem lain SEBELUM blok dihancurkan,
            // supaya data warna/posisinya masih bisa dibaca.
            MatchesCleared?.Invoke(matches, combo);

            ClearMatches(matches);
            yield return new WaitForSeconds(clearDelay);

            ApplyGravity();
            SpawnNewBlocks();
            yield return new WaitForSeconds(fallDuration);

            matches = FindMatches();
        }

        IsBusy = false;
    }

    private void ClearMatches(HashSet<Block> matches)
    {
        foreach (Block block in matches)
        {
            grid[block.GridX, block.GridY] = null;

            // Blok menghancurkan dirinya sendiri setelah animasi selesai.
            block.PlayClear(clearDelay);
        }
    }

    private void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            int emptyCount = 0;

            // Dari bawah ke atas.
            for (int y = 0; y < height; y++)
            {
                Block block = grid[x, y];

                if (block == null)
                {
                    emptyCount++;
                    continue;
                }

                if (emptyCount == 0)
                {
                    continue;
                }

                // Turunkan blok sebanyak sel kosong di bawahnya.
                int targetY = y - emptyCount;

                grid[x, targetY] = block;
                grid[x, y] = null;

                block.SetGridPosition(x, targetY);
                block.FallTo(GetWorldPosition(x, targetY), fallDuration);
            }
        }
    }

    private void SpawnNewBlocks()
    {
        for (int x = 0; x < width; x++)
        {
            int spawnOffset = 0;

            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    continue;
                }

                // Warna acak penuh: boleh langsung membentuk match,
                // justru itu yang memicu chain combo.
                int colorIndex = Random.Range(0, blockColors.Length);

                // Muncul berurutan di atas board supaya tidak menumpuk.
                Vector3 spawnPosition =
                    GetWorldPosition(x, height + spawnOffset);

                Block block = CreateBlock(x, y, colorIndex, spawnPosition);
                grid[x, y] = block;

                block.FallTo(GetWorldPosition(x, y), fallDuration);
                spawnOffset++;
            }
        }
    }
}
