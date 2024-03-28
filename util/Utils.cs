using Godot;
using System;
using System.Collections.Generic;

public static partial class Utils
{
#nullable enable
    public static TV GetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defaultValue = default)
    {
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }
#nullable disable

    public static Vector2I NextPos(this Vector2I pos, int dx, int dy)
    {
        return new Vector2I(pos.X + dx, pos.Y + dy);
    }
    public static Vector2I NextPos(this Vector2I pos, DIR dir)
    {
        return new Vector2I(pos.X + dir.DX(), pos.Y + dir.DY());
    }

    public static int DX(this DIR dir)
    {
        switch (dir)
        {
            case DIR.UP: return 0;
            case DIR.DOWN: return 0;
            case DIR.LEFT: return -1;
            case DIR.RIGHT: return 1;
        }
        return 0;
    }
    public static int DY(this DIR dir)
    {
        switch (dir)
        {
            case DIR.UP: return -1;
            case DIR.DOWN: return 1;
            case DIR.LEFT: return 0;
            case DIR.RIGHT: return 0;
        }
        return 0;
    }

    public static DIR GetDirection(int dx, int dy)
    {
        if (dx == 0)
        {
            if (dy == 1)
            {
                return DIR.DOWN;
            } else
            {
                return DIR.UP;
            }
        } else
        {
            if (dx == 1)
            {
                return DIR.RIGHT;
            } else
            {
                return DIR.LEFT;
            }
        }
    }

    public static DIR Opposite(this DIR dir)
    {
        switch (dir)
        {
            case DIR.UP: return DIR.DOWN;
            case DIR.DOWN: return DIR.UP;
            case DIR.LEFT: return DIR.RIGHT;
            case DIR.RIGHT: return DIR.LEFT;
        }
        return DIR.DOWN;
    }
}
public enum DIR
{
    UP = 0,
    LEFT = 1,
    DOWN = 2,
    RIGHT = 3,
}