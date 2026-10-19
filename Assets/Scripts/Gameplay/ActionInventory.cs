
using UnityEngine;

public enum ActionType
{
    Horizontal,
    Vertical,
    Diagonal,
    Rotate
}

public class ActionInventory : MonoBehaviour
{
    [Header("Initial Action Turns")]
    [SerializeField, Min(0)] private int horizontalTurns = 8;
    [SerializeField, Min(0)] private int verticalTurns = 6;
    [SerializeField, Min(0)] private int diagonalTurns = 3;
    [SerializeField, Min(0)] private int rotateTurns = 2;

    public int HorizontalTurns => horizontalTurns;
    public int VerticalTurns => verticalTurns;
    public int DiagonalTurns => diagonalTurns;
    public int RotateTurns => rotateTurns;
    // Dipanggil setiap kali jumlah turn suatu aksi berubah.
    public event System.Action<ActionType, int> TurnsChanged;
    public bool HasTurns(ActionType action)
    {
        return GetTurns(action) > 0;
    }

    public int GetTurns(ActionType action)
    {
        switch (action)
        {
            case ActionType.Horizontal:
                return horizontalTurns;

            case ActionType.Vertical:
                return verticalTurns;

            case ActionType.Diagonal:
                return diagonalTurns;

            case ActionType.Rotate:
                return rotateTurns;

            default:
                return 0;
        }
    }

    public bool TryUseTurn(ActionType action)
    {
        if (!HasTurns(action))
        {
            Debug.LogWarning(
                $"Tidak bisa melakukan {action}: jatah turn habis."
            );

            return false;
        }

        switch (action)
        {
            case ActionType.Horizontal:
                horizontalTurns--;
                break;

            case ActionType.Vertical:
                verticalTurns--;
                break;

            case ActionType.Diagonal:
                diagonalTurns--;
                break;

            case ActionType.Rotate:
                rotateTurns--;
                break;

            default:
                return false;
        }
        TurnsChanged?.Invoke(action, GetTurns(action));
        Debug.Log(
            $"{action} digunakan. Sisa turn: {GetTurns(action)}"
        );

        return true;
    }

    public void ResetInventory(
        int horizontal,
        int vertical,
        int diagonal,
        int rotate)
    {
        horizontalTurns = Mathf.Max(0, horizontal);
        verticalTurns = Mathf.Max(0, vertical);
        diagonalTurns = Mathf.Max(0, diagonal);
        rotateTurns = Mathf.Max(0, rotate);
        NotifyAllTurnsChanged();
    }

    public bool AreAllTurnsEmpty()
    {
        return horizontalTurns == 0
            && verticalTurns == 0
            && diagonalTurns == 0
            && rotateTurns == 0;
    }
    private void NotifyAllTurnsChanged()
    {
        TurnsChanged?.Invoke(ActionType.Horizontal, horizontalTurns);
        TurnsChanged?.Invoke(ActionType.Vertical, verticalTurns);
        TurnsChanged?.Invoke(ActionType.Diagonal, diagonalTurns);
        TurnsChanged?.Invoke(ActionType.Rotate, rotateTurns);
    }

    // Supaya HUD ikut berubah saat nilai diedit di
    // Inspector selama Play mode (berguna untuk testing).
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            NotifyAllTurnsChanged();
        }
    }
}
