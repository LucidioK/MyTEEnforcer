using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace MTE
{
    class MyTeEnforcer
    {
        class ScreenCapturer
        {
            [DllImport("user32.dll")]
            private static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

            [StructLayout(LayoutKind.Sequential)]
            private struct Rect
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            public Bitmap Capture() => 
                 CaptureInternal(Screen.GetBounds(Point.Empty));

            private static Bitmap CaptureInternal(Rectangle bounds)
            {
                var result = new Bitmap(bounds.Width, bounds.Height);

                using (var g = Graphics.FromImage(result))
                {
                    g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                }

                return result;
            }

            public Bitmap Capture(string programName)
            {
                var rect = new Rect();
                var found = false;
                foreach (var proc in Process.GetProcessesByName(programName))
                {
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        for (int i = 0; GetWindowRect(proc.MainWindowHandle, ref rect) == IntPtr.Zero && i < 100; i++) {}

                        if (rect.Right > 0 && rect.Bottom > 0)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                if (found)
                { 
                    return CaptureInternal(new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top));
                }
                else
                {
                    System.Console.WriteLine("No Chrome found...");
                    return null;
                }
            }
        }

        private const string submittedVerificationFileName = @"c:\temp\submittedVerification.txt";
        private ScreenCapturer screenCapturer = new ScreenCapturer();

        public bool MustEnforceToday()
        {
            if (CheckIfItWasAlreadySubmitted())
            {
                return false;
            }
            
            var day = DateTime.Now.Day;
            var numberOfDaysOnMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            var midMonth = 15;
            var limitDay = (day <= midMonth) ? midMonth : numberOfDaysOnMonth;

            while (Nager.Date.DateSystem.IsPublicHoliday(FromDay(limitDay), Nager.Date.CountryCode.US) && limitDay > 0)
            {
                limitDay--;
            }
            return limitDay > 1 &&
                   day == limitDay;
        }

        private bool CheckIfItWasAlreadySubmitted() =>
                File.Exists(submittedVerificationFileName) &&
                IsToday(File.GetLastWriteTime(submittedVerificationFileName));

        private bool IsToday(DateTime dateTime) =>
            dateTime.Year == DateTime.Now.Year &&
            dateTime.Month == DateTime.Now.Month &&
            dateTime.Day == DateTime.Now.Day;
                   

        public bool OpenMyTe()
        {
            System.Diagnostics.Process.Start("https://myte.accenture.com/OGTE/secure/AssignmentsPage.aspx");
            System.Threading.Thread.Sleep(5000);
            return true;
        }

        public bool WasSubmitted()
        {
            var screen = screenCapturer.Capture("chrome");
            if (screen == null)
            {
                return false;
            }
            screen.Save(@"c:\temp\screen.bmp");
            using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
            {
                using (var page = engine.Process(screen))
                {
                    var text = page.GetText();
                    System.Diagnostics.Debug.WriteLine($"-->{text}");
                    var wasSubmitted = 
                            text.ToLowerInvariant().Contains("status: submitted") ||
                            text.ToLowerInvariant().Contains("status: processed");
                    if (wasSubmitted)
                    {
                        File.WriteAllText(submittedVerificationFileName, text);
                    }
                    return wasSubmitted;
                }
            }
        }

        public DateTime FromDay(int day) =>
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
    }
}
