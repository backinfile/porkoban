using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Element : RefCounted
{
    public int X { get; set; }
    public int Y { get; set; }
    public Vector2I Position { get => new(X, Y); set { X = value.X; Y = value.Y; } }

    public Type Type { get; private set; } = Type.None;
    public Side[] side = { Side.None, Side.None, Side.None, Side.None }; // up down left right
    public char[] gate = { ' ', ' ', ' ', ' ' }; // use non UpperCase

    public int id = -1;
    public Element swallow = null;
    public char swallowGate = ' ';
    public int enterGateIndex = -1;

    private static int IDMax = 0;

    private Element()
    {
        id = IDMax++;
    }

    public Element MakeCopy()
    {
        var copy = new Element
        {
            id = id,
            Type = Type,
            Position = Position,
            side = (Side[])side.Clone(),
            gate = (char[])gate.Clone(),
            swallow = swallow?.MakeCopy(),
            swallowGate = swallowGate,
        };
        return copy;
    }
    public static Element Create(string typeStr, int x = -1, int y = -1)
    {
        var element = new Element();
        element.X = x;
        element.Y = y;
        if (typeStr.Length >= 1)
        {
            foreach (Type type in Enum.GetValues<Type>())
            {
                if ((char)type == typeStr[0])
                {
                    element.Type = type;
                    break;
                }
            }
            for (int i = 0; i < 4 && i + 1 < typeStr.Length; i++)
            {
                char sideChar = typeStr[i + 1];
                if (sideChar != ' ')
                {
                    foreach (Side side in Enum.GetValues<Side>())
                    {
                        if ((char)side == sideChar)
                        {
                            element.side[i] = side;
                            break;
                        }
                    }
                    if (element.side[i] == Side.None)
                    {
                        element.side[i] = Side.Gate;
                        element.gate[i] = sideChar;
                    }
                }
            }
        }
        return element;
    }

    public void Rotate(DIR from, DIR to)
    {
        int Rotation = ((int)to - (int)from + 4) % 4;
        var copySide = (Side[])side.Clone();
        var copyGate = (char[])gate.Clone();
        for (int i = 0; i < copyGate.Length; i++)
        {
            side[(i + Rotation) % 4] = copySide[i];
            gate[(i + Rotation) % 4] = copyGate[i];
        }
        swallow?.Rotate(from, to);
    }
    public bool ContainsGate(char gateChar)
    {
        return gate.Contains(gateChar);
    }
    public bool HasGate(DIR dir)
    {
        return GetGate(dir) != ' ';
    }
    public char GetGate(DIR dir)
    {
        return this.gate[(int)dir];
    }
    public Side GetSide(DIR dir)
    {
        return this.side[(int)dir];
    }

    public List<DIR> GetDIRByGate(char gate, DIR? except = null)
    {
        List<DIR> dirList = new List<DIR>();
        foreach (var dir in Enum.GetValues<DIR>())
        {
            if (dir != except && GetGate(dir) == gate) dirList.Add(dir);
        }
        return dirList;
    }

    public void ClearSwallowState()
    {
        swallow = null;
        swallowGate = ' ';
        enterGateIndex = -1;
    }

    public bool IsFloorElement()
    {
        return Type.IsFloorType();
    }
    public bool CanSwallowOther(DIR dir)
    {
        return swallow == null && HasGate(dir);
    }
    public bool CanEnterFrom(DIR dir)
    {
        return swallow == null && HasGate(dir.Opposite());
    }

    public override string ToString()
    {
        //return $"({Type} gate:{string.Join("", gate)} {((swallow != null) ? "swallow" : "")})";
        return Type.ToString();
    }

    public string ToFullString()
    {
        return $"({Type} {X},{Y} gate:{string.Join("", gate)} {((swallow != null) ? "swallow" : "")})";
    }
}

public enum Type
{
    None = ' ',
    Player = 'P',
    Box = 'B',
    Wall = 'W',
    Target = 'T',
    Finish = 'F',
}
public enum Side
{
    None = ' ',
    Gate = 'G',
    //Push = 'P',
    //Drag = 'D',
}

