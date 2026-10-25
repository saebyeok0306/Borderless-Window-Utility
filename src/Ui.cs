using System;
using System.Drawing;
using System.Windows.Forms;

namespace BorderlessWindowUtility
{
    internal static class Ui
    {
        public static Icon LoadIcon()
        {
            using (var stream = typeof(Ui).Assembly.GetManifestResourceStream("icon.ico"))
                return new Icon(stream);
        }

        public static void Setup(Form form, string title)
        {
            form.Text = title;
            form.Icon = LoadIcon();
            form.Font = SystemFonts.MessageBoxFont;
            form.StartPosition = FormStartPosition.CenterScreen;
        }

        /// <summary>Scales a 96 DPI pixel value to the form's DPI.</summary>
        public static int Scale(Control control, int pixels) => (int)Math.Round(pixels * control.DeviceDpi / 96.0);

        public static Label Label(string text) =>
            new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 6) };

        public static Button Button(string text, EventHandler onClick)
        {
            var button = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6, 1, 6, 1) };
            button.Click += onClick;
            return button;
        }

        public static FlowLayoutPanel Row(params Control[] controls)
        {
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), Anchor = AnchorStyles.Left };
            row.Controls.AddRange(controls);
            return row;
        }
    }
}
