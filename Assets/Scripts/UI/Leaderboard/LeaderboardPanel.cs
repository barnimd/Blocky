using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel leaderboard global Infinite Mode.
// - Dari menu utama: lihat Top 10 + rank pemain sendiri.
// - Dari panel hasil Infinite: isi nama, kirim skor, lalu lihat rank.
[RequireComponent(typeof(CanvasGroup))]
public class LeaderboardPanel : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private LeaderboardRowView[] rows;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text myRankText;

    [Header("Submit (Infinite Game Over)")]
    [SerializeField] private GameObject submitGroup;
    [SerializeField] private TMP_Text runSummaryText;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button submitButton;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    private CanvasGroup canvasGroup;
    private int pendingScore;
    private int pendingSeconds;
    private bool isSubmitting;

    // Naik setiap kali data diminta, supaya hasil request lama
    // (yang datang terlambat) tidak menimpa data baru.
    private int requestVersion;

    public bool IsShown { get; private set; }
    public bool HasSubmitted { get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        SetVisible(false);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(HandleSubmit);
        }

        if (nameInput != null)
        {
            nameInput.characterLimit = LeaderboardClient.MaxNameLength;
        }
    }

    // Menu utama: hanya melihat.
    public void ShowView()
    {
        SetSubmitVisible(false);
        SetVisible(true);
        Refresh();
    }

    // Panel hasil Infinite: kirim skor run barusan.
    public void ShowSubmit(int score, int seconds)
    {
        pendingScore = score;
        pendingSeconds = seconds;

        SetSubmitVisible(!HasSubmitted);

        if (runSummaryText != null)
        {
            runSummaryText.text = $"YOUR RUN  {score:N0}  ·  {RunTimer.Format(seconds)}";
        }

        SetVisible(true);
        PrefillName();
        Refresh();
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private async void PrefillName()
    {
        if (nameInput == null || !string.IsNullOrEmpty(nameInput.text))
        {
            return;
        }

        // Nama acak dari UGS dipakai sebagai saran awal.
        await LeaderboardClient.EnsureSignedInAsync();

        if (this != null && nameInput != null && string.IsNullOrEmpty(nameInput.text))
        {
            nameInput.text = LeaderboardClient.PlayerName;
        }
    }

    private async void Refresh()
    {
        int version = ++requestVersion;

        foreach (LeaderboardRowView row in rows)
        {
            row.Hide();
        }

        SetMyRank(string.Empty);

        if (!LeaderboardClient.IsSupported)
        {
            SetStatus("Leaderboard is offline in this build.");
            return;
        }

        SetStatus("Loading...");

        List<LeaderboardRow> top = await LeaderboardClient.GetTopAsync(rows.Length);
        LeaderboardRow mine = await LeaderboardClient.GetMineAsync();

        // Panel sudah ditutup / scene sudah berganti / ada request lebih baru.
        if (this == null || version != requestVersion)
        {
            return;
        }

        if (top == null)
        {
            SetStatus("Can't reach the leaderboard. Check your connection.");
            return;
        }

        SetStatus(top.Count == 0 ? "No scores yet. Be the first!" : string.Empty);

        for (int i = 0; i < rows.Length; i++)
        {
            if (i < top.Count)
            {
                rows[i].Show(top[i]);
            }
        }

        SetMyRank(mine != null
            ? $"YOUR BEST  #{mine.Rank}  ·  {mine.Score:N0}  ·  {RunTimer.Format(mine.Seconds)}"
            : "Play Infinite Mode to get ranked!");
    }

    private async void HandleSubmit()
    {
        if (isSubmitting || HasSubmitted)
        {
            return;
        }

        string name = LeaderboardClient.SanitizeName(nameInput != null ? nameInput.text : string.Empty);

        if (name.Length < 3)
        {
            SetStatus("Name needs at least 3 letters or numbers.");
            return;
        }

        isSubmitting = true;
        SetSubmitInteractable(false);
        SetStatus("Submitting...");

        LeaderboardRow result = await LeaderboardClient.SubmitAsync(name, pendingScore, pendingSeconds);

        if (this == null)
        {
            return;
        }

        isSubmitting = false;

        if (result == null)
        {
            SetSubmitInteractable(true);
            SetStatus("Submit failed. Check your connection and try again.");
            return;
        }

        HasSubmitted = true;
        SetSubmitVisible(false);
        Refresh();
    }

    private void SetVisible(bool visible)
    {
        IsShown = visible;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    private void SetSubmitVisible(bool visible)
    {
        if (submitGroup != null)
        {
            submitGroup.SetActive(visible);
        }

        SetSubmitInteractable(true);
    }

    private void SetSubmitInteractable(bool interactable)
    {
        if (submitButton != null)
        {
            submitButton.interactable = interactable;
        }

        if (nameInput != null)
        {
            nameInput.interactable = interactable;
        }
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
            statusText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }

    private void SetMyRank(string text)
    {
        if (myRankText != null)
        {
            myRankText.text = text;
        }
    }
}
