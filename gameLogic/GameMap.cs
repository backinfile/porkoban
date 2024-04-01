using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

public partial class GameMap : RefCounted
{
    public int width = 1;
    public int height = 1;
    public readonly Dictionary<Element, bool> boxData = new();
    public readonly Dictionary<Element, bool> floorData = new();
    public string comment = "";
    public string levelName = "";

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
        Dictionary<Element, bool> result = new Dictionary<Element, bool>();
        foreach (var e in boxData.Keys) _FindGateElements(gate, e, result, except);
        return result.Keys.ToList();
    }
    private void _FindGateElements(char gate, Element e, Dictionary<Element, bool> result, Element except, int layer = 0)
    {
        if (e != except && e.ContainsGate(gate)) result[e] = true;
        if (e.swallow != null && layer < 5) _FindGateElements(gate, e.swallow, result, except, layer + 1);
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
        copy.levelName = levelName;
        copy.comment = comment;
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
            else if (target.Type == Type.Finish)
            {
                Element element = GetElement(target.Position);
                if (element == null || element.Type != Type.Player)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void FullWithEmptyElement()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (GetElement(x, y) == null)
                {
                    AddElement(Element.Create("     ", x, y));
                }
            }
        }
    }

    public static GameMap CreateEmpty(int width, int height, bool fullEmptyElement = false)
    {
        var gameMap = new GameMap();
        gameMap.width = width;
        gameMap.height = height;
        if (fullEmptyElement)
        {
            gameMap.FullWithEmptyElement();
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
        if (data.ContainsKey("comment"))
        {
            map.comment = (string)data["comment"];
        }
        return map;
    }

    public bool Save(string fileName, string path)
    {
        var dict = new Godot.Collections.Dictionary();
        dict["width"] = width;
        dict["height"] = height;
        var boxData = new Godot.Collections.Array();
        var floorData = new Godot.Collections.Array();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                {
                    Element e = GetElement(x, y);
                    if (e != null)
                    {
                        boxData.Add((char)e.Type + string.Join("", e.gate));
                    }
                    else
                    {
                        boxData.Add("     ");
                    }
                }
                {
                    Element e = GetFloorElement(x, y);
                    if (e != null)
                    {
                        floorData.Add((char)e.Type + string.Join("", e.gate));
                    }
                    else
                    {
                        floorData.Add("     ");
                    }
                }
            }
        }
        dict["box"] = boxData;
        dict["floor"] = floorData;
        dict["comment"] = comment;

        string jsonStr = Json.Stringify(dict, "    ");
        return Utils.SaveFile(path, fileName + ".json", jsonStr);
    }
}
