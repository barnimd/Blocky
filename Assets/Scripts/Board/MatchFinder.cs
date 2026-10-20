using System.Collections.Generic;

public class MatchFinder
{
    private const int MinMatchLength = 3;

    private readonly BoardManager board;

    public MatchFinder(BoardManager board)
    {
        this.board = board;
    }

    // Mengembalikan semua blok yang termasuk match 3+,
    // horizontal maupun vertikal (tanpa duplikat).
    public HashSet<Block> FindAllMatches()
    {
        HashSet<Block> matches = new HashSet<Block>();

        // Setiap baris: mulai dari kiri, melangkah ke kanan.
        for (int y = 0; y < board.Height; y++)
        {
            ScanLine(0, y, 1, 0, board.Width, matches);
        }

        // Setiap kolom: mulai dari bawah, melangkah ke atas.
        for (int x = 0; x < board.Width; x++)
        {
            ScanLine(x, 0, 0, 1, board.Height, matches);
        }

        return matches;
    }

    private void ScanLine(
        int startX,
        int startY,
        int stepX,
        int stepY,
        int length,
        HashSet<Block> matches)
    {
        List<Block> run = new List<Block>();

        for (int i = 0; i < length; i++)
        {
            Block block = board.GetBlock(
                startX + stepX * i,
                startY + stepY * i
            );

            // Warna sama dengan run saat ini ? perpanjang run.
            if (block != null &&
                run.Count > 0 &&
                block.ColorIndex == run[0].ColorIndex)
            {
                run.Add(block);
                continue;
            }

            // Warna berganti (atau sel kosong) ? tutup run lama.
            AddRunIfMatch(run, matches);
            run.Clear();

            if (block != null)
            {
                run.Add(block);
            }
        }

        // Jangan lupa run terakhir di ujung baris/kolom.
        AddRunIfMatch(run, matches);
    }

    private void AddRunIfMatch(List<Block> run, HashSet<Block> matches)
    {
        if (run.Count < MinMatchLength)
        {
            return;
        }

        foreach (Block block in run)
        {
            matches.Add(block);
        }
    }
}