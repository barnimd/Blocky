using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// Player Settings bersama untuk Android, iOS, dan Web (itch.io).
// Dipanggil dari Blocky > Apply Platform Settings. Aman dijalankan berulang.
public static class BlockyPlatformSetup
{
    public const string CompanyName = "barnimd";
    public const string ProductName = "Blocky";
    public const string PackageId = "com.barnimd.blocky";

    // Ukuran canvas web di itch.io: portrait, setengah dari 1080x1920.
    private const int WebWidth = 540;
    private const int WebHeight = 960;
    private const string WebTemplate = "PROJECT:Blocky";

    [MenuItem("Blocky/Apply Platform Settings")]
    public static void Run()
    {
        ApplyCommon();
        ApplyAndroid();
        ApplyIOS();
        ApplyWeb();

        AssetDatabase.SaveAssets();
        Debug.Log($"BlockyPlatformSetup: selesai ({PackageId}).");
    }

    private static void ApplyCommon()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;

        // Game dirancang 1080x1920, jadi kunci ke portrait.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
    }

    private static void ApplyAndroid()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);

        // Google Play mewajibkan 64-bit, dan ARM64 butuh IL2CPP.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    }

    private static void ApplyIOS()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, PackageId);
    }

    private static void ApplyWeb()
    {
        // itch.io tidak selalu mengirim header kompresi yang benar.
        // Gzip + Decompression Fallback = jalan di hosting mana pun.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        PlayerSettings.defaultWebScreenWidth = WebWidth;
        PlayerSettings.defaultWebScreenHeight = WebHeight;

        // Template sendiri di Assets/WebGLTemplates/Blocky (tanpa footer Unity).
        // Catatan: Build Profile "Web" punya Player Settings override sendiri,
        // jadi template juga harus dipilih di profile tersebut.
        PlayerSettings.WebGL.template = WebTemplate;
    }
}
