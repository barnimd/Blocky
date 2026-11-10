// Mengecek apakah masih ada SATU aksi (swap horizontal/vertikal/diagonal
// atau rotate 2x2) yang langsung menghasilkan match 3+.
// Dipakai Infinite Mode untuk mendeteksi papan terkunci (GDD 3.2).
public static class MoveFinder
{
    private const int MinMatchLength = 3;

    // Arah swap cukup ke "depan" saja, karena swap A↔B = swap B↔A.
    private static readonly (int dx, int dy)[] SwapDirections =
    {
        (1, 0),   // horizontal
        (0, 1),   // vertikal
        (1, 1),   // diagonal ↗
        (1, -1)   // diagonal ↘
    };

    // colors[x, y] = ColorIndex, atau -1 untuk sel kosong.
    public static bool HasAnyMatchingMove(int[,] colors)
    {
        int width = colors.GetLength(0);
        int height = colors.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                foreach ((int dx, int dy) in SwapDirections)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    {
                        continue;
                    }

                    // Menukar dua warna yang sama tidak mengubah apa pun.
                    if (colors[x, y] == colors[nx, ny])
                    {
                        continue;
                    }

                    Swap(colors, x, y, nx, ny);
                    bool found = HasMatch(colors);
                    Swap(colors, x, y, nx, ny);

                    if (found)
                    {
                        return true;
                    }
                }
            }
        }

        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                if (RotateCreatesMatch(colors, x, y, true) ||
                    RotateCreatesMatch(colors, x, y, false))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool RotateCreatesMatch(int[,] colors, int x, int y, bool clockwise)
    {
        Rotate(colors, x, y, clockwise);
        bool found = HasMatch(colors);
        Rotate(colors, x, y, !clockwise);

        return found;
    }

    // Urutan putaran sama seperti BoardManager.TryRotate.
    private static void Rotate(int[,] colors, int x, int y, bool clockwise)
    {
        int bottomLeft = colors[x, y];
        int bottomRight = colors[x + 1, y];
        int topRight = colors[x + 1, y + 1];
        int topLeft = colors[x, y + 1];

        if (clockwise)
        {
            colors[x + 1, y + 1] = topLeft;
            colors[x + 1, y] = topRight;
            colors[x, y] = bottomRight;
            colors[x, y + 1] = bottomLeft;
        }
        else
        {
            colors[x, y] = topLeft;
            colors[x + 1, y] = bottomLeft;
            colors[x + 1, y + 1] = bottomRight;
            colors[x, y + 1] = topRight;
        }
    }

    private static void Swap(int[,] colors, int ax, int ay, int bx, int by)
    {
        (colors[ax, ay], colors[bx, by]) = (colors[bx, by], colors[ax, ay]);
    }

    public static bool HasMatch(int[,] colors)
    {
        int width = colors.GetLength(0);
        int height = colors.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int color = colors[x, y];

                if (color < 0)
                {
                    continue;
                }

                if (x + MinMatchLength - 1 < width &&
                    colors[x + 1, y] == color && colors[x + 2, y] == color)
                {
                    return true;
                }

                if (y + MinMatchLength - 1 < height &&
                    colors[x, y + 1] == color && colors[x, y + 2] == color)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
