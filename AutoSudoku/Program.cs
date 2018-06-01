using AutoCore;
using SudokuSolverLib;
using System;
using System.Drawing;
using System.Linq;
using System.Text;
using Tesseract;

namespace AutoSudoku
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Single Plyaer Mode?(Y/y)");
            bool singlePlayer = Console.ReadLine().ToLower().Contains("y");

            AdbWrapper adb = new AdbWrapper();

            int[][] board = new int[9][];
            StringBuilder sboard = new StringBuilder();
            Rectangle boardRegion = singlePlayer ? new Rectangle(55, 407, 966, 966) : new Rectangle(55, 330, 966, 966);
            float offset = 5;

            using (var img = adb.GetScreenshot())
            using (var boardImg = img.Clone(boardRegion, img.PixelFormat))
            {
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.TesseractAndCube))
                {
                    engine.SetVariable("tessedit_char_whitelist", "0123456789");
                    for (int i = 0; i < 9; i++)
                    {
                        board[i] = new int[9];
                        for (int j = 0; j < 9; j++)
                        {
                            float x = boardImg.Width / 9f * j + offset;
                            float y = boardImg.Height / 9f * i + offset;
                            float w = boardImg.Width / 9f - offset * 2f;
                            float h = boardImg.Height / 9f - offset * 2f;
                            using (var cell = boardImg.Clone(new RectangleF(x, y, w, h), boardImg.PixelFormat))
                            using (var cellx = new Bitmap(cell, new Size(cell.Width / 4, cell.Height / 4)))
                            using (var page = engine.Process(cellx, PageSegMode.SingleChar))
                            {
                                var text = page.GetText();
                                if (string.IsNullOrWhiteSpace(text))
                                {
                                    board[i][j] = 0;
                                    sboard.Append('.');
                                }
                                else
                                {
                                    board[i][j] = text[0] - '0';
                                    sboard.Append(text[0]);
                                }
                            }
                        }
                        sboard.Append('\n');
                    }
                }
            }

            Console.WriteLine(sboard);
            var option = StringSplitOptions.RemoveEmptyEntries;
            var mod = Console.ReadLine().Split(new char[] { ' ' }, option).Select(a => int.Parse(a)).ToArray();
            while (mod.Length == 3)
            {
                sboard[mod[0] * 10 + mod[1]] = (char)(mod[2] + '0');
                mod = Console.ReadLine().Split(new char[] { ' ' }, option).Select(a => int.Parse(a)).ToArray();
            }
            Console.WriteLine(sboard);

            var puzzle = SudokuPuzzle.FromString(sboard.ToString(), 3, 3);
            puzzle.SolveGrid();
            Console.WriteLine(puzzle);

            int kx = 160 + 75;
            int ky = singlePlayer ? 1460 + 75 : 1356 + 75;
            int cellh = boardRegion.Height / 9;
            int cellw = boardRegion.Width / 9;
            int bx = boardRegion.X + boardRegion.Width / 18;
            int by = boardRegion.Y + boardRegion.Height / 18;

            int index = 0;
            foreach (var node in puzzle.GetNodes())
            {
                int v = node.Value;
                adb.Tap(new Point(kx + v % 5 * 150, ky + v / 5 * 150));
                adb.Tap(new Point(bx + index % 9 * cellw, by + index / 9 * cellh));
                index++;
            }
        }
    }
}
