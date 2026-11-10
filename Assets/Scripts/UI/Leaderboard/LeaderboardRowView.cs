using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Satu baris tabel leaderboard: rank, nama, skor, waktu.
public class LeaderboardRowView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;

    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.05f);
    [SerializeField] private Color meColor = new Color(0.2f, 0.6f, 1f, 0.45f);

    // Juara 1-3 diberi warna emas, perak, perunggu.
    private static readonly Color[] PodiumColors =
    {
        new Color(1f, 0.82f, 0.1f),
        new Color(0.8f, 0.85f, 0.95f),
        new Color(0.9f, 0.55f, 0.3f)
    };

    public void Show(LeaderboardRow row)
    {
        gameObject.SetActive(true);

        SetText(rankText, $"#{row.Rank}");
        SetText(nameText, row.IsMe ? $"{row.Name} (you)" : row.Name);
        SetText(scoreText, row.Score.ToString("N0"));
        SetText(timeText, RunTimer.Format(row.Seconds));

        if (rankText != null)
        {
            rankText.color = row.Rank >= 1 && row.Rank <= PodiumColors.Length
                ? PodiumColors[row.Rank - 1]
                : Color.white;
        }

        if (background != null)
        {
            background.color = row.IsMe ? meColor : normalColor;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
