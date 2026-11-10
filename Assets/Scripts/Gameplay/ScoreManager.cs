using System.Collections.Generic;
using UnityEngine;

// Skor Infinite Mode: setiap blok hancur = poin, dikali urutan combo.
public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;

    [Header("Scoring")]
    [SerializeField, Min(1)] private int pointsPerBlock = 10;

    public int Score { get; private set; }

    public event System.Action<int> ScoreChanged;

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

    public void ResetScore()
    {
        Score = 0;
        ScoreChanged?.Invoke(Score);
    }

    // Combo x2 = poin dobel, x3 = tiga kali lipat, dst.
    public void AddCleared(int blockCount, int combo)
    {
        if (blockCount <= 0)
        {
            return;
        }

        Score += blockCount * pointsPerBlock * Mathf.Max(1, combo);
        ScoreChanged?.Invoke(Score);
    }

    private void HandleMatchesCleared(HashSet<Block> blocks, int combo)
    {
        AddCleared(blocks.Count, combo);
    }
}
