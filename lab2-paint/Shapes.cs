using System.Drawing.Drawing2D;
using System.Text.Json;
namespace Zen.Paint;

public record ShapeData(string Type, int Color, int Width, Point[] Points);
public abstract class Shape
{
    protected readonly List<Point> Points;
    public Color Color
    {
        get;
    }
    public int Width
    {
        get;
    }
    public Point Start => Points[0];
    public Point End => Points[^1];
    protected Shape(Point start, Color color, int width)
    {
        if (width is < 1 or > 40)
            throw new ArgumentException("Толщина должна быть от 1 до 40.");
        Color = color;
        Width = width;
        Points = [start, start];
    }
    public virtual void MoveEnd(Point point) => Points[^1] = point;
    public void Move(int dx, int dy)
    {
        for (var i = 0; i < Points.Count; i++)
            Points[i] = new Point(Points[i].X + dx, Points[i].Y + dy);
    }
    protected Rectangle Bounds() => new(Math.Min(Start.X, End.X), Math.Min(Start.Y, End.Y), Math.Abs(End.X - Start.X), Math.Abs(End.Y - Start.Y));
    protected Pen Pen() => new(Color, Width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
    public abstract void Draw(Graphics g);
    public abstract bool Contains(Point p);
    protected abstract string Kind
    {
        get;
    }
    public ShapeData Data() => new(Kind, Color.ToArgb(), Width, Points.ToArray());
    public void RestorePoints(Point[] points)
    {
        if (points.Length < 2 || points.Length > 100000 || points.Any(p => Math.Abs((long)p.X) > 100000 || Math.Abs((long)p.Y) > 100000))
            throw new InvalidDataException("Некорректные точки фигуры.");
        Points.Clear();
        Points.AddRange(points);
    }
    protected static double Distance(Point p, Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        var t = dx == 0 && dy == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy), 0, 1);
        return Math.Sqrt(Math.Pow(p.X - a.X - t * dx, 2) + Math.Pow(p.Y - a.Y - t * dy, 2));
    }
}
public sealed class LineShape(Point start, Color color, int width) : Shape(start, color, width)
{
    protected override string Kind => "Line";
    public override void Draw(Graphics g)
    {
        using var pen = Pen();
        g.DrawLine(pen, Start, End);
    }
    public override bool Contains(Point p) => Distance(p, Start, End) <= Width / 2.0 + 5;
}
public sealed class RectShape(Point start, Color color, int width) : Shape(start, color, width)
{
    protected override string Kind => "Rect";
    public override void Draw(Graphics g)
    {
        using var pen = Pen();
        g.DrawRectangle(pen, Bounds());
    }
    public override bool Contains(Point p)
    {
        var r = Bounds();
        r.Inflate(5, 5);
        return r.Contains(p);
    }
}
public sealed class EllipseShape(Point start, Color color, int width) : Shape(start, color, width)
{
    protected override string Kind => "Ellipse";
    public override void Draw(Graphics g)
    {
        using var pen = Pen();
        var r = Bounds();
        if (r.Width > 0 && r.Height > 0)
            g.DrawEllipse(pen, r);
    }
    public override bool Contains(Point p)
    {
        var r = Bounds();
        double rx = Math.Max(1, r.Width / 2.0), ry = Math.Max(1, r.Height / 2.0);
        return Math.Pow((p.X - r.Left - rx) / (rx + 5), 2) + Math.Pow((p.Y - r.Top - ry) / (ry + 5), 2) <= 1;
    }
}
public sealed class PencilShape(Point start, Color color, int width) : Shape(start, color, width)
{
    protected override string Kind => "Pencil";
    public override void MoveEnd(Point p) => Points.Add(p);
    public override void Draw(Graphics g)
    {
        using var pen = Pen();
        g.DrawLines(pen, Points.ToArray());
    }
    public override bool Contains(Point p) => Points.Zip(Points.Skip(1)).Any(pair => Distance(p, pair.First, pair.Second) <= Width / 2.0 + 5);
}
public static class ShapeFactory
{
    public static Shape Create(string type, Point start, Color color, int width) => type switch
    {
        "Line" => new LineShape(start, color, width),
        "Rect" => new RectShape(start, color, width),
        "Ellipse" => new EllipseShape(start, color, width),
        "Pencil" => new PencilShape(start, color, width),
        _ => throw new InvalidDataException("Неизвестный тип фигуры.")
    };
    public static Shape Restore(ShapeData data)
    {
        if (data.Points is null || data.Points.Length < 2)
            throw new InvalidDataException("Нет точек фигуры.");
        var shape = Create(data.Type, data.Points[0], Color.FromArgb(data.Color), data.Width);
        shape.RestorePoints(data.Points);
        return shape;
    }
}
public interface ICommand
{
    void Execute(); void Undo();
}
public sealed class EditCommand(Action execute, Action undo) : ICommand
{
    public void Execute() => execute(); public void Undo() => undo();
}
public sealed class Drawing
{
    private readonly List<Shape> _shapes = [];
    private readonly Stack<ICommand> _undo = [], _redo = [];
    public IReadOnlyList<Shape> Shapes => _shapes.AsReadOnly();
    public void Execute(ICommand command)
    {
        command.Execute();
        _undo.Push(command);
        _redo.Clear();
    }
    public void Add(Shape shape) => Execute(new EditCommand(() => _shapes.Add(shape), () => _shapes.Remove(shape)));
    public void Delete(Shape shape)
    {
        var index = _shapes.IndexOf(shape);
        if (index < 0)
            throw new ArgumentException("Выберите фигуру.");
        Execute(new EditCommand(() => _shapes.Remove(shape), () => _shapes.Insert(index, shape)));
    }
    public void Move(Shape shape, int dx, int dy) => Execute(new EditCommand(() => shape.Move(dx, dy), () => shape.Move(-dx, -dy)));
    public void Clear()
    {
        var old = _shapes.ToArray();
        Execute(new EditCommand(_shapes.Clear, () => _shapes.AddRange(old)));
    }
    public void Undo()
    {
        if (_undo.TryPop(out var command))
        {
            command.Undo();
            _redo.Push(command);
        }
    }
    public void Redo()
    {
        if (_redo.TryPop(out var command))
        {
            command.Execute();
            _undo.Push(command);
        }
    }
    public Shape? HitTest(Point p) => _shapes.LastOrDefault(s => s.Contains(p));
    public void Draw(Graphics g)
    {
        foreach (var shape in _shapes)
            shape.Draw(g);
    }
    public void Save(string path) => Core.Storage.Save(path, _shapes.Select(s => s.Data()).ToArray());
    public void Load(string path)
    {
        var data = Core.Storage.Load<ShapeData[]>(path);
        if (data.Any(x => x is null))
            throw new InvalidDataException("Пустая фигура в файле.");
        var shapes = data.Select(ShapeFactory.Restore).ToArray();
        _shapes.Clear();
        _shapes.AddRange(shapes);
        _undo.Clear();
        _redo.Clear();
    }
}
