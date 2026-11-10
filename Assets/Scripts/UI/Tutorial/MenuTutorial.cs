using UnityEngine;
using UnityEngine.UI;

// Tutorial pembuka di menu utama (pertama kali game dibuka):
// menjelaskan Stage Mode vs Infinite Mode, lalu langsung
// mengarahkan pemain ke Stage 1-1 (tempat tutorial gameplay lanjut).
// Tombol HOW TO PLAY memutar ulang semua tutorial.
public class MenuTutorial : MonoBehaviour
{
    [SerializeField] private TutorialOverlay overlay;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private TutorialStep[] introSteps;

    [Tooltip("Stage yang dibuka setelah intro selesai (0 = Stage 1-1).")]
    [SerializeField, Min(0)] private int firstStageIndex = 0;

    public bool IsShowing => overlay != null && overlay.IsPlaying;

    private void Awake()
    {
        if (howToPlayButton != null)
        {
            howToPlayButton.onClick.AddListener(HandleHowToPlay);
        }
    }

    private void Start()
    {
        if (!TutorialProgress.IsDone(TutorialId.Intro))
        {
            PlayIntro();
        }
    }

    private void HandleHowToPlay()
    {
        // Semua tutorial (intro, Stage 1-1, Infinite) tampil lagi.
        TutorialProgress.ResetAll();
        PlayIntro();
    }

    private void PlayIntro()
    {
        if (overlay == null)
        {
            return;
        }

        overlay.Play(introSteps, "PLAY 1-1", completed =>
        {
            TutorialProgress.MarkDone(TutorialId.Intro);

            // SKIP = tetap di menu. Selesai = langsung main Stage 1-1.
            if (completed)
            {
                GameSession.StartStage(firstStageIndex);
            }
        });
    }
}
