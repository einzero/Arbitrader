using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arbitrader
{
    public static class Debug
    {
        public static void Error(string format, params object[] args)
        {
            Log(LogLevel.Error, format, args);
        }

        public static void Warning(string format, params object[] args)
        {
            Log(LogLevel.Warning, format, args);
        }

        public static void Info(string format, params object[] args)
        {
            Log(LogLevel.Info, format, args);
        }

        public static void Info(string[] list)
        {
            foreach (var text in list)
            {
                Info(text);
            }
        }

        private static void Log(LogLevel level, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
            {
                return;
            }

            MainForm form = MainForm.Instance;

            if (!string.IsNullOrEmpty(form.Logs.Text))
            {
                form.Logs.AppendText(Environment.NewLine);
            }

            Color color = GetColorByLevel(level);

            var str = string.Format(format, args);
            var now = DateTime.Now;
            var header = string.Format("{0}: {1}", now, str);
            form.Logs.AppendText(header, color);
            form.Logs.ScrollToCaret();
        }

        private static Color GetColorByLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Warning:
                    return Color.DarkOrange;
                case LogLevel.Error:
                    return Color.DarkRed;
            }

            return Color.Black;
        }

        private enum LogLevel
        {
            Info,
            Warning,
            Error,
        }
    }
}
