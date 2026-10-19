using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ActionInventory actionInventory;
    [SerializeField] private ActionTurnSlot[] actionSlots;

    private void OnEnable()
    {
        if (actionInventory != null)
        {
            actionInventory.TurnsChanged += HandleTurnsChanged;
        }
    }

    private void OnDisable()
    {
        if (actionInventory != null)
        {
            actionInventory.TurnsChanged -= HandleTurnsChanged;
        }
    }

    private void Start()
    {
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
        foreach (ActionTurnSlot slot in actionSlots)
        {
            if (slot != null && slot.ActionType == action)
            {
                slot.SetCount(turns);
            }
        }
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