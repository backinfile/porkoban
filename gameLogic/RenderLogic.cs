using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

public partial class RenderLogic : Node
{
    public const double MOVE_INTERVAL = 0.13;
    private static readonly Dictionary<Element, ElementNode> nodeMap = new();

    public static async Task UpdateGameMap(GameMap map, List<Element> removed = null)
    {
        if (removed != null && removed.Count > 0)
        {
            foreach (var e in removed)
            {
                ElementNode node = GetElementNode(e, false);
                if (node != null)
                {
                    Game.Instance.RemoveElementNode(node);
                }
            }
        }

        //RefreshRender(map);
        foreach (var e in map.boxData.Keys)
        {
            MoveRe(map, e);
        }

        if (!EditorLogic.InEditorMode)
        {
            await Game.Wait(MOVE_INTERVAL);
        }

        foreach (var e in map.boxData.Keys)
        {
            CreateElementNodeRe(map, e);
        }

        // set element march state
        RefreshElementMarch(map);
    }

    private static void MoveRe(GameMap gameMap, Element e, int layer = 0)
    {
        if (e == null) return;
        ElementNode node = GetElementNode(e, false);
        if (node != null)
        {
            var tween = node.CreateTween();
            tween.Parallel().TweenProperty(node, "position", CalcNodePosition(gameMap, e.Position), MOVE_INTERVAL);
            tween.Parallel().TweenProperty(node, "scale", Res.Scale_Normal * Mathf.Pow(Res.Scale_Swallow_f, layer), MOVE_INTERVAL);
            tween.Parallel().TweenProperty(node, "modulate", new Color(1, 1, 1, layer == 0 ? 1 : 0.8f), MOVE_INTERVAL);
            node.ZIndex = (layer == 0) ? Res.Z_Ground : (Res.Z_Swallow + layer);
        }
        MoveRe(gameMap, e.swallow, layer + 1);
    }

    public static void RefreshElementMarch(GameMap gameMap)
    {
        foreach(var e in gameMap.boxData.Keys)
        {
            SetElementMarchRe(gameMap, e);
        }
    }

    private static void SetElementMarchRe(GameMap gameMap, Element e, int layer = 0)
    {
        if (e == null) return;
        ElementNode node = GetElementNode(e, false);
        if (node != null)
        {
            bool march = false;
            if (layer == 0 && !EditorLogic.InEditorMode)
            {
                if (e.Type == Type.Player)
                {
                    Element floor = gameMap.GetFloorElement(e.Position);
                    if (floor != null && floor.Type == Type.Finish) march = true;
                }
                else if (e.Type == Type.Box)
                {
                    Element floor = gameMap.GetFloorElement(e.Position);
                    if (floor != null && floor.Type == Type.Target) march = true;
                }
            }
            node.SetMarch(march);
        }
        SetElementMarchRe(gameMap, e.swallow, layer + 1);
    }

    public static void RefreshRender(GameMap gameMap)
    {
        nodeMap.Clear();
        var game = Game.Instance;
        Node world = game.GetNode("World");

        foreach (var node in world.GetChildren())
        {
            world.RemoveChild(node);
            node.QueueFree();
        }

        { // add background
            Vector2 startPosition = CalcNodePosition(gameMap);
            var background = new ColorRect();
            background.Color = Colors.LightGray;
            background.Position = new Vector2(startPosition.X - ElementNode.Element_Size / 2.0f, startPosition.Y - ElementNode.Element_Size / 2.0f);
            background.Size = new Vector2(gameMap.width * (ElementNode.Element_Size + ElementNode.Element_Gap), gameMap.height * (ElementNode.Element_Size + ElementNode.Element_Gap));
            background.ZIndex = Res.Z_Background;
            world.AddChild(background);
        }

        foreach (var e in gameMap.floorData.Keys.Concat(gameMap.boxData.Keys))
        {
            CreateElementNodeRe(gameMap, e);
        }

        {
            string levelName = gameMap.levelName.Length > 0 ? $"[{gameMap.levelName}]" : "";
            Game.Instance.GetNode<Label>("%LevelCommentLabel").Text = $"{levelName}{gameMap.comment}";
        }

        OnSizeChanged();

        // set element march state
        RefreshElementMarch(gameMap);
    }

    public static void OnSizeChanged()
    {
        GameMap gameMap = GameLogic.gameMap;
        if (gameMap != null)
        {
            Game.Instance.SetCameraView(
                new Vector2(0, 0),
                new Vector2(ElementNode.Element_Unit_Size * gameMap.width,
                ElementNode.Element_Unit_Size * gameMap.height));
        }
    }

    public static void CreateElementNodeRe(GameMap gameMap, Element e, int layer = 0)
    {
        if (GetElementNode(e, false) == null)
        {
            ElementNode node = GetElementNode(e);
            node.Position = CalcNodePosition(gameMap, e.Position);
            node.Scale = Res.Scale_Normal * Mathf.Pow(Res.Scale_Swallow_f, layer);
            node.ZIndex = (layer == 0) ? Res.Z_Ground : (Res.Z_Swallow + layer);
            node.Modulate = new Color(1, 1, 1, layer == 0 ? 1 : 0.8f);
            Game.Instance.AddElementNode(node);

            if (e.Type.IsFloorType())
            {
                node.ZIndex = Res.Z_Target;
            }
        }
        if (e.swallow != null) CreateElementNodeRe(gameMap, e.swallow, layer + 1);
    }

    internal static ElementNode GetElementNode(Element e, bool createIfNotExist = true)
    {
        if (nodeMap.TryGetValue(e, out var node)) return node;
        if (createIfNotExist)
        {
            node = ElementNode.CreateElementNode(e);
            nodeMap[e] = node;
            return node;
        }
        return null;
    }

    public static Vector2 CalcNodePosition(GameMap gameMap, int x = 0, int y = 0)
    {
        var startX = ElementNode.Element_Size / 2f;// + (ElementNode.Element_Size + ElementNode.Element_Gap);
        var startY = ElementNode.Element_Size / 2f;// + (ElementNode.Element_Size + ElementNode.Element_Gap);
        var fx = startX + x * (ElementNode.Element_Size + ElementNode.Element_Gap);
        var fy = startY + y * (ElementNode.Element_Size + ElementNode.Element_Gap);
        return new Vector2(fx, fy);
    }
    public static Vector2 CalcNodePosition(GameMap gameMap, Vector2I pos)
    {
        return CalcNodePosition(gameMap, pos.X, pos.Y);
    }

    public static void Remove(GameMap gameMap, Element e)
    {
        gameMap.RemoveElement(e);
        ElementNode node = GetElementNode(e, false);
        if (node != null)
        {
            Game.Instance.RemoveElementNode(node);
        }
        nodeMap.Remove(e);
    }
}
