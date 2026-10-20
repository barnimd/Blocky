using System.Collections.Generic;
using UnityEngine;

// Progres satu target warna, misal "15 blok Biru".
public class GoalProgress
{
    public BlockColor Color { get; }
    public int Target { get; }
    public int Remaining { get; private set; }
    public bool IsComplete => Remaining == 0;

    public GoalProgress(BlockColor color, int target)
    {
        Color = color;
        Target = Mathf.Max(1, target);
        Remaining = Target;
    }

    // Mengembalikan true jika sisa target berubah.
    public bool Reduce(int amount)
    {
        if (amount <= 0 || Remaining == 0)
        {
            return false;
        }

        // Kelebihan blok tidak membuat sisa jadi minus.
        Remaining = Mathf.Max(0, Remaining - amount);
        return true;
    }
}

// Objective "Color Target": hancurkan sejumlah blok warna tertentu.
public class ObjectiveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;

    private readonly List<GoalProgress> goals = new List<GoalProgress>();

    public IReadOnlyList<GoalProgress> Goals => goals;

    public bool IsComplete
    {
        get
        {
            if (goals.Count == 0)
            {
                return false;
            }

            foreach (GoalProgress goal in goals)
            {
                if (!goal.IsComplete)
                {
                    return false;
                }
            }

            return true;
        }
    }

    // Dipanggil saat daftar target dibuat ulang (stage baru).
    public event System.Action GoalsReset;

    // Dipanggil saat sisa salah satu target berubah.
    public event System.Action<GoalProgress> GoalProgressChanged;

    private void OnEnable()
    {
        if (boardManager != null)
        {
            boardManager.MatchesCleared += HandleMatchesCleared;
        }
    }

    private void OnDisable()
    {
        if (boardManager != null)
        {
            boardManager.MatchesCleared -= HandleMatchesCleared;
        }
    }

    public void Setup(IEnumerable<ColorGoal> stageGoals)
    {
        goals.Clear();

        foreach (ColorGoal goal in stageGoals)
        {
            goals.Add(new GoalProgress(goal.Color, goal.Amount));
        }

        if (goals.Count == 0)
        {
            Debug.LogError("ObjectiveManager: Stage tidak punya target.", this);
        }

        GoalsReset?.Invoke();
    }

    private void HandleMatchesCleared(HashSet<Block> blocks, int combo)
    {
        List<int> colorIndices = new List<int>();

        foreach (Block block in blocks)
        {
            colorIndices.Add(block.ColorIndex);
        }

        RegisterCleared(colorIndices);
    }

    // Hitung blok yang hancur per warna, lalu kurangi target yang cocok.
    // Blok dari chain combo juga ikut dihitung.
    public void RegisterCleared(IEnumerable<int> colorIndices)
    {
        Dictionary<int, int> countByColor = new Dictionary<int, int>();

        foreach (int colorIndex in colorIndices)
        {
            countByColor.TryGetValue(colorIndex, out int count);
            countByColor[colorIndex] = count + 1;
        }

        foreach (GoalProgress goal in goals)
        {
            if (countByColor.TryGetValue((int)goal.Color, out int cleared) &&
                goal.Reduce(cleared))
            {
                GoalProgressChanged?.Invoke(goal);
            }
        }
    }
}
