using System.Collections.Generic;
using UnityEngine;

// Urutan harus sama dengan Block Colors di BoardManager.
public enum BlockColor
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple
}

[System.Serializable]
public class ColorGoal
{
    [SerializeField] private BlockColor color;
    [SerializeField, Min(1)] private int amount = 10;

    public BlockColor Color => color;
    public int Amount => amount;

    public ColorGoal(BlockColor color, int amount)
    {
        this.color = color;
        this.amount = amount;
    }
}

// Data satu stage. Strukturnya mengikuti JSON di GDD (6.1).
[CreateAssetMenu(fileName = "Stage_", menuName = "Blocky/Stage Data")]
public class StageData : ScriptableObject
{
    [SerializeField, Min(1)] private int stageId = 1;

    [Header("Board")]
    [SerializeField, Range(3, 9)] private int width = 8;
    [SerializeField, Range(3, 9)] private int height = 8;

    [Header("Objective (Color Target)")]
    [SerializeField] private List<ColorGoal> goals = new List<ColorGoal>();

    [Header("Action Inventory")]
    [SerializeField, Min(0)] private int horizontalTurns = 8;
    [SerializeField, Min(0)] private int verticalTurns = 6;
    [SerializeField, Min(0)] private int diagonalTurns = 3;
    [SerializeField, Min(0)] private int rotateTurns = 2;

    [Header("Star Thresholds (sisa total turn minimal)")]
    [SerializeField, Min(0)] private int star3MinTurnsLeft = 5;
    [SerializeField, Min(0)] private int star2MinTurnsLeft = 2;

    public int StageId => stageId;
    public int Width => width;
    public int Height => height;
    public IReadOnlyList<ColorGoal> Goals => goals;

    public int HorizontalTurns => horizontalTurns;
    public int VerticalTurns => verticalTurns;
    public int DiagonalTurns => diagonalTurns;
    public int RotateTurns => rotateTurns;

    public int TotalTurns =>
        horizontalTurns + verticalTurns + diagonalTurns + rotateTurns;

    public int Star3MinTurnsLeft => star3MinTurnsLeft;
    public int Star2MinTurnsLeft => star2MinTurnsLeft;

    // 1 bintang = menang dengan sisa turn berapa pun.
    public int GetStars(int turnsLeft)
    {
        return StarRating.Calculate(turnsLeft, star3MinTurnsLeft, star2MinTurnsLeft);
    }

    private void OnValidate()
    {
        // Ambang bintang 2 tidak boleh lebih tinggi dari bintang 3.
        star2MinTurnsLeft = Mathf.Min(star2MinTurnsLeft, star3MinTurnsLeft);
    }
}
