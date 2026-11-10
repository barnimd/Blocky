using NUnit.Framework;

// Fungsi kecil untuk fitur bintang, stopwatch, leaderboard, dan tutorial.
public class FeatureHelperTests
{
    // ───────────── Penjelasan bintang ─────────────

    // Contoh aturan: 3★ ≥ 6, 2★ = 2-5, 1★ = 0-1.
    [TestCase(3, "6+ turns left")]
    [TestCase(2, "2-5 turns left")]
    [TestCase(1, "0-1 turns left")]
    public void DescribeRange_MatchesThresholds(int stars, string expected)
    {
        Assert.AreEqual(expected, StarRating.DescribeRange(stars, 6, 2));
    }

    [Test]
    public void DescribeRange_SingleValues()
    {
        // 3★ ≥ 3, 2★ = 2, 1★ = 0-1.
        Assert.AreEqual("2 turns left", StarRating.DescribeRange(2, 3, 2));
        Assert.AreEqual("0 turns left", StarRating.DescribeRange(1, 3, 1));
        Assert.AreEqual("1 turn left", StarRating.DescribeRange(2, 2, 1));
    }

    [TestCase(0, 2)]   // 1★ → butuh 2 turn untuk 2★.
    [TestCase(1, 1)]
    [TestCase(2, 4)]   // 2★ → butuh 4 turn lagi untuk 3★.
    [TestCase(5, 1)]
    [TestCase(6, 0)]   // Sudah 3★.
    [TestCase(20, 0)]
    public void TurnsToNextStar(int turnsLeft, int expected)
    {
        Assert.AreEqual(expected, StarRating.TurnsToNextStar(turnsLeft, 6, 2));
    }

    // ───────────── Stopwatch ─────────────

    [TestCase(0f, "00:00")]
    [TestCase(9.9f, "00:09")]
    [TestCase(75f, "01:15")]
    [TestCase(3725f, "1:02:05")]
    [TestCase(-5f, "00:00")]
    public void RunTimer_Format(float seconds, string expected)
    {
        Assert.AreEqual(expected, RunTimer.Format(seconds));
    }

    // ───────────── Leaderboard ─────────────

    [TestCase("Bara", "Bara")]
    [TestCase("  Bara Nim  ", "Bara_Nim")]
    [TestCase("a!b@c#", "abc")]
    [TestCase("ThisNameIsWayTooLongForTheBoard", "ThisNameIsWayToo")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void SanitizeName(string input, string expected)
    {
        Assert.AreEqual(expected, LeaderboardClient.SanitizeName(input));
    }

    [TestCase("Bara#1234", "Bara")]
    [TestCase("Bara", "Bara")]
    [TestCase("", "")]
    public void StripTag(string input, string expected)
    {
        Assert.AreEqual(expected, LeaderboardClient.StripTag(input));
    }

    [TestCase("{\"time\":\"95\"}", 95)]
    [TestCase("", 0)]
    [TestCase(null, 0)]
    [TestCase("{\"other\":\"1\"}", 0)]
    public void ReadSeconds(string json, int expected)
    {
        Assert.AreEqual(expected, LeaderboardClient.ReadSeconds(json));
    }

    // ───────────── Tutorial progress ─────────────

    [Test]
    public void TutorialProgress_MarkAndReset()
    {
        string oldPrefix = StageProgress.KeyPrefix;
        StageProgress.KeyPrefix = "blocky.edittest.";

        try
        {
            TutorialProgress.ResetAll();
            Assert.IsFalse(TutorialProgress.IsDone(TutorialId.Stage));

            TutorialProgress.MarkDone(TutorialId.Stage);
            Assert.IsTrue(TutorialProgress.IsDone(TutorialId.Stage));
            Assert.IsFalse(TutorialProgress.IsDone(TutorialId.Intro));

            TutorialProgress.ResetAll();
            Assert.IsFalse(TutorialProgress.IsDone(TutorialId.Stage));
        }
        finally
        {
            TutorialProgress.ResetAll();
            StageProgress.KeyPrefix = oldPrefix;
        }
    }
}
