using UnityEngine;
using UnityEngine.UI;

// Menampilkan 0-3 bintang: bintang yang didapat menyala, sisanya redup.
// Dipakai di top bar (preview) dan di panel hasil.
public class StarDisplay : MonoBehaviour
{
    [SerializeField] private Image[] stars;
    [SerializeField] private Color litColor = new Color(1f, 0.82f, 0.1f);
    [SerializeField] private Color dimColor = new Color(1f, 1f, 1f, 0.2f);

    public int ShownStars { get; private set; }

    public void SetStars(int count)
    {
        ShownStars = Mathf.Clamp(count, 0, stars.Length);

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].color = i < ShownStars ? litColor : dimColor;
            }
        }
    }
}
