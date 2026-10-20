using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ActionInventory actionInventory;
    [SerializeField] private ActionTurnSlot[] actionSlots;

    [Tooltip("Khusus Infinite Mode. Disembunyikan di Stage Mode.")]
    [SerializeField] private GameObject refreshButton;

    private void OnEnable()
    {
        if (actionInventory != null)
        {
            actionInventory.TurnsChanged += HandleTurnsChanged;
            actionInventory.TurnDenied += HandleTurnDenied;
        }
    }

    private void OnDisable()
    {
        if (actionInventory != null)
        {
            actionInventory.TurnsChanged -= HandleTurnsChanged;
            actionInventory.TurnDenied -= HandleTurnDenied;
        }
    }

    private void Start()
    {
        // Saat ini hanya ada Stage Mode.
        if (refreshButton != null)
        {
            refreshButton.SetActive(false);
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
