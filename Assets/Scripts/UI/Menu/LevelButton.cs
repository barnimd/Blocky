using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Satu tombol level di level selector: nomor, 0-3 bintang, atau terkunci.
public class LevelButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private StarDisplay stars;
    [SerializeField] private GameObject lockLabel;

    [Header("Locked State")]
    [SerializeField] private Color lockedColor = new Color(0.25f, 0.27f, 0.4f, 1f);
    [SerializeField] private Color lockedTextColor = new Color(1f, 1f, 1f, 0.35f);

    private int stageIndex;
    private System.Action<int> onClicked;

    public int StageIndex => stageIndex;
    public bool IsUnlocked { get; private set; }
    public int ShownStars => stars != null ? stars.ShownStars : 0;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Setup(
        int index,
        string label,
        bool unlocked,
        int starCount,
        Color chapterColor,
        System.Action<int> clicked)
    {
        stageIndex = index;
        onClicked = clicked;
        IsUnlocked = unlocked;

        if (numberText != null)
        {
            numberText.text = label;
            numberText.color = unlocked ? Color.white : lockedTextColor;
        }

        if (background != null)
        {
            background.color = unlocked ? chapterColor : lockedColor;
        }

        if (stars != null)
        {
            stars.gameObject.SetActive(unlocked);
            stars.SetStars(starCount);
        }

        if (lockLabel != null)
        {
            lockLabel.SetActive(!unlocked);
        }

        if (button != null)
        {
            button.interactable = unlocked;
        }
    }

    private void HandleClick()
    {
        if (IsUnlocked)
        {
            onClicked?.Invoke(stageIndex);
        }
    }
}
