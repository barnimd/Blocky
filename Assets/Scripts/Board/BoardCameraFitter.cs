using UnityEngine;

// Mengatur ukuran & posisi kamera supaya board muat di area kosong
// di antara Top Bar dan Bottom Bar HUD, untuk rasio layar apa pun.
[RequireComponent(typeof(Camera))]
public class BoardCameraFitter : MonoBehaviour
{
    [Header("HUD (opsional)")]
    [Tooltip("Board ditempatkan di bawah bar ini.")]
    [SerializeField] private RectTransform topBar;

    [Tooltip("Board ditempatkan di atas bar ini.")]
    [SerializeField] private RectTransform bottomBar;

    [Header("Spacing")]
    [Tooltip("Jarak kosong di sekeliling board (satuan dunia).")]
    [SerializeField, Min(0f)] private float padding = 0.35f;

    private Camera targetCamera;
    private Vector3 boardCenter;
    private Vector2 boardSize;
    private bool hasBoard;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    public void Fit(Vector3 center, Vector2 size)
    {
        boardCenter = center;
        boardSize = size;
        hasBoard = true;

        Apply();
    }

    // Ukuran layar bisa berubah (rotasi HP, resize Game view).
    private void LateUpdate()
    {
        if (hasBoard && lastScreenSize != new Vector2Int(Screen.width, Screen.height))
        {
            Apply();
        }
    }

    private void Apply()
    {
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        // Area kosong dalam viewport (0 = bawah layar, 1 = atas layar).
        Canvas.ForceUpdateCanvases();
        float freeTop = topBar != null ? GetViewportEdge(topBar, false) : 1f;
        float freeBottom = bottomBar != null ? GetViewportEdge(bottomBar, true) : 0f;

        float freeHeight = Mathf.Max(0.2f, freeTop - freeBottom);

        // Tinggi layar (satuan dunia) agar board + padding muat
        // di area kosong secara vertikal DAN di lebar layar.
        float neededForHeight = (boardSize.y + padding * 2f) / freeHeight;
        float neededForWidth = (boardSize.x + padding * 2f) / targetCamera.aspect;
        float viewHeight = Mathf.Max(neededForHeight, neededForWidth);

        targetCamera.orthographicSize = viewHeight / 2f;

        // Geser kamera supaya pusat board jatuh di tengah area kosong.
        float freeCenter = (freeTop + freeBottom) / 2f;
        float offsetY = (freeCenter - 0.5f) * viewHeight;

        Vector3 position = targetCamera.transform.position;
        targetCamera.transform.position =
            new Vector3(boardCenter.x, boardCenter.y - offsetY, position.z);
    }

    // Tepi atas (useTop) atau bawah bar, dalam koordinat viewport.
    // Canvas Screen Space Overlay: world corners = posisi piksel layar.
    private static float GetViewportEdge(RectTransform bar, bool useTop)
    {
        if (!bar.gameObject.activeInHierarchy || Screen.height <= 0)
        {
            return useTop ? 0f : 1f;
        }

        Vector3[] corners = new Vector3[4];
        bar.GetWorldCorners(corners);

        // corners: 0 = kiri-bawah, 1 = kiri-atas.
        float pixelY = useTop ? corners[1].y : corners[0].y;
        return Mathf.Clamp01(pixelY / Screen.height);
    }
}
