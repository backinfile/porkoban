using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class RenderLogic : Node
{
    public const double MOVE_INTERVAL = 0.13;
    private static readonly Dictionary<Element, ElementNode> nodeMap = new();

    public static async Task UpdateGameMap(GameMap map, List<Element> removed)
    {
        if (removed.Count > 0)
        {
            foreach (var e in removed)
            {
                ElementNode node = GetElementNode(e, false);
                if (node != null)
                {
                    Game.Instance.RemoveElementNode(node);
                }
            }
            Game.Instance.SetProcess(false);
            await Game.Instance.Wait(MOVE_INTERVAL);
            Game.Instance.SetProcess(true);
        }

        //RefreshRender(map);
        foreach (var e in map.boxData.Keys)
        {
            MoveRe(map, e);
        }

        Game.Instance.SetProcess(false);
        await Game.Instance.Wait(MOVE_INTERVAL);
        Game.Instance.SetProcess(true);

        foreach (var e in map.boxData.Keys)
        {
            CreateElementNodeRe(map, e);
        }
    }

    private static void MoveRe(GameMap gameMap, Element e, int layer = 0)
    {
        if (e == null) return;
        ElementNode node = GetElementNode(e, false);
        if (node != null)
        {
            var tween = node.CreateTween();
            tween.TweenProperty(node, "position", Game.CalcNodePosition(gameMap, e.Position), MOVE_INTERVAL);
            tween.TweenProperty(node, "scale", Res.Scale_Normal * Mathf.Pow(Res.Scale_Swallow_f, layer), MOVE_INTERVAL);
            node.ZIndex = (layer == 0) ? Res.Z_Ground : (Res.Z_Swallow + layer);
        }
        MoveRe(gameMap, e.swallow, layer + 1);
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
            Vector2 startPosition = Game.CalcNodePosition(gameMap);
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
    }

    private static void CreateElementNodeRe(GameMap gameMap, Element e, int layer = 0)
    {
        if (GetElementNode(e, false) == null)
        {
            ElementNode node = GetElementNode(e);
            node.Position = Game.CalcNodePosition(gameMap, e.Position);
            node.Scale = Res.Scale_Normal * Mathf.Pow(Res.Scale_Swallow_f, layer);
            node.ZIndex = (layer == 0) ? Res.Z_Ground : (Res.Z_Swallow + layer);
            Game.Instance.AddElementNode(node);
        }
        if (e.swallow != null) CreateElementNodeRe(gameMap, e.swallow, layer + 1);
    }

    private static ElementNode GetElementNode(Element e, bool createIfNotExist = true)
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

    public static void DoMove(List<Step> steps)
    {
        RefreshRender(Game.gameMap);
    }
}
