using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Deliveries.BlockExeDialog
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string name = GetArgument(args, 0, "-productname:");
            string date = GetArgument(args, 1, "-installdate:");
            DateTime started;
            if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out started))
                date = started.ToLocalTime().ToString("f", CultureInfo.CurrentCulture);
            if (String.IsNullOrWhiteSpace(name)) name = "Unknown installation";
            if (String.IsNullOrWhiteSpace(date)) date = "Unknown";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (Form form = new Form())
            {
                form.Text = "Installation in progress";
                form.StartPosition = FormStartPosition.CenterScreen;
                form.ClientSize = new Size(500, 225);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.Font = new Font("Segoe UI", 9F);
                form.Controls.Add(new Label { Text = "This application is temporarily unavailable", Font = new Font(form.Font, FontStyle.Bold), AutoSize = true, Location = new Point(24, 24) });
                form.Controls.Add(new Label { Text = "Please try again after the installation finishes.", AutoSize = true, Location = new Point(24, 54) });
                form.Controls.Add(new Label { Text = "Installation:", AutoSize = true, Location = new Point(24, 94) });
                form.Controls.Add(new Label { Text = name, AutoEllipsis = true, Location = new Point(115, 94), Size = new Size(355, 22) });
                form.Controls.Add(new Label { Text = "Started:", AutoSize = true, Location = new Point(24, 124) });
                form.Controls.Add(new Label { Text = date, AutoEllipsis = true, Location = new Point(115, 124), Size = new Size(355, 22) });
                Button close = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(395, 177), Size = new Size(80, 28) };
                close.Click += (sender, eventArgs) => form.Close();
                form.Controls.Add(close);
                form.AcceptButton = close;
                form.CancelButton = close;
                Application.Run(form);
            }
        }

        private static string GetArgument(string[] args, int index, string prefix)
        {
            return args.Length > index && args[index].StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? args[index].Substring(prefix.Length) : "";
        }
    }
}
