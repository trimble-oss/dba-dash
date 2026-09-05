using System;
using System.Globalization;

namespace DBADash.Deadlock.Layout
{
    /// <summary>
    /// A point in layout space.
    ///
    /// This layer defines its own geometry types rather than using System.Drawing (Windows only on
    /// modern .NET) or SkiaSharp (that would tie layout to one renderer).  Layout space is abstract:
    /// the renderer decides what a unit is on screen.
    /// </summary>
    public readonly struct LayoutPoint : IEquatable<LayoutPoint>
    {
        public LayoutPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }

        public LayoutPoint Offset(double dx, double dy) => new(X + dx, Y + dy);

        public bool Equals(LayoutPoint other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object? obj) => obj is LayoutPoint other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static bool operator ==(LayoutPoint left, LayoutPoint right) => left.Equals(right);

        public static bool operator !=(LayoutPoint left, LayoutPoint right) => !left.Equals(right);

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##})", X, Y);
    }

    /// <summary>A width and height in layout space.</summary>
    public readonly struct LayoutSize : IEquatable<LayoutSize>
    {
        public LayoutSize(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double Width { get; }

        public double Height { get; }

        public static LayoutSize Empty => new(0, 0);

        public bool Equals(LayoutSize other) => Width.Equals(other.Width) && Height.Equals(other.Height);

        public override bool Equals(object? obj) => obj is LayoutSize other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Width, Height);

        public static bool operator ==(LayoutSize left, LayoutSize right) => left.Equals(right);

        public static bool operator !=(LayoutSize left, LayoutSize right) => !left.Equals(right);

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0:0.##} x {1:0.##}", Width, Height);
    }

    /// <summary>An axis aligned rectangle in layout space.</summary>
    public readonly struct LayoutRect : IEquatable<LayoutRect>
    {
        public LayoutRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double X { get; }

        public double Y { get; }

        public double Width { get; }

        public double Height { get; }

        public double Left => X;

        public double Top => Y;

        public double Right => X + Width;

        public double Bottom => Y + Height;

        public LayoutPoint Centre => new(X + (Width / 2), Y + (Height / 2));

        public LayoutSize Size => new(Width, Height);

        /// <summary>A rectangle of <paramref name="size"/> centred on <paramref name="centre"/>.</summary>
        public static LayoutRect FromCentre(LayoutPoint centre, LayoutSize size) =>
            new(centre.X - (size.Width / 2), centre.Y - (size.Height / 2), size.Width, size.Height);

        public LayoutRect Offset(double dx, double dy) => new(X + dx, Y + dy, Width, Height);

        /// <summary>Grow the rectangle by <paramref name="amount"/> on every side.</summary>
        public LayoutRect Inflate(double amount) =>
            new(X - amount, Y - amount, Width + (2 * amount), Height + (2 * amount));

        public bool Contains(LayoutPoint point) =>
            point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

        /// <summary>The smallest rectangle containing both this and <paramref name="other"/>.</summary>
        public LayoutRect Union(LayoutRect other)
        {
            var left = Math.Min(Left, other.Left);
            var top = Math.Min(Top, other.Top);
            var right = Math.Max(Right, other.Right);
            var bottom = Math.Max(Bottom, other.Bottom);
            return new LayoutRect(left, top, right - left, bottom - top);
        }

        public bool Equals(LayoutRect other) =>
            X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);

        public override bool Equals(object? obj) => obj is LayoutRect other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public static bool operator ==(LayoutRect left, LayoutRect right) => left.Equals(right);

        public static bool operator !=(LayoutRect left, LayoutRect right) => !left.Equals(right);

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##}) {2:0.##} x {3:0.##}", X, Y, Width, Height);
    }
}
