using System;

public struct Position : IEquatable<Position>
{
    public required int X;
    public required int Y;
    public static Position Invalid => new Position { X = -1, Y = -1 };

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static float Distance(Position a, Position b)
    {
        return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }

    public override bool Equals(object? obj)
    {
        return obj is Position other && Equals(other);
    }

    public bool Equals(Position other)
    {
        return X == other.X && Y == other.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Position left, Position right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Position left, Position right)
    {
        return !(left == right);
    }
}