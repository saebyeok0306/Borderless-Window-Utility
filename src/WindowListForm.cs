using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BorderlessWindowUtility
{
    /// <summary>Lists visible windows with their process names so a game can be found and added to profiles.ini.</summary>
    internal sealed class WindowListForm : Form
    {
        private readonly ListView list;
        private readonly TextBox nameBox, processBox, titleBox, widthBox, heightBox;
        private readonly ComboBox alignBox;

        public WindowListForm()
        {
            Ui.Setup(this, I18n.Tr("window_list_title"));
            int S(int px) => Ui.Scale(this, px);
            ClientSize = new Size(S(900), S(580));
            MinimumSize = new Size(S(640), S(480));

            list = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false,
                Dock = DockStyle.Fill,
            };
            list.Columns.Add(I18n.Tr("col_process"), S(180));
            list.Columns.Add(I18n.Tr("col_title"), S(380));
            list.Columns.Add(I18n.Tr("col_size"), S(90));
            list.Columns.Add(I18n.Tr("col_position"), S(90));
            list.Columns.Add(I18n.Tr("col_profile"), S(120));
            list.SelectedIndexChanged += (s, e) => OnSelect();

            nameBox = new TextBox { Width = S(280) };
            processBox = new TextBox { Width = S(280) };
            titleBox = new TextBox { Width = S(280) };
            var (width, height) = DefaultGameSize();
            widthBox = new TextBox { Width = S(70), Text = width.ToString() };
            heightBox = new TextBox { Width = S(70), Text = height.ToString() };
            alignBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = S(100) };
            foreach (string align in Profiles.Alignments)
                alignBox.Items.Add(I18n.Tr("align_" + align));
            alignBox.SelectedIndex = Array.IndexOf(Profiles.Alignments, "center");

            var fields = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, Dock = DockStyle.Top };
            fields.Controls.Add(Ui.Label(I18n.Tr("field_name")), 0, 0);
            fields.Controls.Add(nameBox, 1, 0);
            fields.Controls.Add(Ui.Label(I18n.Tr("field_process")), 0, 1);
            fields.Controls.Add(processBox, 1, 1);
            fields.Controls.Add(Ui.Label(I18n.Tr("field_title")), 0, 2);
            fields.Controls.Add(titleBox, 1, 2);
            fields.Controls.Add(Ui.Label(I18n.Tr("field_title_hint")), 2, 2);
            fields.Controls.Add(Ui.Label(I18n.Tr("field_width")), 0, 3);
            fields.Controls.Add(Ui.Row(widthBox, Ui.Label(I18n.Tr("field_height")), heightBox), 1, 3);
            fields.Controls.Add(Ui.Label(I18n.Tr("field_align")), 0, 4);
            fields.Controls.Add(Ui.Row(alignBox, Ui.Label(I18n.Tr("field_align_hint"))), 1, 4);
            fields.Controls.Add(Ui.Button(I18n.Tr("add_to_profiles"), (s, e) => OnAdd()), 1, 5);

            var group = new GroupBox
            {
                Text = I18n.Tr("add_profile"),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Padding = new Padding(S(8)),
            };
            group.Controls.Add(fields);

            var close = Ui.Button(I18n.Tr("close"), (s, e) => Close());
            close.Anchor = AnchorStyles.Right;
            CancelButton = close;
            var bottom = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0) };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            bottom.Controls.Add(Ui.Button(I18n.Tr("open_profiles"), (s, e) => OpenProfiles()), 0, 0);
            bottom.Controls.Add(close, 1, 0);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(S(10)) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(Ui.Label(I18n.Tr("list_intro")), 0, 0);
            root.Controls.Add(list, 0, 1);
            root.Controls.Add(Ui.Row(Ui.Button(I18n.Tr("refresh"), (s, e) => RefreshList()),
                                     Ui.Button(I18n.Tr("copy_process"), (s, e) => CopyProcess())), 0, 2);
            root.Controls.Add(group, 0, 3);
            root.Controls.Add(bottom, 0, 4);
            Controls.Add(root);

            RefreshList();
        }

        /// <summary>Hides windows the user can't see: tool windows, the desktop and cloaked (suspended UWP) windows.</summary>
        private static bool IsListable(IntPtr hwnd)
        {
            if (Native.GetClassName(hwnd) == "Progman")
                return false;
            if ((Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE) & Native.WS_EX_TOOLWINDOW) != 0)
                return false;
            Native.DwmGetWindowAttribute(hwnd, Native.DWMWA_CLOAKED, out int cloaked, sizeof(int));
            return cloaked == 0;
        }

        private void RefreshList()
        {
            var profiles = Profiles.Read();
            uint ownPid = (uint)Process.GetCurrentProcess().Id;
            var rows = new List<string[]>();
            foreach (var window in WindowOps.FindCandidateWindows())
            {
                IntPtr hwnd = window.Key;
                if (WindowOps.GetProcessId(hwnd) == ownPid || !IsListable(hwnd))
                    continue;
                string process = WindowOps.GetProcessName(hwnd);
                var r = Native.GetWindowRect(hwnd);
                var profile = Profiles.Find(profiles, window.Value, process);
                rows.Add(new[] { process, window.Value, $"{r.Right - r.Left}x{r.Bottom - r.Top}", $"{r.Left},{r.Top}",
                                 profile?.Name ?? "" });
            }

            list.BeginUpdate();
            list.Items.Clear();
            foreach (var row in rows.OrderBy(r => r[0].Length == 0).ThenBy(r => r[0], StringComparer.Ordinal)
                                    .ThenBy(r => r[1].ToLowerInvariant(), StringComparer.Ordinal))
                list.Items.Add(new ListViewItem(row));
            list.EndUpdate();
        }

        private string[] Selected =>
            list.SelectedItems.Count > 0
                ? list.SelectedItems[0].SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text).ToArray()
                : null;

        private void OnSelect()
        {
            var row = Selected;
            if (row == null)
                return;
            string process = row[0], title = row[1];
            nameBox.Text = process.Length > 0 ? System.IO.Path.GetFileNameWithoutExtension(process) : title;
            processBox.Text = process;
            titleBox.Text = process.Length > 0 ? "" : title;
        }

        private void CopyProcess()
        {
            var row = Selected;
            if (row != null && row[0].Length > 0)
                Clipboard.SetText(row[0]);
        }

        /// <summary>16:9 at the full height of the primary monitor, e.g. 2560x1440 on 5120x1440.</summary>
        private static (int Width, int Height) DefaultGameSize()
        {
            var m = WindowOps.GetPrimaryMonitorRect();
            int height = m.Bottom - m.Top;
            return (Math.Min(m.Right - m.Left, height * 16 / 9), height);
        }

        private string ValidateInput(out string name, out int width, out int height)
        {
            name = nameBox.Text.Trim().Replace('[', '(').Replace(']', ')');
            width = height = 0;
            if (name.Length == 0)
                return I18n.Tr("error_name");
            if (processBox.Text.Trim().Length == 0 && titleBox.Text.Trim().Length == 0)
                return I18n.Tr("error_target");
            string candidate = name;
            if (Profiles.Read().Any(p => p.Name == candidate))
                return I18n.Tr("error_duplicate", "name", name);
            if (!int.TryParse(widthBox.Text.Trim(), out width) || !int.TryParse(heightBox.Text.Trim(), out height))
                return I18n.Tr("error_size_number");
            if (width <= 0 || height <= 0)
                return I18n.Tr("error_size_positive");
            return null;
        }

        private void OnAdd()
        {
            string error = ValidateInput(out string name, out int width, out int height);
            if (error != null)
            {
                MessageBox.Show(this, error, I18n.Tr("add_profile"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Profiles.Append(name, processBox.Text.Trim(), titleBox.Text.Trim(), width, height,
                            Profiles.Alignments[alignBox.SelectedIndex]);
            RefreshList();
            MessageBox.Show(this, I18n.Tr("added", "name", name) + "\n\n" + I18n.Tr("added_hint"),
                            I18n.Tr("add_profile"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void OpenProfiles()
        {
            Profiles.EnsureFile();
            Process.Start(new ProcessStartInfo(Paths.ProfileFile) { UseShellExecute = true });
        }
    }
}
