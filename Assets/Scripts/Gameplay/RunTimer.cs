using UnityEngine;

// Stopwatch Infinite Mode: mulai dari 0 saat board siap,
// berhenti saat papan terkunci (Game Over).
// Waktunya ditampilkan di HUD, panel hasil, dan dikirim ke leaderboard.
public class RunTimer : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    public float Elapsed { get; private set; }
    public bool IsRunning { get; private set; }

    // Dihentikan sementara, misal saat tutorial sedang tampil.
    public bool IsPaused { get; set; }

    public int ElapsedSeconds => Mathf.FloorToInt(Elapsed);

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (!gameManager.IsInfinite)
        {
            IsRunning = false;
            return;
        }

        switch (state)
        {
            case GameState.Init:
                Elapsed = 0f;
                IsRunning = false;
                break;

            case GameState.PlayerInput:
            case GameState.Resolving:
                IsRunning = true;
                break;

            default:
                IsRunning = false;
                break;
        }
    }

    private void Update()
    {
        if (IsRunning && !IsPaused)
        {
            Elapsed += Time.deltaTime;
        }
    }

    // 75 detik → "01:15", 3725 detik → "1:02:05".
    public static string Format(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int hours = total / 3600;
        int minutes = total / 60 % 60;
        int secs = total % 60;

        return hours > 0
            ? $"{hours}:{minutes:00}:{secs:00}"
            : $"{minutes:00}:{secs:00}";
    }
}
