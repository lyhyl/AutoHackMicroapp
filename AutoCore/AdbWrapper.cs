using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace AutoCore
{
    public class AdbWrapper
    {
        private string adbPath, adbDir;
        private Random random = new Random();

        public AdbWrapper()
        {
            adbDir = AppDomain.CurrentDomain.BaseDirectory;
            adbPath = Path.Combine(adbDir, "adb.exe");
        }

        public string AdbCommand(string arg, bool waitForResult = false)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = adbPath;
                process.StartInfo.WorkingDirectory = adbDir;
                process.StartInfo.Arguments = arg;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = waitForResult;
                process.StartInfo.RedirectStandardOutput = waitForResult;
                process.StartInfo.RedirectStandardError = waitForResult;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string result = string.Empty;
                if (waitForResult)
                {
                    result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }
                return result;
            }
        }

        public void Tap(int x, int y, int sleep = 200)
        {
            var args = $"shell input tap {x} {y}";
            AdbCommand(args);
            Thread.Sleep(sleep);
        }

        public void Tap2(int x, int y)
        {
            var args = "shell sendevent /dev/input/event5 {0} {1} {2}";

            AdbCommand(string.Format(args, 3, 0x35, x));
            AdbCommand(string.Format(args, 3, 0x36, y));

            AdbCommand(string.Format(args, 1, 330, 1));
            AdbCommand(string.Format(args, 0, 0, 0));

            AdbCommand(string.Format(args, 1, 330, 0));
            AdbCommand(string.Format(args, 0, 0, 0));
        }

        public void Swipe(Point x1, Point x2, int time)
        {
            var args = $"shell input swipe {x1.X} {x1.Y} {x2.X} {x2.Y} {time}";
            AdbCommand(args);
        }

        public void LongPress(Point x, int time) => Swipe(x, x, time);

        public void Text(string text)
        {
            var args = $"shell input text \"{text}\"";
            AdbCommand(args);
        }

        public Bitmap GetScreenshot()
        {
            var fileDirectory = Path.GetTempPath();
            var fileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";
            var createFileArg = $"shell screencap -p /sdcard/adb/{fileName}";
            AdbCommand(createFileArg, true);
            var downloadArg = $"pull /sdcard/adb/{fileName} {fileDirectory}";
            AdbCommand(downloadArg, true);
            var deleteFileArg = $"shell rm /sdcard/adb/{fileName}";
            AdbCommand(deleteFileArg, true);
            var imgFilePath = Path.Combine(fileDirectory, fileName);
            if (!File.Exists(imgFilePath))
                return null;
            return new Bitmap(imgFilePath);
        }

        public IEnumerable<string> GetDeciveID()
        {
            var delim0 = new string[] { Environment.NewLine };
            var delim1 = new string[] { " ", "\t" };
            var option = StringSplitOptions.RemoveEmptyEntries;

            var devicesArg = "devices";
            var result = AdbCommand(devicesArg);

            var lines = result.Split(delim0, option);
            return lines.Skip(1).Select(l => l.Split(delim1, option).First());
        }
    }
}
