using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class GameMap : RefCounted
{
    public int width = 1;
    public int height = 1;
    public readonly Dictionary<Element, bool> boxData = new();
    public readonly Dictionary<Element, bool> floorData = new();

    private GameMap() { }

    #region element getter and setter
    public Element GetElement(int x, int y)
    {
        foreach (var element in boxData.Keys)
        {
            if (element.X == x && element.Y == y)
            {
                return element;
            }
        }
        return null;
    }

    public Element GetElement(Vector2I pos)
    {
        return GetElement(pos.X, pos.Y);
    }
    public Element GetFloorElement(int x, int y)
    {
        foreach (var element in floorData.Keys)
        {
            if (element.X == x && element.Y == y)
            {
                return element;
            }
        }
        return null;
    }
    public Element GetFloorElement(Vector2I pos)
    {
        return GetFloorElement(pos.X, pos.Y);
    }


    public void AddElement(Element e)
    {
        boxData[e] = true;
    }

    public void AddFloorElement(Element e)
    {
        floorData[e] = true;
    }


    public void RemoveElement(Element e)
    {
        boxData.Remove(e);
        floorData.Remove(e);
    }

    #endregion


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
        if (e != null && e.Type == Type.Wall) return true;
        return false;
    }

    public List<Element> FindGateElements(char gate, Element except = null)
    {
        return boxData.Keys.Where(e => e != except && e.ContainsGate(gate)).ToList();
    }


    public List<Element> GetPlayers()
    {
        return boxData.Keys.Where(e => e.Type == Type.Player).ToList();
    }

    public GameMap MakeCopy()
    {
        var copy = new GameMap
        {
            width = width,
            height = height
        };
        foreach (var e in boxData.Keys)
        {
            copy.boxData[e.MakeCopy()] = true;
        }
        foreach (var e in floorData.Keys)
        {
            copy.floorData[e.MakeCopy()] = true;
        }
        return copy;
    }

    public bool IsGameOver()
    {
        foreach (var target in floorData.Keys)
        {
            if (target.Type == Type.Target)
            {
                Element element = GetElement(target.Position);
                if (element == null || element.Type != Type.Box)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public static GameMap CreateEmpty(int width, int height, bool fullEmptyElement = false)
    {
        var gameMap = new GameMap();
        gameMap.width = width;
        gameMap.height = height;
        if (fullEmptyElement)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gameMap.AddElement(Element.Create("     ", x, y));
                }
            }
        }
        return gameMap;
    }


    public static GameMap ParseFile(string file)
    {
        string json = FileAccess.GetFileAsString(file);
        if (json == null || json.Length == 0)
        {
            return null;
        }

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
            map.AddElement(Element.Create(typeStr, i % width, i / width));
        }
        for (int i = 0; i < floorData.Length; i++)
        {
            string typeStr = floorData[i];
            if (typeStr.Length == 0 || typeStr[0] == ' ') continue;
            map.AddFloorElement(Element.Create(typeStr, i % width, i / width));
        }
        return map;
    }
}
