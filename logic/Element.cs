using Godot;
using System;

public partial class Element : RefCounted
{
    public Type type = Type.None;
    public Side[] side = { Side.None, Side.None, Side.None, Side.None }; // up down left right
    public char[] gate = { ' ', ' ', ' ', ' ' }; // use non UpperCase
    public ElementNode node = null;
    public ElementNode copyNode = null;
    public int Rotation { get; private set; }

    public Element(string typeStr)
    {
        if (typeStr.Length >= 1)
        {
            foreach (Type type in Enum.GetValues<Type>())
            {
                if ((char)type == typeStr[0])
                {
                    this.type = type;
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
                            this.side[i] = side;
                            break;
                        }
                    }
                    if (this.side[i] == Side.None)
                    {
                        this.side[i] = Side.Gate;
                        this.gate[i] = sideChar;
                    }
                }
            }
        }
    }

    public void Rotate(DIR from, DIR to)
    {
        this.Rotation = ((int)to - (int)from + 4) % 4;
    }

    public int GetSlot(DIR dir)
    {
        GD.Print($"before {(int)dir} after {(this.Rotation + (int)dir + 4) % 4}");
        return (this.Rotation + (int)dir + 4) % 4;
    }
    public char GetGate(DIR dir)
    {
        return this.gate[GetSlot(dir)];
    }
    public Side GetSide(DIR dir)
    {
        return this.side[GetSlot(dir)];
    }

    public override string ToString()
    {
        string gateStr = "" + gate[Rotation] + gate[(Rotation + 1) % 4] + gate[(Rotation + 2) % 4] + gate[(Rotation + 3) % 4];
        return $"[{type} gate:{gateStr}]";
    }
}

public enum Type
{
    None = ' ',
    Wall = 'W',
    Player = 'P',
    Target = 'T',
    Box = 'B'
}
public enum Side
{
    None = ' ',
    Gate = 'G',
    Push = 'P',
    Drag = 'D',
}

