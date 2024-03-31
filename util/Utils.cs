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

    public static void Merge<TK, TV>(this IDictionary<TK, TV> dict, IDictionary<TK, TV> other)
    {
        foreach (var item in other)
        {
            dict.Add(item.Key, item.Value);
        }
    }
    public static void Map<TSource>(this IEnumerable<TSource> source, Action<TSource> mapper)
    {
        foreach (var t in source) mapper.Invoke(t);
    }

    public static Vector2I NextPos(this Vector2I pos, int dx, int dy)
    {
        return new Vector2I(pos.X + dx, pos.Y + dy);
    }
    public static Vector2I NextPos(this Vector2I pos, DIR dir)
    {
        return new Vector2I(pos.X + dir.DX(), pos.Y + dir.DY());
    }

    public static int ToRotation(this DIR dir)
    {
        return dir switch
        {
            DIR.UP => 0,
            DIR.DOWN => 180,
            DIR.LEFT => 270,
            DIR.RIGHT => 90,
            _ => 0,
        };
    }

    public static int DX(this DIR dir)
    {
        return dir switch
        {
            DIR.UP => 0,
            DIR.DOWN => 0,
            DIR.LEFT => -1,
            DIR.RIGHT => 1,
            _ => 0,
        };
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
            }
            else
            {
                return DIR.UP;
            }
        }
        else
        {
            if (dx == 1)
            {
                return DIR.RIGHT;
            }
            else
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


    public static List<string> ListFiles(string path, string filterExtension = ".json")
    {
        var result = new List<string>();
        using var dir = DirAccess.Open(path);
        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (!dir.CurrentIsDir() && fileName.EndsWith(filterExtension))
                {
                    result.Add(System.IO.Path.GetFileNameWithoutExtension(fileName));
                }
                fileName = dir.GetNext();
            }
        }
        return result;
    }

    public static bool SaveFile(string path, string fileName, string content)
    {
        DirAccess.MakeDirRecursiveAbsolute(path);
        var fullPath = path + '/' + fileName;
        try
        {
            FileAccess fileAccess = FileAccess.Open(fullPath, FileAccess.ModeFlags.Write);
            if (fileAccess != null)
            {
                fileAccess.StoreString(content);
                fileAccess.Close();
                return true;
            }
        } catch (Exception e)
        {
            GD.PrintErr(e);
        }
        return false;
    }

    public static void ClearChildren(this Node node)
    {
        foreach(var n in node.GetChildren())
        {
            node.RemoveChild(n);
            n.QueueFree();
        }
    }
}
public enum DIR
{
    UP = 0,
    LEFT = 1,
    DOWN = 2,
    RIGHT = 3,
}