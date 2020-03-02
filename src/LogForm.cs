using System;
using System.Drawing;
using System.Windows.Forms;

namespace Arbitrader
{
    public partial class LogForm : Form
    {
        public LogForm()
        {
            InitializeComponent();
        }

        public static LogForm Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LogForm();
                }

                return _instance;
            }
        }

        private static LogForm _instance;

        public RichTextBox TextBox
        {
            get
            {
                 return richTextBox_Logs;
            }
        }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }

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

            LogForm form = LogForm.Instance;

            if (!string.IsNullOrEmpty(form.TextBox.Text))
            {
                form.TextBox.AppendText(Environment.NewLine);
            }

            Color color = GetColorByLevel(level);

            var str = string.Format(format, args);
            var now = DateTime.Now;
            var header = string.Format("{0}: {1}", now, str);
            form.TextBox.AppendText(header, color);
            form.TextBox.ScrollToCaret();
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

    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
}
