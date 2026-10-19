
using UnityEngine;

public class BoardInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ActionInventory actionInventory;
    [SerializeField] private Camera mainCamera;

    [Header("Selection")]
    [SerializeField] private float selectedScale = 1.12f;

    private Block selectedBlock;
    private Vector3 originalScale;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        if (mainCamera == null ||
            boardManager == null ||
            actionInventory == null)
        {
            return;
        }

        Vector3 mousePosition =
            mainCamera.ScreenToWorldPoint(Input.mousePosition);

        Vector2 worldPoint = new Vector2(
            mousePosition.x,
            mousePosition.y
        );

        Collider2D hit = Physics2D.OverlapPoint(worldPoint);

        if (hit == null)
        {
            ClearSelection();
            return;
        }

        Block clickedBlock = hit.GetComponent<Block>();

        if (clickedBlock == null)
        {
            return;
        }

        if (selectedBlock == null)
        {
            SelectBlock(clickedBlock);
            return;
        }

        if (clickedBlock == selectedBlock)
        {
            ClearSelection();
            return;
        }

        int dx = clickedBlock.GridX - selectedBlock.GridX;
        int dy = clickedBlock.GridY - selectedBlock.GridY;

        // Versi ini baru mendukung swap horizontal
        // dan vertical.
        if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1)
        {
            ClearSelection();
            SelectBlock(clickedBlock);
            return;
        }

        ActionType action = Mathf.Abs(dx) == 1
            ? ActionType.Horizontal
            : ActionType.Vertical;

        // Validasi kuota sebelum melakukan swap.
        if (!actionInventory.HasTurns(action))
        {
            Debug.LogWarning(
                $"Jatah {action} habis! Swap dibatalkan."
            );

            ClearSelection();
            return;
        }

        // Konsumsi turn hanya jika swap berhasil.
        if (boardManager.TrySwap(selectedBlock, clickedBlock))
        {
            actionInventory.TryUseTurn(action);
        }

        ClearSelection();
    }

    private void SelectBlock(Block block)
    {
        ClearSelection();

        selectedBlock = block;
        originalScale = block.transform.localScale;

        block.transform.localScale =
            originalScale * selectedScale;
    }

    private void ClearSelection()
    {
        if (selectedBlock != null)
        {
            selectedBlock.transform.localScale = originalScale;
        }

        selectedBlock = null;
    }
}
