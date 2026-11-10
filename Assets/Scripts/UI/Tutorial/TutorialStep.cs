using UnityEngine;

// Di mana kartu penjelasan muncul relatif ke area yang di-highlight.
public enum TutorialCardPlacement
{
    Auto,   // Otomatis di seberang target (target di atas → kartu di bawah).
    Top,
    Bottom,
    Center
}

// Satu langkah tutorial: judul, isi, dan UI yang di-highlight.
// Diisi oleh BlockyTutorialSetup, tapi teksnya bisa diubah di Inspector.
[System.Serializable]
public class TutorialStep
{
    public string title;

    [TextArea(2, 5)]
    public string body;

    [Tooltip("UI yang di-highlight. Kosong = tanpa spotlight (layar redup penuh).")]
    public RectTransform target;

    public TutorialCardPlacement placement = TutorialCardPlacement.Auto;

    public TutorialStep(string title, string body, RectTransform target,
        TutorialCardPlacement placement = TutorialCardPlacement.Auto)
    {
        this.title = title;
        this.body = body;
        this.target = target;
        this.placement = placement;
    }
}
