using Godot;
using System;
using System.Threading.Tasks;

public partial class Game : Node
{
    public static Game Instance { get; private set; }
    public static GameMap gameMap { get; private set; }
    private static Vector2 viewport_size;


    public override void _Ready()
    {
        Instance = this;
        viewport_size = GetViewport().GetVisibleRect().Size with { };
        Res.Init();

        //Element element = new Element("B    ");
        //GetNode("World").AddChild(ElementNode.CreateElementNode(element, 100));
        Restart();
    }

    public void Restart()
    {
        GameLogic.Clear();
        Game.gameMap = null;

        GameMap gameMap = GameMap.ParseFile("res://mapResource/level1.json");
        GD.Print(gameMap.boxData);
        LoadGameMap(gameMap);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override async void _Process(double delta)
    {
        int dx = 0;
        int dy = 0;

        if (Input.IsActionJustPressed("restart"))
        {
            Restart();
        }
        else if (Input.IsActionJustPressed("back"))
        {
            GameLogic.PlayBack();
        }
        else if (Input.IsActionJustPressed("move_left"))
        {
            dx -= 1;
        }
        else if (Input.IsActionJustPressed("move_right"))
        {
            dx += 1;
        }
        else if (Input.IsActionJustPressed("move_up"))
        {
            dy -= 1;
        }
        else if (Input.IsActionJustPressed("move_down"))
        {
            dy += 1;
        }

        if (dx != 0 || dy != 0)
        {
            await GameLogic.Move(dx, dy);
        }
    }

    public async Task Wait(double time)
    {
        await ToSignal(GetTree().CreateTimer(time), "timeout");
    }
   

    public void LoadGameMap(GameMap gameMap)
    {
        Game.gameMap = gameMap;
        Node world = GetNode("World");
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

        foreach (var pair in gameMap.floorData)
        {
            ElementNode node = ElementNode.CreateElementNode(pair.Value, CalcNodePosition(gameMap, gameMap.GetElementPos(pair.Value)));
            AddElementNode(node);
        }
        foreach (var pair in gameMap.boxData)
        {
            ElementNode node = ElementNode.CreateElementNode(pair.Value, CalcNodePosition(gameMap, gameMap.GetElementPos(pair.Value)));
            AddElementNode(node);
        }
    }

    public void AddElementNode(Node node)
    {
        GetNode("World").AddChild(node);
    }
    public void RemoveElementNode(Node node)
    {
        GetNode("World").RemoveChild(node);
        node.QueueFree();
    }

    public static Vector2 CalcNodePosition(GameMap gameMap, int x = 0, int y = 0)
    {
        var startX = viewport_size.X / 2f - gameMap.width / 2f * (ElementNode.Element_Size + ElementNode.Element_Gap);
        var startY = viewport_size.Y / 2f - gameMap.height / 2f * (ElementNode.Element_Size + ElementNode.Element_Gap);
        var fx = startX + x * (ElementNode.Element_Size + ElementNode.Element_Gap);
        var fy = startY + y * (ElementNode.Element_Size + ElementNode.Element_Gap);
        return new Vector2(fx, fy);
    }
    public static Vector2 CalcNodePosition(GameMap gameMap, Vector2I pos)
    {
        return CalcNodePosition(gameMap, pos.X, pos.Y);
    }
}
