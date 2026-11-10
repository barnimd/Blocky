using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Collections.Generic;
using System.Runtime.InteropServices;
#endif

// Tempat penyimpanan data save. Isinya beda per platform:
// - Mobile & Editor: PlayerPrefs.
// - Web (itch.io): localStorage browser dengan key tetap, supaya
//   progres tidak hilang saat game di-upload ulang ke URL baru.
public interface ISaveStore
{
    int GetInt(string key, int fallback);
    void SetInt(string key, int value);
    void DeleteKey(string key);
    void Save();
}

public static class SaveStore
{
    private static ISaveStore current;

    // Dipilih otomatis sesuai platform saat pertama kali dipakai.
    public static ISaveStore Current
    {
        get
        {
            if (current == null)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                current = new WebLocalStorageSaveStore();
#else
                current = new PlayerPrefsSaveStore();
#endif
            }

            return current;
        }
    }
}

public class PlayerPrefsSaveStore : ISaveStore
{
    public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);

    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);

    public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);

    public void Save() => PlayerPrefs.Save();
}

#if UNITY_WEBGL && !UNITY_EDITOR
// Menulis ke window.localStorage lewat Assets/Plugins/WebGL/BlockySave.jslib.
// Semua game HTML di itch.io berbagi domain yang sama, jadi key
// diberi awalan unik agar tidak bentrok dengan game lain.
public class WebLocalStorageSaveStore : ISaveStore
{
    private const string Namespace = "com.barnimd.blocky:";

    // Cache di memori: progres tetap jalan selama sesi
    // walau browser memblokir localStorage (misal mode privat).
    private readonly Dictionary<string, int> cache = new Dictionary<string, int>();

    [DllImport("__Internal")] private static extern int BlockySave_GetInt(string key, int fallback);
    [DllImport("__Internal")] private static extern void BlockySave_SetInt(string key, int value);
    [DllImport("__Internal")] private static extern void BlockySave_DeleteKey(string key);

    public int GetInt(string key, int fallback)
    {
        if (cache.TryGetValue(key, out int cached))
        {
            return cached;
        }

        return BlockySave_GetInt(Namespace + key, fallback);
    }

    public void SetInt(string key, int value)
    {
        cache[key] = value;
        BlockySave_SetInt(Namespace + key, value);
    }

    public void DeleteKey(string key)
    {
        cache.Remove(key);
        BlockySave_DeleteKey(Namespace + key);
    }

    // localStorage langsung tersimpan saat SetInt, tidak perlu flush.
    public void Save()
    {
    }
}
#endif
