using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ActionInventory actionInventory;
    [SerializeField] private ActionTurnSlot[] actionSlots;
    [SerializeField] private GameManager gameManager;

    [Header("Refresh (Infinite Mode)")]
    [Tooltip("Khusus Infinite Mode. Disembunyikan di Stage Mode.")]
    [SerializeField] private GameObject refreshButton;
    [SerializeField] private TMP_Text refreshCountText;
    [SerializeField] private float pulseScale = 1.12f;
    [SerializeField] private float pulseSpeed = 6f;

    private Button refreshButtonComponent;

    private void Awake()
    {
        if (refreshButton != null)
        {
            refreshButtonComponent = refreshButton.GetComponent<Button>();

            if (refreshButtonComponent != null)
            {
                refreshButtonComponent.onClick.AddListener(HandleRefreshClicked);
            }
        }
    }

    private void OnEnable()
    {
        if (actionInventory != null)
        {
            actionInventory.TurnsChanged += HandleTurnsChanged;
            actionInventory.TurnDenied += HandleTurnDenied;
        }

        if (gameManager != null)
        {
            gameManager.RefreshesChanged += HandleRefreshesChanged;
        }
    }

    private void OnDisable()
    {
        if (actionInventory != null)
        {
            actionInventory.TurnsChanged -= HandleTurnsChanged;
            actionInventory.TurnDenied -= HandleTurnDenied;
        }

        if (gameManager != null)
        {
            gameManager.RefreshesChanged -= HandleRefreshesChanged;
        }
    }

    private void Start()
    {
        // Tombol Refresh hanya muncul di Infinite Mode.
        if (refreshButton != null)
        {
            bool isInfinite = gameManager != null && gameManager.IsInfinite;
            refreshButton.SetActive(isInfinite);
        }

        if (actionInventory == null)
        {
            Debug.LogError(
                "HUDManager: Action Inventory belum diisi.",
                this
            );
            return;
        }

        RefreshAllSlots();

        if (gameManager != null)
        {
            HandleRefreshesChanged(gameManager.RefreshesLeft);
        }
    }

    // Saat semua jatah habis dan refresh masih ada, tombol berdenyut
    // supaya pemain tahu harus menekan Refresh.
    private void Update()
    {
        if (refreshButton == null || !refreshButton.activeSelf)
        {
            return;
        }

        bool needsAttention =
            gameManager != null &&
            gameManager.RefreshesLeft > 0 &&
            actionInventory != null &&
            actionInventory.AreAllTurnsEmpty();

        float scale = needsAttention
            ? 1f + (pulseScale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed))
            : 1f;

        refreshButton.transform.localScale = Vector3.one * scale;
    }

    private void HandleTurnsChanged(ActionType action, int turns)
    {
        ActionTurnSlot slot = GetSlot(action);

        if (slot != null)
        {
            slot.SetCount(turns);
        }
    }

    private void HandleTurnDenied(ActionType action)
    {
        ActionTurnSlot slot = GetSlot(action);

        if (slot != null)
        {
            slot.PlayDenied();
        }
    }

    private void HandleRefreshesChanged(int refreshesLeft)
    {
        if (refreshCountText != null && gameManager != null)
        {
            refreshCountText.text = $"{refreshesLeft}/{gameManager.MaxRefreshes}";
        }

        if (refreshButtonComponent != null)
        {
            refreshButtonComponent.interactable = refreshesLeft > 0;
        }
    }

    private void HandleRefreshClicked()
    {
        if (gameManager != null)
        {
            gameManager.UseRefresh();
        }
    }

    public ActionTurnSlot GetSlot(ActionType action)
    {
        foreach (ActionTurnSlot slot in actionSlots)
        {
            if (slot != null && slot.ActionType == action)
            {
                return slot;
            }
        }

        return null;
    }

    private void RefreshAllSlots()
    {
        foreach (ActionTurnSlot slot in actionSlots)
        {
            if (slot != null)
            {
                slot.SetCount(actionInventory.GetTurns(slot.ActionType));
            }
        }
    }
}
