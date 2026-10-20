
using System.Collections.Generic;
using UnityEngine;

public class BoardInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ActionInventory actionInventory;
    [SerializeField] private Camera mainCamera;

    [Tooltip("Opsional. Jika diisi, input hanya diterima saat state PlayerInput.")]
    [SerializeField] private GameManager gameManager;

    [Header("Gesture")]
    [Tooltip("Seberapa dekat jari ke tengah blok agar blok dihitung tersentuh.")]
    [SerializeField, Range(0.2f, 0.5f)] private float cellEnterRadius = 0.38f;

    [Tooltip("Jarak geser minimal agar dianggap swipe walau belum sampai blok tetangga.")]
    [SerializeField, Min(0.05f)] private float swipeMinDistance = 0.3f;

    [Tooltip("Waktu tunggu setelah SEMUA input lepas sebelum aksi dieksekusi.")]
    [SerializeField, Min(0f)] private float releaseDelay = 0.12f;

    // 4 = satu putaran penuh di grup 2x2.
    private const int MaxPathLength = 4;

    internal enum StepResult
    {
        Allowed,
        Invalid,
        NoTurns
    }

    // Urutan sel yang dilewati jari selama satu gesture.
    private readonly List<Vector2Int> gesturePath = new List<Vector2Int>();
    private bool isGestureActive;
    private bool wasPointerHeld;
    private float releaseTimer;
    private Vector2 gestureStartWorld;
    private Vector2 lastPointerWorld;

    // Sel terakhir yang ditolak karena jatah habis,
    // supaya feedback merah tidak diulang setiap frame.
    private Vector2Int? lastDeniedCell;

    // Blok yang dipilih untuk mode tap-tap.
    private Block selectedBlock;

    // Board diam DAN game sedang menunggu input pemain.
    internal bool CanInteract =>
        !boardManager.IsBusy &&
        (gameManager == null || gameManager.State == GameState.PlayerInput);

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

    }

    private void Update()
    {
        if (mainCamera == null ||
            boardManager == null ||
            actionInventory == null)
        {
            return;
        }

        bool isHeld = IsPointerHeld();
        bool pressStarted = isHeld && !wasPointerHeld;
        wasPointerHeld = isHeld;

        if (isHeld)
        {
            // Masih ada input: jangan eksekusi apa pun dulu.
            releaseTimer = releaseDelay;

            // Abaikan input selama board masih resolve/jatuh,
            // atau saat stage sudah selesai.
            if (CanInteract)
            {
                TrackPointer(pressStarted);
            }

            return;
        }

        if (!isGestureActive)
        {
            return;
        }

        // Semua input sudah lepas. Tunggu sebentar: kalau pemain
        // menyentuh lagi di blok sebelahnya, gesture masih lanjut.
        releaseTimer -= Time.deltaTime;

        if (releaseTimer <= 0f)
        {
            FinishGesture();
        }
    }

    // ───────────── Mengumpulkan gesture ─────────────

    private void TrackPointer(bool pressStarted)
    {
        Vector2 worldPoint = GetPointerWorldPosition();

        bool isOnCell = boardManager.TryGetCellAt(
            worldPoint,
            cellEnterRadius,
            out int x,
            out int y
        );
        Vector2Int cell = new Vector2Int(x, y);

        if (!isGestureActive)
        {
            // Gesture baru hanya dimulai saat jari/mouse baru menekan.
            if (!pressStarted)
            {
                return;
            }

            if (isOnCell)
            {
                StartGesture(cell, worldPoint);
            }
            else if (!boardManager.IsInsideBoard(x, y))
            {
                // Tap di luar board membatalkan seleksi.
                ClearSelection();
            }

            return;
        }

        lastPointerWorld = worldPoint;

        if (!isOnCell)
        {
            return;
        }

        Vector2Int lastCell = gesturePath[gesturePath.Count - 1];

        if (cell == lastCell)
        {
            return;
        }

        // Geser balik ke blok sebelumnya = batalkan langkah terakhir.
        // Berguna saat pemain masih ragu.
        if (gesturePath.Count >= 2 &&
            cell == gesturePath[gesturePath.Count - 2])
        {
            SetCellHighlight(lastCell, false);
            gesturePath.RemoveAt(gesturePath.Count - 1);
            lastDeniedCell = null;
            return;
        }

        StepResult step = EvaluateStep(
            gesturePath,
            cell,
            actionInventory,
            out ActionType deniedAction
        );

        if (step == StepResult.Allowed)
        {
            gesturePath.Add(cell);
            SetCellHighlight(cell, true);
            lastDeniedCell = null;
            return;
        }

        if (step == StepResult.NoTurns)
        {
            // Langsung beri feedback merah, sekali per sel.
            if (lastDeniedCell != cell)
            {
                lastDeniedCell = cell;
                ShowDenied(deniedAction, lastCell, cell);
            }

            return;
        }

        // Sentuhan baru jauh dari gesture lama (saat masa tunggu):
        // selesaikan gesture lama, lalu mulai yang baru.
        if (pressStarted)
        {
            FinishGesture();

            if (CanInteract)
            {
                StartGesture(cell, worldPoint);
            }
        }
    }

    // Cek apakah sel boleh ditambahkan ke path.
    // Path hanya boleh membentuk gerakan yang sah DAN masih punya jatah:
    //   2 blok   = swap (horizontal / vertical / diagonal)
    //   3-4 blok = rotate, berbentuk L di dalam satu grup 2x2
    // NoTurns = gerakannya sah tapi jatahnya habis (deniedAction).
    internal static StepResult EvaluateStep(
        List<Vector2Int> path,
        Vector2Int cell,
        ActionInventory inventory,
        out ActionType deniedAction)
    {
        deniedAction = default;

        if (path.Count >= MaxPathLength)
        {
            return StepResult.Invalid;
        }

        Vector2Int lastCell = path[path.Count - 1];

        if (!IsNeighbor(cell, lastCell) || path.Contains(cell))
        {
            return StepResult.Invalid;
        }

        Vector2Int move = cell - lastCell;

        // Blok ke-2: swap. Jenis swap-nya harus masih punya jatah.
        // Langkah lurus juga boleh jika jatah Rotate masih ada,
        // karena bisa jadi awal gerakan rotate (bentuk L).
        if (path.Count == 1)
        {
            ActionType swapAction = GetSwapAction(move);

            if (inventory.HasTurns(swapAction) ||
                (IsStraightStep(move) && inventory.HasTurns(ActionType.Rotate)))
            {
                return StepResult.Allowed;
            }

            deniedAction = swapAction;
            return StepResult.NoTurns;
        }

        Vector2Int firstMove = path[1] - path[0];

        // Blok ke-3: harus belok tegak lurus membentuk L,
        // dan jatah Rotate masih ada.
        if (path.Count == 2)
        {
            bool isLShape =
                IsStraightStep(firstMove) &&
                IsStraightStep(move) &&
                Dot(firstMove, move) == 0;

            if (!isLShape)
            {
                return StepResult.Invalid;
            }

            if (!inventory.HasTurns(ActionType.Rotate))
            {
                deniedAction = ActionType.Rotate;
                return StepResult.NoTurns;
            }

            return StepResult.Allowed;
        }

        // Blok ke-4: hanya untuk menutup putaran di grup 2x2 yang sama,
        // jadi harus belok ke arah yang sama seperti belokan pertama.
        Vector2Int secondMove = path[2] - path[1];

        bool closesLoop =
            IsStraightStep(move) &&
            Cross(secondMove, move) == Cross(firstMove, secondMove);

        return closesLoop ? StepResult.Allowed : StepResult.Invalid;
    }

    private void StartGesture(Vector2Int cell, Vector2 worldPoint)
    {
        isGestureActive = true;
        lastDeniedCell = null;

        gesturePath.Clear();
        gesturePath.Add(cell);

        gestureStartWorld = worldPoint;
        lastPointerWorld = worldPoint;

        SetCellHighlight(cell, true);
    }

    // ───────────── Mengeksekusi gesture ─────────────

    private void FinishGesture()
    {
        isGestureActive = false;

        foreach (Vector2Int cell in gesturePath)
        {
            SetCellHighlight(cell, false);
        }

        List<Vector2Int> path = new List<Vector2Int>(gesturePath);
        gesturePath.Clear();

        if (path.Count == 1)
        {
            Vector2 drag = lastPointerWorld - gestureStartWorld;

            // Swipe pendek: jari belum sampai ke blok tetangga,
            // tapi arah gesernya sudah jelas.
            if (drag.magnitude >= swipeMinDistance)
            {
                ClearSelection();
                TrySwapCells(path[0], path[0] + GetSwipeDirection(drag));
            }
            else
            {
                HandleTap(path[0]);
            }
        }
        else if (path.Count == 2)
        {
            // Slide melewati 2 blok = swap.
            ClearSelection();
            TrySwapCells(path[0], path[1]);
        }
        else
        {
            // Slide melewati 3+ blok = rotate.
            ClearSelection();
            TryRotatePath(path);
        }
    }

    private void HandleTap(Vector2Int cell)
    {
        Block tappedBlock = boardManager.GetBlock(cell.x, cell.y);

        if (tappedBlock == null)
        {
            return;
        }

        if (selectedBlock == null)
        {
            SelectBlock(tappedBlock);
            return;
        }

        if (tappedBlock == selectedBlock)
        {
            ClearSelection();
            return;
        }

        Vector2Int selectedCell =
            new Vector2Int(selectedBlock.GridX, selectedBlock.GridY);

        // Bukan tetangga (termasuk diagonal) → pindah seleksi.
        if (!IsNeighbor(selectedCell, cell))
        {
            SelectBlock(tappedBlock);
            return;
        }

        ClearSelection();
        TrySwapCells(selectedCell, cell);
    }

    internal bool TrySwapCells(Vector2Int from, Vector2Int to)
    {
        Block first = boardManager.GetBlock(from.x, from.y);
        Block second = boardManager.GetBlock(to.x, to.y);

        // Contoh: swipe keluar dari tepi board.
        if (first == null || second == null)
        {
            return false;
        }

        ActionType action = GetSwapAction(to - from);

        if (!HasTurnsOrDeny(action, from, to))
        {
            return false;
        }

        if (boardManager.TrySwap(first, second))
        {
            CompleteAction(action);
            return true;
        }

        return false;
    }

    internal bool TryRotatePath(List<Vector2Int> path)
    {
        Vector2Int firstMove = path[1] - path[0];
        Vector2Int secondMove = path[2] - path[1];

        // Harus berbentuk "L": dua langkah lurus (bukan diagonal)
        // yang saling tegak lurus (dot product = 0).
        bool isLShape =
            IsStraightStep(firstMove) &&
            IsStraightStep(secondMove) &&
            Dot(firstMove, secondMove) == 0;

        if (!isLShape)
        {
            Debug.LogWarning(
                "Gesture tidak dikenali: rotate butuh 3 blok berbentuk L."
            );
            return false;
        }

        // Grup 2x2 = kotak terkecil yang memuat ketiga blok.
        int anchorX = Mathf.Min(path[0].x, path[1].x, path[2].x);
        int anchorY = Mathf.Min(path[0].y, path[1].y, path[2].y);

        // Blok ke-4 (misal geser satu putaran penuh) harus tetap
        // di dalam grup 2x2 yang sama.
        for (int i = 3; i < path.Count; i++)
        {
            if (path[i].x < anchorX || path[i].x > anchorX + 1 ||
                path[i].y < anchorY || path[i].y > anchorY + 1)
            {
                Debug.LogWarning(
                    "Gesture tidak dikenali: geseran keluar dari grup 2x2."
                );
                return false;
            }
        }

        // Cross product menentukan arah belokan.
        // Sumbu Y Unity ke atas: hasil negatif = searah jarum jam.
        bool clockwise = Cross(firstMove, secondMove) < 0;

        if (!HasTurnsOrDeny(ActionType.Rotate, path.ToArray()))
        {
            return false;
        }

        if (boardManager.TryRotate(anchorX, anchorY, clockwise))
        {
            CompleteAction(ActionType.Rotate);
            return true;
        }

        return false;
    }

    // Semua aksi wajib lewat sini sebelum dieksekusi.
    private bool HasTurnsOrDeny(ActionType action, params Vector2Int[] cells)
    {
        if (actionInventory.HasTurns(action))
        {
            return true;
        }

        ShowDenied(action, cells);
        return false;
    }

    // Feedback "jatah habis": blok berkedip merah + bergetar,
    // dan inventory memberi tahu HUD/audio lewat event TurnDenied.
    private void ShowDenied(ActionType action, params Vector2Int[] cells)
    {
        actionInventory.ReportDenied(action);

        foreach (Vector2Int cell in cells)
        {
            Block block = boardManager.GetBlock(cell.x, cell.y);

            if (block != null)
            {
                block.PlayDenied();
            }
        }
    }

    // Dipanggil setelah aksi berhasil dieksekusi di board.
    private void CompleteAction(ActionType action)
    {
        // Turn tetap terpakai walau tidak ada match (sesuai desain).
        actionInventory.TryUseTurn(action);

        boardManager.ResolveMatches();
    }

    // ───────────── Helper ─────────────

    private bool IsPointerHeld()
    {
        // Di HP: selama masih ada jari yang menempel, input belum selesai.
        return Input.touchCount > 0 || Input.GetMouseButton(0);
    }

    private Vector2 GetPointerWorldPosition()
    {
        Vector3 screenPosition = Input.touchCount > 0
            ? (Vector3)Input.GetTouch(0).position
            : Input.mousePosition;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    // Tetangga = 8 arah (atas, bawah, kiri, kanan, dan diagonal).
    internal static bool IsNeighbor(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        return dx <= 1 && dy <= 1 && dx + dy > 0;
    }

    private static bool IsStraightStep(Vector2Int step)
    {
        return Mathf.Abs(step.x) + Mathf.Abs(step.y) == 1;
    }

    // Jenis swap dari arah langkah antar dua blok bertetangga.
    private static ActionType GetSwapAction(Vector2Int move)
    {
        if (move.x != 0 && move.y != 0)
        {
            return ActionType.Diagonal;
        }

        return move.x != 0 ? ActionType.Horizontal : ActionType.Vertical;
    }

    // Dot product = 0 berarti dua langkah saling tegak lurus.
    private static int Dot(Vector2Int a, Vector2Int b)
    {
        return a.x * b.x + a.y * b.y;
    }

    // Cross product: negatif = belok searah jarum jam (sumbu Y ke atas).
    private static int Cross(Vector2Int a, Vector2Int b)
    {
        return a.x * b.y - a.y * b.x;
    }

    // Membulatkan arah geser ke salah satu dari 8 arah (tiap 45°).
    private static Vector2Int GetSwipeDirection(Vector2 drag)
    {
        float angle = Mathf.Atan2(drag.y, drag.x) * Mathf.Rad2Deg;
        float snapped = Mathf.Round(angle / 45f) * 45f * Mathf.Deg2Rad;

        return new Vector2Int(
            Mathf.RoundToInt(Mathf.Cos(snapped)),
            Mathf.RoundToInt(Mathf.Sin(snapped))
        );
    }

    private void SetCellHighlight(Vector2Int cell, bool highlighted)
    {
        Block block = boardManager.GetBlock(cell.x, cell.y);

        if (block == null)
        {
            return;
        }

        // Blok yang sedang dipilih (tap-tap) tetap menyala.
        if (!highlighted && block == selectedBlock)
        {
            return;
        }

        block.SetHighlighted(highlighted);
    }

    private void SelectBlock(Block block)
    {
        ClearSelection();

        selectedBlock = block;
        selectedBlock.SetHighlighted(true);
    }

    private void ClearSelection()
    {
        if (selectedBlock != null)
        {
            Block previous = selectedBlock;
            selectedBlock = null;
            previous.SetHighlighted(false);
        }

        selectedBlock = null;
    }
}
