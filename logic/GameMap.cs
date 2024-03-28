using Godot;
using Godot.Collections;

public partial class GameMap : RefCounted
{
    public int width = 1;
    public int height = 1;
    public readonly Dictionary<int, Element> boxData = new();
    public readonly Dictionary<int, Element> floorData = new();
    public readonly Dictionary<char, Element> gateData = new();

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
        foreach(var ch in e.gate)
        {
            if (ch != ' ' && gateData.TryGetValue(ch, out _))
            {
                return true;
            }
        }
        return false;
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
        var copy = new GameMap();
        copy.width = width;
        copy.height = height;
        copy.boxData.Merge(boxData);
        copy.floorData.Merge(boxData);
        copy.gateData.Merge(gateData);
        return copy;
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

    public void ApplyStep(System.Collections.Generic.List<Step> steps, bool reverse = false)
    {
        if (reverse)
        {
            for (int i = steps.Count - 1; i >= 0; i--)
            {
                Step step = steps[i];
                RemoveElement(step.e);
                SetElement(step.e, step.from.X, step.from.Y);

                if (step.intoGate)
                {
                    gateData.Remove(step.gate);
                }
                if (step.outGate)
                {
                    gateData[step.gate] = step.e;
                }
            }
        }
        else
        {
            for (int i = 0; i < steps.Count; i++)
            {
                Step step = steps[i];
                RemoveElement(step.e);
                if (!step.intoGate) SetElement(step.e, step.to.X, step.to.Y);
                if (step.intoGate)
                {
                    gateData[step.gate] = step.e;
                }
                if (step.outGate)
                {
                    gateData.Remove(step.gate);
                }
            }
        }
    }

    public static GameMap ParseFile(string file)
    {
        string json = FileAccess.GetFileAsString(file);
        GameMap map = new GameMap();
        Dictionary data = Json.ParseString(json).AsGodotDictionary();
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
