using Godot;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public partial class Game : Node
{
    public static Game Instance { get; private set; }
    public static GameMap gameMap { get; internal set; }
    private static Vector2 viewport_size;


    public override async void _Ready()
    {
        Instance = this;
        viewport_size = GetViewport().GetVisibleRect().Size with { };
        Res.Init();

        //Element element = new Element("B    ");
        //GetNode("World").AddChild(ElementNode.CreateElementNode(element, 100));
        await Restart();
    }

    public async Task Restart()
    {
        if (GameLogic.IsMoving)
        {
            GameLogic.IsMoving = false;
            await Wait(RenderLogic.MOVE_INTERVAL * 2);
        }
        else if (gameMap != null)
        {
            GameLogic.history.Push(gameMap.MakeCopy());
        }

        GameLogic.Clear();
        Game.gameMap = null;


        gameMap = GameMap.ParseFile(Path.GetDirectoryName(OS.GetExecutablePath()) + "/level1.json");
        if (gameMap == null)
        {
            gameMap = GameMap.ParseFile("res://mapResource/level1.json");
        }
        GD.Print(gameMap.boxData.Count);
        RenderLogic.RefreshRender(gameMap);
    }

    public async Task PlayBack()
    {
        if (GameLogic.IsMoving)
        {
            GameLogic.IsMoving = false;
            await Game.Instance.Wait(RenderLogic.MOVE_INTERVAL * 2);
        }

        if (GameLogic.history.Count > 0)
        {
            GD.Print("take out history");
            GameMap gameMap = GameLogic.history.Pop();
            Game.gameMap = gameMap;
            RenderLogic.RefreshRender(gameMap);
        }

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override async void _Process(double delta)
    {
        int dx = 0;
        int dy = 0;

        if (Input.IsActionJustPressed("restart"))
        {
            await Restart();
        }
        else if (Input.IsActionJustPressed("back"))
        {
            await PlayBack();
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
            if (!GameLogic.IsMoving)
            {
                await GameLogic.Move(dx, dy);
            }
        }
    }

    public async Task Wait(double time)
    {
        await ToSignal(GetTree().CreateTimer(time), "timeout");
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
