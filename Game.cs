using Godot;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public partial class Game : Node
{
    public static Game Instance { get; private set; }

    public override void _Ready()
    {
        GD.Print("WorkPath: " + OS.GetExecutablePath());
        Instance = this;
        Res.Init();
        GetTree().Root.SizeChanged += RenderLogic.OnSizeChanged;
        GetNode<Button>("%ExitButton").Pressed += () => { GetTree().Quit(); };
        GetNode<Button>("%ToggleFullScreen").Pressed += () =>
        {
            if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen)
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            else
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        };
        GetNode<Button>("%CreateNewLevelButton").Pressed += async () => { await EditorLogic.CreateSelfDefineLevel(); };
        UpdateLevels();

        EditorLogic.InitEditorPanel();
        //GD.Print($"RenderingServer.CanvasItemZMax = {RenderingServer.CanvasItemZMax} min= {RenderingServer.CanvasItemZMin}");
    }

    public override async void _Process(double delta)
    {
        EditorLogic.Update();
        await GameLogic.Update();
    }

    private async Task WaitPrivate(double time)
    {
        await ToSignal(GetTree().CreateTimer(time), "timeout");
    }

    public static async Task Wait(double time)
    {
        await Instance.WaitPrivate(time);
    }


    public void SetCameraView(Vector2 offset, Vector2 size)
    {
        var expandSize = new Vector2(size.X / 2f, size.Y / 4f);
        offset += expandSize;
        size += expandSize;

        Camera2D camera2D = GetNode<Camera2D>("Camera2D");
        Vector2 cameraSize = camera2D.GetViewportRect().Size;
        float scale = Math.Max(size.X / cameraSize.X, size.Y / cameraSize.Y);
        float zoom = 1 / scale;
        camera2D.Position = -(cameraSize * scale - size) / 2f - offset / 2f;
        camera2D.Zoom = new Vector2(zoom, zoom);
        GD.Print($"SetCameraView scale={scale}, c:{cameraSize} b:{size} cP:{camera2D.Position}");

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

    public void UpdateLevels()
    {
        { // init levels
            var buildInNode = GetNode<VBoxContainer>("%BuildInLevels");
            buildInNode.ClearChildren();
            foreach (var fileName in Utils.ListFiles("res://mapResource"))
            {
                var btn = new Button();
                btn.Text = fileName;
                btn.Pressed += async () => { await GameLogic.OpenLevel(fileName, true); };
                buildInNode.AddChild(btn);
            }
        }
        {
            var selfDefineNode = GetNode<VBoxContainer>("%SelfDefineLevels");
            selfDefineNode.ClearChildren();
            foreach (var fileName in Utils.ListFiles(GetSelfDefineLevelPath()))
            {
                var btn = new Button();
                btn.Text = fileName;
                btn.Pressed += async () => { await GameLogic.OpenLevel(fileName, false); };
                selfDefineNode.AddChild(btn);
            }
        }
    }
    public static string GetSelfDefineLevelPath()
    {
        return Path.GetDirectoryName(OS.GetExecutablePath()) + "/levels";
    }
}
