using NUnit.Framework;

public class MoveFinderTests
{
    // (x + 2y) % 5: tidak ada match, dan tidak ada satu pun aksi
    // (swap H/V/D atau rotate) yang bisa membuat match.
    private static int[,] LockedPattern(int width, int height)
    {
        int[,] colors = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colors[x, y] = (x + 2 * y) % 5;
            }
        }

        return colors;
    }

    [Test]
    public void LockedPattern_HasNoMove()
    {
        int[,] colors = LockedPattern(8, 8);

        Assert.IsFalse(MoveFinder.HasMatch(colors));
        Assert.IsFalse(MoveFinder.HasAnyMatchingMove(colors));
    }

    [Test]
    public void HorizontalSwap_IsFound()
    {
        // Baris bawah: 0 0 1 0 → tukar (2,0)↔(3,0) = 0 0 0 1.
        int[,] colors = LockedPattern(8, 8);
        colors[0, 0] = 0;
        colors[1, 0] = 0;
        colors[2, 0] = 1;
        colors[3, 0] = 0;

        Assert.IsFalse(MoveFinder.HasMatch(colors));
        Assert.IsTrue(MoveFinder.HasAnyMatchingMove(colors));
    }

    [Test]
    public void Search_DoesNotChangeBoard()
    {
        int[,] colors = LockedPattern(8, 8);
        int[,] copy = (int[,])colors.Clone();

        MoveFinder.HasAnyMatchingMove(colors);

        Assert.AreEqual(copy, colors, "Board harus kembali seperti semula.");
    }

    [Test]
    public void EmptyCells_AreIgnored()
    {
        int[,] colors = { { -1, -1, -1 }, { -1, -1, -1 }, { -1, -1, -1 } };

        Assert.IsFalse(MoveFinder.HasMatch(colors));
        Assert.IsFalse(MoveFinder.HasAnyMatchingMove(colors));
    }
}
