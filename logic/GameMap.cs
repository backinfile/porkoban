using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class GameMap : RefCounted
{
    public int width = 1;
    public int height = 1;
    public readonly Dictionary<int, Element> boxData = new();
    public readonly Dictionary<int, Element> floorData = new();

    private const int FACTOR = 1000;

#nullable enable
    public Element? GetElement(int x, int y)
    {
        return boxData.GetValue(x * FACTOR + y);
    }
    public Element? GetElement(Vector2I pos)
    {
        return GetElement(pos.X, pos.Y);
    }
    public Element? GetFloorElement(int x, int y)
    {
        return floorData.GetValue(x * FACTOR + y);
    }
    public Element? GetFloorElement(Vector2I pos)
    {
        return GetFloorElement(pos.X, pos.Y);
    }

#nullable disable


    public void SetElement(Element e, int x, int y)
    {
        boxData[x * FACTOR + y] = e;
    }

    public void SetFloorElement(Element e, int x, int y)
    {
        floorData[x * FACTOR + y] = e;
    }

    public bool InMapArea(int x, int y)
    {
        return (0 <= x && x < width && 0 <= y && y < height);
    }
    public bool InMapArea(Vector2I pos)
    {
        return InMapArea(pos.X, pos.Y);
    }

    public bool IsStop(int x, int y)
    {
        if (!InMapArea(x, y)) return true;
        var e = GetElement(x, y);
        if (e != null && e.type == Type.Wall) return true;
        return false;
    }

    // element full, cannot swallow
    public bool IsGateFull(Element e)
    {
        return e.swallows.Count > 0;
    }

    public List<Element> FindGateElements(char gate, Element except = null)
    {
        var list = new List<Element>();
        foreach (var pair in boxData)
        {
            if (pair.Value != except && pair.Value.gate.Contains(gate))
            {
                list.Add(pair.Value);
            }
        }
        return list;
    }

    public Vector2I GetElementPos(Element e)
    {
        foreach (var pair in boxData)
        {
            if (pair.Value == e) return new Vector2I(pair.Key / FACTOR, pair.Key % FACTOR);
        }
        foreach (var pair in floorData)
        {
            if (pair.Value == e) return new Vector2I(pair.Key / FACTOR, pair.Key % FACTOR);
        }
        return new Vector2I(-1, -1);
    }

    public void RemoveElement(Element e)
    {
        foreach (var pair in boxData)
        {
            if (pair.Value == e)
            {
                boxData.Remove(pair.Key);
                break;
            }
        }
        foreach (var pair in floorData)
        {
            if (pair.Value == e)
            {
                boxData.Remove(pair.Key);
                break;
            }
        }
    }

    public Element GetPlayer()
    {
        foreach (var pair in boxData)
        {
            if (pair.Value.type == Type.Player)
            {
                return pair.Value;
            }
        }
        return null;
    }

    public GameMap MakeCopy()
    {
        //var copy = new GameMap();
        //copy.width = width;
        //copy.height = height;
        //copy.boxData.Merge(boxData);
        //copy.floorData.Merge(boxData);
        //copy.gateData.Merge(gateData);
        //return copy;
        throw new NotImplementedException();
    }

    public bool IsGameOver()
    {

        foreach (var pair in floorData)
        {
            if (pair.Value.type == Type.Target)
            {
                Element element = boxData.GetValue(pair.Key);
                if (element != null && element.type == Type.Box)
                {
                    return false;
                }
            }
        }
        return true;
    }


    public static GameMap ParseFile(string file)
    {
        string json = FileAccess.GetFileAsString(file);
        GameMap map = new GameMap();
        Godot.Collections.Dictionary data = Json.ParseString(json).AsGodotDictionary();
        int width = map.width = (int)data["width"];
        int height = map.height = (int)data["height"];

        var boxData = data["box"].AsStringArray();
        var floorData = data["floor"].AsStringArray();
        for (int i = 0; i < boxData.Length; i++)
        {
            string typeStr = boxData[i];
            if (typeStr.Length == 0 || typeStr[0] == ' ') continue;
            map.SetElement(new Element(typeStr), i % width, i / width);
        }
        for (int i = 0; i < floorData.Length; i++)
        {
            string typeStr = floorData[i];
            if (typeStr.Length == 0 || typeStr[0] == ' ') continue;
            map.SetFloorElement(new Element(typeStr), i % width, i / width);
        }
        return map;
    }
}
