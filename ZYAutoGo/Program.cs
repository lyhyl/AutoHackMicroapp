using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AutoCore;

namespace ZYAutoGo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                AdbWrapper adbWrapper = new AdbWrapper();
                using (Bitmap screenshot = adbWrapper.GetScreenshot())
                {
                    using (Form form = new Form())
                    {
                        int maxWinSize = Math.Min(Screen.PrimaryScreen.WorkingArea.Height, Screen.PrimaryScreen.WorkingArea.Width);
                        int maxImgSize = Math.Max(screenshot.Width, screenshot.Height);
                        int scale = (int)Math.Ceiling((double)maxImgSize / (double)maxWinSize);
                        int w = 1080 / scale;
                        int h = 1920 / scale;
                        form.ClientSize = new Size(w, h);
                        form.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                        form.BackgroundImage = screenshot.GetThumbnailImage(w, h, null, IntPtr.Zero);
                        form.MouseClick += Program.Form_MouseClick;
                        form.ShowDialog();
                        Point point = (Point)form.Tag;
                        Random random = new Random();
                        for (; ; )
                        {
                            int dx = (int)((random.NextDouble() * 2.0 - 1.0) * 10.0);
                            int dy = (int)((random.NextDouble() * 2.0 - 1.0) * 10.0);
                            int x = point.X * scale + dx;
                            int y = point.Y * scale + dy;
                            Console.WriteLine(string.Format("tap {0} {1} ({2})", x, y, DateTime.Now));
                            adbWrapper.Tap(x, y, 200);
                            Thread.Sleep(1000 + (int)(random.NextDouble() * 1000.0));
                        }
                    }
                }
            }
            catch (Exception innerException)
            {
                while (innerException != null)
                {
                    Console.WriteLine(innerException.Message);
                    Console.WriteLine(innerException.StackTrace);
                    Console.WriteLine(innerException.Source);
                    innerException = innerException.InnerException;
                }
                Console.ReadKey();
            }
        }

        private static void Form_MouseClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show(string.Format("click {0}", e.Location));
            ((Form)sender).Tag = e.Location;
        }
    }
}
