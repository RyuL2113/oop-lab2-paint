using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Zen.Ui;
namespace Zen.Paint;

public sealed class Canvas : Panel
{
    public Canvas()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }
}
public sealed class MainForm : ZenForm
{
    private readonly Drawing _drawing = new();
    private readonly Canvas _canvas = new() { Width = 970, Height = 430, BackColor = Color.FromArgb(255, 254, 250), BorderStyle = BorderStyle.FixedSingle };
    private readonly ComboBox _tool = Choice(["Линия", "Прямоугольник", "Эллипс", "Карандаш", "Выделить"]);
    private readonly NumericUpDown _width = new() { Minimum = 1, Maximum = 40, Value = 2, Width = 60 };
    private Color _color = Ink;
    private Shape? _preview, _selected;
    private Point _last, _origin;
    public MainForm() : base("Линия и тишина", "02 / ГРАФИЧЕСКИЙ РЕДАКТОР     ·     Оставьте только необходимое")
    {
        Body.Controls.Add(Row(_tool, Button("Цвет", () => { using var d = new ColorDialog { Color = _color }; if (d.ShowDialog() == DialogResult.OK) _color = d.Color; }), _width,
            Button("Отменить", () => { _drawing.Undo(); Reset(); }), Button("Повторить", () => { _drawing.Redo(); Reset(); }), Button("Очистить", () => { _drawing.Clear(); Reset(); })));
        Body.Controls.Add(_canvas);
        Body.Controls.Add(Row(Button("Открыть", () => FileAction(false)), Button("Сохранить", () => FileAction(true), true), Button("Экспорт PNG", Export)));
        Body.Controls.Add(Caption("Потяните мышью, чтобы нарисовать. Выделение: щелчок и перетаскивание. Delete · Ctrl+Z · Ctrl+Y"));
        _canvas.Paint += (_, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; _drawing.Draw(e.Graphics); _preview?.Draw(e.Graphics); if (_selected != null) { using var pen = new Pen(Red, 1) { DashStyle = DashStyle.Dash }; var points = _selected.Data().Points; var bounds = Rectangle.FromLTRB(points.Min(p => p.X) - 6, points.Min(p => p.Y) - 6, points.Max(p => p.X) + 6, points.Max(p => p.Y) + 6); e.Graphics.DrawRectangle(pen, bounds); } };
        _canvas.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
                return;
            _canvas.Capture = true;
            _origin = _last = e.Location;
            if (_tool.SelectedIndex == 4)
                _selected = _drawing.HitTest(e.Location);
            else
            {
                _selected = null;
                _preview = ShapeFactory.Create(new[] { "Line", "Rect", "Ellipse", "Pencil" }[_tool.SelectedIndex], e.Location, _color, (int)_width.Value);
            }
            _canvas.Invalidate();
        };
        _canvas.MouseMove += (_, e) =>
        {
            if (!_canvas.Capture)
                return;
            if (_preview != null)
                _preview.MoveEnd(e.Location);
            else if (_selected != null)
                _selected.Move(e.X - _last.X, e.Y - _last.Y);
            _last = e.Location;
            _canvas.Invalidate();
        };
        _canvas.MouseUp += (_, _) =>
        {
            if (!_canvas.Capture)
                return;
            _canvas.Capture = false;
            if (_preview != null)
            {
                _drawing.Add(_preview);
                _preview = null;
            }
            else if (_selected != null)
            {
                var dx = _last.X - _origin.X;
                var dy = _last.Y - _origin.Y;
                _selected.Move(-dx, -dy);
                if (dx != 0 || dy != 0)
                    _drawing.Move(_selected, dx, dy);
            }
            _canvas.Invalidate();
        };
        KeyPreview = true;
        KeyDown += (_, e) => Guard(() =>
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                _drawing.Undo();
                Reset();
                e.SuppressKeyPress = true;
            }
            if (e.Control && e.KeyCode == Keys.Y)
            {
                _drawing.Redo();
                Reset();
                e.SuppressKeyPress = true;
            }
            if (e.KeyCode == Keys.Delete && _selected != null)
            {
                _drawing.Delete(_selected);
                Reset();
            }
        });
    }
    private void Reset()
    {
        _preview = _selected = null;
        _canvas.Invalidate();
    }
    private void FileAction(bool save)
    {
        using FileDialog dialog = save ? new SaveFileDialog() : new OpenFileDialog();
        dialog.Filter = "Рисунок JSON|*.json";
        if (dialog.ShowDialog() != DialogResult.OK)
            return;
        if (save)
            _drawing.Save(dialog.FileName);
        else
            _drawing.Load(dialog.FileName);
        Reset();
    }
    private void Export()
    {
        using var dialog = new SaveFileDialog { Filter = "Изображение PNG|*.png" };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;
        using var bitmap = new Bitmap(_canvas.Width, _canvas.Height);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(_canvas.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        _drawing.Draw(g);
        bitmap.Save(dialog.FileName, ImageFormat.Png);
    }
}
