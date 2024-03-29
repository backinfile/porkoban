using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class RenderLogic : Node
{
    public const double MOVE_INTERVAL = 0.13;
    private static readonly Dictionary<Element, ElementNode> nodeMap = new();

    public static async Task UpdateGameMap(GameMap map)
    {
        RefreshRender(map);

        Game.Instance.SetProcess(false);
        await Game.Instance.Wait(MOVE_INTERVAL);
        Game.Instance.SetProcess(true);
    }

    public static void RefreshRender(GameMap gameMap)
    {
        var game = Game.Instance;

        Node world = game.GetNode("World");
        nodeMap.Clear();
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
        ElementNode node = GetElementNode(e);
        node.Position = Game.CalcNodePosition(gameMap, e.Position);
        node.Scale = Res.Scale_Normal * Mathf.Pow(Res.Scale_Swallow_f, layer);
        node.ZIndex = Res.Z_Swallow + layer;
        Game.Instance.AddElementNode(node);


        if (e.swallow != null) CreateElementNodeRe(gameMap, e.swallow, layer + 1);
    }

    private static ElementNode GetElementNode(Element e)
    {
        if (nodeMap.TryGetValue(e, out var node)) return node;
        node = ElementNode.CreateElementNode(e);
        nodeMap[e] = node;
        return node;
    }

    public static void DoMove(List<Step> steps)
    {
        RefreshRender(Game.gameMap);
    }
}
