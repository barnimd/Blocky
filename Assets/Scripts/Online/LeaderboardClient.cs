using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if BLOCKY_UGS
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
#endif

// Satu baris di leaderboard (sudah dirapikan untuk UI).
public class LeaderboardRow
{
    public int Rank;        // Mulai dari 1.
    public string Name;
    public int Score;
    public int Seconds;     // Lama permainan (stopwatch).
    public bool IsMe;
}

// Penghubung ke Unity Gaming Services (UGS) Leaderboards.
// Kode online hanya ikut di-compile jika package
// com.unity.services.leaderboards terpasang (define BLOCKY_UGS diatur
// otomatis lewat Version Defines di Blocky.asmdef). Tanpa package,
// game tetap jalan dan leaderboard menampilkan status "offline".
public static class LeaderboardClient
{
    // Harus sama persis dengan ID leaderboard di Unity Cloud Dashboard.
    public const string LeaderboardId = "infinite_score";

    public const int MaxNameLength = 16;

    private static Task<bool> signInTask;

    // Nama pemain tanpa tag "#1234" yang ditambahkan UGS.
    public static string PlayerName { get; private set; } = string.Empty;

    public static bool IsSupported
    {
        get
        {
#if BLOCKY_UGS
            return true;
#else
            return false;
#endif
        }
    }

    // Login anonim (tanpa akun). Hanya dijalankan sekali; kalau gagal
    // (misal tidak ada internet), percobaan berikutnya mengulang.
    public static Task<bool> EnsureSignedInAsync()
    {
        if (signInTask == null || (signInTask.IsCompleted && !signInTask.Result))
        {
            signInTask = SignInAsync();
        }

        return signInTask;
    }

    // Kirim skor. Leaderboard di dashboard diatur "Keep Best",
    // jadi hanya skor tertinggi pemain yang tersimpan.
    // Mengembalikan entry pemain (rank terbaru) atau null jika gagal.
    public static async Task<LeaderboardRow> SubmitAsync(string playerName, int score, int seconds)
    {
#if BLOCKY_UGS
        if (!await EnsureSignedInAsync())
        {
            return null;
        }

        try
        {
            string cleanName = SanitizeName(playerName);

            if (cleanName.Length > 0 && cleanName != PlayerName)
            {
                string fullName = await AuthenticationService.Instance.UpdatePlayerNameAsync(cleanName);
                PlayerName = StripTag(fullName);
            }

            AddPlayerScoreOptions options = new AddPlayerScoreOptions
            {
                Metadata = new Dictionary<string, string> { { "time", seconds.ToString() } }
            };

            LeaderboardEntry entry =
                await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score, options);

            // Jika skor baru tidak lebih tinggi, entry = rekor lama
            // (lengkap dengan waktunya sendiri).
            int savedSeconds = ReadSeconds(entry.Metadata);
            return ToRow(entry, savedSeconds > 0 ? savedSeconds : seconds);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"LeaderboardClient: gagal kirim skor. {exception.Message}");
            return null;
        }
#else
        await Task.Yield();
        return null;
#endif
    }

    // Top N pemain. Null = gagal (offline / belum di-setup).
    public static async Task<List<LeaderboardRow>> GetTopAsync(int limit)
    {
#if BLOCKY_UGS
        if (!await EnsureSignedInAsync())
        {
            return null;
        }

        try
        {
            LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(
                LeaderboardId,
                new GetScoresOptions { Limit = limit, IncludeMetadata = true }
            );

            List<LeaderboardRow> rows = new List<LeaderboardRow>();

            foreach (LeaderboardEntry entry in page.Results)
            {
                rows.Add(ToRow(entry, ReadSeconds(entry.Metadata)));
            }

            return rows;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"LeaderboardClient: gagal ambil leaderboard. {exception.Message}");
            return null;
        }
#else
        await Task.Yield();
        return null;
#endif
    }

    // Entry milik pemain sendiri. Null = belum pernah kirim skor / gagal.
    public static async Task<LeaderboardRow> GetMineAsync()
    {
#if BLOCKY_UGS
        if (!await EnsureSignedInAsync())
        {
            return null;
        }

        try
        {
            LeaderboardEntry entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(
                LeaderboardId,
                new GetPlayerScoreOptions { IncludeMetadata = true }
            );

            return ToRow(entry, ReadSeconds(entry.Metadata));
        }
        catch (LeaderboardsException exception)
            when (exception.Reason == LeaderboardsExceptionReason.EntryNotFound)
        {
            return null;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"LeaderboardClient: gagal ambil skor pemain. {exception.Message}");
            return null;
        }
#else
        await Task.Yield();
        return null;
#endif
    }

    // UGS menolak spasi & simbol aneh: spasi → "_", sisanya dibuang.
    public static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        foreach (char c in name.Trim())
        {
            if (builder.Length >= MaxNameLength)
            {
                break;
            }

            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                builder.Append(c);
            }
            else if (c == ' ')
            {
                builder.Append('_');
            }
        }

        return builder.ToString();
    }

    // "Bara#1234" → "Bara".
    public static string StripTag(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            return string.Empty;
        }

        int hash = fullName.LastIndexOf('#');
        return hash > 0 ? fullName.Substring(0, hash) : fullName;
    }

    // Metadata dari server berupa JSON, misal {"time":"95"}.
    public static int ReadSeconds(string metadataJson)
    {
        if (string.IsNullOrEmpty(metadataJson))
        {
            return 0;
        }

        try
        {
            Metadata data = JsonUtility.FromJson<Metadata>(metadataJson);
            return data != null && int.TryParse(data.time, out int seconds) ? seconds : 0;
        }
        catch (System.ArgumentException)
        {
            return 0;
        }
    }

    [System.Serializable]
    private class Metadata
    {
        public string time;
    }

#if BLOCKY_UGS
    private static async Task<bool> SignInAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // Pemain baru otomatis dapat nama acak dari UGS.
            PlayerName = StripTag(await AuthenticationService.Instance.GetPlayerNameAsync());
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"LeaderboardClient: login gagal. {exception.Message}");
            return false;
        }
    }

    private static LeaderboardRow ToRow(LeaderboardEntry entry, int seconds)
    {
        return new LeaderboardRow
        {
            Rank = entry.Rank + 1, // UGS mulai dari 0.
            Name = StripTag(entry.PlayerName),
            Score = (int)System.Math.Round(entry.Score),
            Seconds = seconds,
            IsMe = entry.PlayerId == AuthenticationService.Instance.PlayerId
        };
    }
#else
    private static Task<bool> SignInAsync()
    {
        return Task.FromResult(false);
    }
#endif
}
