using AutoCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AutoSpeedLinkup
{
    class Program
    {
        private const int Rows = 10;
        private const int Cols = 7;
        private const int Offset = 50;
        private const int ROILeft = 50;
        private const int ROIRight = 1029;
        private static int ROITop = 412;
        private static int ROIBottom = 1795;

        private static Color[] DefColors = new Color[]
        {
            Color.FromArgb(175,127,213),
            Color.FromArgb(205,139, 30),
            Color.FromArgb(132,185,215),
            Color.FromArgb(175,193,176),
            Color.FromArgb( 91,156, 79),
            Color.FromArgb(195,116,134),
            Color.FromArgb(212,174, 54),
            Color.FromArgb(197,174,152)
        };

        static void Main(string[] args)
        {
            Console.WriteLine("x for demo");
            bool demo = Console.ReadLine() == "x";

            if (demo)
            {
                ROITop = 268;
                ROIBottom = 1651;
            }

            Color[][] raw = new Color[Rows][];
            int[][] board = new int[Rows][];
            for (int i = 0; i < Rows; i++)
            {
                raw[i] = new Color[Cols];
                board[i] = new int[Cols];
            }

            AdbWrapper adb = new AdbWrapper();
            Rectangle ROI = new Rectangle(ROILeft, ROITop, ROIRight - ROILeft + 1, ROIBottom - ROITop + 1);
            double cwidth = (double)ROI.Width / Cols;
            double cheight = (double)ROI.Height / Rows;
            int icwidth = (int)Math.Round(cwidth);
            int icheight = (int)Math.Round(cheight);
            using (var img = adb.GetScreenshot())
            //using (var img = new Bitmap(@"D:\WeChat Image_20180711125059.jpg"))
            using (var crop = img.Clone(ROI, img.PixelFormat))
            {
                BitmapData srcData = crop.LockBits(
                   new Rectangle(0, 0, crop.Width, crop.Height),
                   ImageLockMode.ReadOnly,
                   PixelFormat.Format32bppArgb);

                for (int i = 0; i < Rows; i++)
                    for (int j = 0; j < Cols; j++)
                    {
                        int oft = 20;
                        var rect = new Rectangle(
                            (int)Math.Round(j * cwidth) + oft,
                            (int)Math.Round(i * cheight) + oft,
                            icwidth - oft * 2,
                            icheight - oft * 2);
                        raw[i][j] = CalculateAverageColor(srcData, rect);
                        board[i][j] = ClosestColorIndex(raw[i][j]);
                    }

                crop.UnlockBits(srcData);
            }

            if (demo)
                using (var sw = new StreamWriter(@"D:\a.txt"))
                {
                    sw.WriteLine("[");
                    for (int i = 0; i < Rows; i++)
                        for (int j = 0; j < Cols; j++)
                        {
                            var c = raw[i][j];
                            sw.WriteLine($"{c.R},{c.G},{c.B};");
                        }
                    sw.WriteLine("]");
                }

            PrintBoard(board);

            Queue<Tuple<Point, Point>> solution = new Queue<Tuple<Point, Point>>();

            for (int i = 0; i < Rows - 1; i++)
            {
                for (int j = 0; j < Cols - 1; j++)
                {
                    if (board[i][j] > 0)
                    {
                        if (board[i][j] == board[i + 1][j])
                        {
                            solution.Enqueue(Tuple.Create(new Point(i, j), new Point(i + 1, j)));
                            board[i][j] = board[i + 1][j] = 0;
                        }
                        if (board[i][j] == board[i][j + 1])
                        {
                            solution.Enqueue(Tuple.Create(new Point(i, j), new Point(i, j + 1)));
                            board[i][j] = board[i][j + 1] = 0;
                        }
                    }
                }
            }

            while (solution.Count < Rows * Cols / 2)
            {
                for (int i = 0; i < Rows; i++)
                {
                    for (int j = 0; j < Cols; j++)
                    {
                        if (board[i][j] > 0)
                        {
                            var tar = BFS(new Point(i, j), board);
                            if (tar.X >= 0)
                            {
                                board[tar.X][tar.Y] = board[i][j] = 0;
                                solution.Enqueue(Tuple.Create(new Point(i, j), tar));
                                Console.WriteLine(solution.Count);
                            }
                        }
                    }
                }
            }

            PrintSolution(solution);

            while (solution.Count > 0)
            {
                var p = solution.Dequeue();
                adb.Tap(ROILeft + p.Item1.Y * icwidth + Offset, ROITop + p.Item1.X * icheight + Offset);
                adb.Tap(ROILeft + p.Item2.Y * icwidth + Offset, ROITop + p.Item2.X * icheight + Offset);
            }

            Console.WriteLine("End");
            Console.ReadKey();
        }

        private struct BFSCell
        {
            public int r, c, d, l;
        }

        private static Point BFS(Point s, int[][] board)
        {
            Queue<BFSCell> q = new Queue<BFSCell>();
            int[] dr = new int[] { 1, 0, -1, 0 };
            int[] dc = new int[] { 0, 1, 0, -1 };
            for (int i = 0; i < 4; i++)
            {
                int nr = s.X + dr[i];
                int nc = s.Y + dc[i];
                if (nr >= 0 && nc >= 0 && nr < Rows && nc < Cols && board[nr][nc] == 0)
                {
                    q.Enqueue(new BFSCell { r = nr, c = nc, d = i, l = 0 });
                }
            }
            while (q.Count > 0)
            {
                BFSCell p = q.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    if (Math.Abs(p.d - i) == 2)
                        continue;
                    int nr = p.r + dr[i];
                    int nc = p.c + dc[i];
                    if (nr >= 0 && nc >= 0 && nr < Rows && nc < Cols)
                    {
                        int nl = p.l;
                        if (p.d != i)
                            nl++;
                        if (nl > 2)
                            continue;
                        if (board[nr][nc] == 0)
                        {
                            BFSCell cell = new BFSCell { r = nr, c = nc, d = i, l = nl };
                            q.Enqueue(cell);
                        }
                        else if (board[nr][nc] == board[s.X][s.Y])
                            return new Point(nr, nc);
                    }
                }
            }
            return new Point(-1, -1);
        }

        private static void PrintSolution(Queue<Tuple<Point, Point>> solution)
        {
            foreach (var item in solution)
            {
                Console.Write(item.Item1);
                Console.WriteLine(item.Item2);
            }
        }

        private static void PrintBoard(int[][] board)
        {
            for (int j = 0; j < Rows; j++)
            {
                for (int i = 0; i < Cols; i++)
                    Console.Write(board[j][i]);
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        private static int ClosestColorIndex(Color c)
        {
            int idx = 0, mdt = int.MaxValue;
            for (int i = 0; i < DefColors.Length; i++)
            {
                int dr = c.R - DefColors[i].R;
                int dg = c.G - DefColors[i].G;
                int db = c.B - DefColors[i].B;
                int dt = dr * dr + dg * dg + db * db;
                if (dt < mdt)
                {
                    mdt = dt;
                    idx = i;
                }
            }
            return idx + 1;
        }

        private static Color CalculateAverageColor(BitmapData srcData, Rectangle region)
        {
            int stride = srcData.Stride;
            long[] totals = new long[] { 0, 0, 0 };

            unsafe
            {
                byte* p = (byte*)srcData.Scan0;

                for (int y = region.Top; y < region.Bottom; y++)
                {
                    for (int x = region.Left; x < region.Right; x++)
                    {
                        for (int color = 0; color < 3; color++)
                        {
                            int idx = (y * stride) + x * 4 + color;

                            totals[color] += p[idx];
                        }
                    }
                }
            }


            int width = region.Width;
            int height = region.Height;
            int avgB = (int)(totals[0] / (width * height));
            int avgG = (int)(totals[1] / (width * height));
            int avgR = (int)(totals[2] / (width * height));

            return Color.FromArgb(avgR, avgG, avgB);
        }
    }
}
