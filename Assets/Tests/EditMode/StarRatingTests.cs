using NUnit.Framework;

public class StarRatingTests
{
    // Ambang contoh dari JSON GDD: star_3 = 5, star_2 = 2.
    [TestCase(19, 3)]
    [TestCase(5, 3)]
    [TestCase(4, 2)]
    [TestCase(2, 2)]
    [TestCase(1, 1)]
    [TestCase(0, 1)]
    [TestCase(-3, 1)]
    public void Calculate_UsesJsonThresholds(int turnsLeft, int expectedStars)
    {
        Assert.AreEqual(expectedStars, StarRating.Calculate(turnsLeft, 5, 2));
    }

    [Test]
    public void Calculate_WinOnLastMove_IsAlwaysAtLeastOneStar()
    {
        Assert.AreEqual(1, StarRating.Calculate(0, 7, 3));
    }
}
