using System.Drawing;
using System.Windows.Forms;

namespace Zen.Ui;

public class ZenForm : Form
{
    public static readonly Color Paper = ColorTranslator.FromHtml("#F7F5F0");
    public static readonly Color Stone = ColorTranslator.FromHtml("#EFECE6");
    public static readonly Color Ink = ColorTranslator.FromHtml("#2B2B2B");
    public static readonly Color Red = ColorTranslator.FromHtml("#C83E3B");
    public static readonly Color Matcha = ColorTranslator.FromHtml("#6B7F6E");
    protected readonly FlowLayoutPanel Body = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(28, 0, 28, 16) };
    public ZenForm(string title, string subtitle)
    {
        Text = title + " · ZEN / ООП";
        Size = new Size(1100, 820);
        MinimumSize = new Size(900, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Paper;
        ForeColor = Ink;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        var heading = new Panel { Dock = DockStyle.Top, Height = 138, Padding = new Padding(32) };
        heading.Controls.Add(new Label { Text = title, Font = new Font("Georgia", 28), AutoSize = true, Location = new Point(30, 27) });
        heading.Controls.Add(new Label { Text = subtitle, AutoSize = true, ForeColor = Matcha, Location = new Point(34, 91) });
        var seal = new Label { Text = "間", BackColor = Red, ForeColor = Paper, TextAlign = ContentAlignment.MiddleCenter, Size = new Size(42, 42), Font = new Font("Yu Gothic", 19) };
        heading.Controls.Add(seal);
        heading.Resize += (_, _) => seal.Location = new Point(heading.ClientSize.Width - 76, 32);
        Controls.Add(Body);
        Controls.Add(heading);
    }
    public static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 6, 0, 10) };
        row.Controls.AddRange(controls);
        return row;
    }
    public static Button Button(string text, Action action, bool accent = false)
    {
        var b = new CalmButton(accent ? Matcha : Stone) { Text = text, AutoSize = true, Height = 38, MinimumSize = new Size(95, 38), Padding = new Padding(12, 4, 12, 4), Margin = new Padding(0, 0, 10, 0), FlatStyle = FlatStyle.Flat, ForeColor = accent ? Paper : Ink, Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = Color.FromArgb(213, 209, 200);
        b.Click += (_, _) => Guard(action);
        return b;
    }
    public static TextBox Input(string placeholder, int width = 240) => new() { PlaceholderText = placeholder, Width = width, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 5, 12, 0) };
    public static ComboBox Choice(string[] values, int width = 150)
    {
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, Margin = new Padding(0, 4, 12, 0) };
        c.Items.AddRange(values);
        c.SelectedIndex = 0;
        return c;
    }
    public static Label Caption(string text) => new() { Text = text, AutoSize = true, MaximumSize = new Size(970, 0), Margin = new Padding(0, 12, 0, 10) };
    public static void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or InvalidDataException or IOException or System.Text.Json.JsonException or System.Net.Sockets.SocketException or UnauthorizedAccessException or OperationCanceledException)
        {
            MessageBox.Show(ex.Message, "Проверьте действие", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    protected async Task GuardAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or InvalidDataException or IOException or System.Net.Sockets.SocketException or OperationCanceledException)
        {
            MessageBox.Show(this, ex.Message, "Соединение", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    protected void OnUi(Action action)
    {
        if (!IsDisposed && IsHandleCreated)
        {
            try
            {
                BeginInvoke(action);
            }
            catch (InvalidOperationException) { }
        }
    }
    public static string? Prompt(string title, string initial = "")
    {
        using var dialog = new Form { Text = title, Size = new Size(470, 180), StartPosition = FormStartPosition.CenterParent, BackColor = Paper, Font = new Font("Segoe UI", 11) };
        var input = Input(title, 405);
        input.Text = initial;
        input.Location = new Point(20, 20);
        var ok = Button("Сохранить", () => dialog.DialogResult = DialogResult.OK, true);
        ok.Location = new Point(20, 70);
        dialog.Controls.AddRange([input, ok]);
        dialog.AcceptButton = ok;
        return dialog.ShowDialog() == DialogResult.OK ? input.Text : null;
    }
}

public sealed class CalmButton : Button
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly Color _rest;
    private Color _from, _to;
    private long _started;
    public CalmButton(Color color)
    {
        _rest = BackColor = color;
        _timer.Tick += (_, _) =>
        {
            var progress = Math.Clamp((Environment.TickCount64 - _started) / 250.0, 0, 1);
            var eased = 1 - Math.Pow(1 - progress, 3);
            BackColor = Color.FromArgb((int)(_from.R + (_to.R - _from.R) * eased), (int)(_from.G + (_to.G - _from.G) * eased), (int)(_from.B + (_to.B - _from.B) * eased));
            FlatAppearance.MouseOverBackColor = BackColor;
            if (progress >= 1)
                _timer.Stop();
        };
    }
    private void Animate(Color target)
    {
        _from = BackColor;
        _to = target;
        _started = Environment.TickCount64;
        _timer.Start();
    }
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Animate(ControlPaint.Light(_rest, .12f));
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Animate(_rest);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _timer.Dispose();
        base.Dispose(disposing);
    }
}
