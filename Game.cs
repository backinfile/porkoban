using Godot;
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public partial class Game : Node
{
    private double noticeHideTimer = 0;


    public static Game Instance { get; private set; }

    public override void _Ready()
    {
        GD.Print("WorkPath: " + OS.GetExecutablePath());
        Instance = this;
        Res.Init();

        {
            var retina_scale = DisplayServer.ScreenGetScale();
            GD.Print($"retina_scale = {retina_scale} window sacle factor={GetWindow().ContentScaleFactor}");
            if (GetWindow().ContentScaleFactor != retina_scale)
            {
                var relative_scale = retina_scale / GetWindow().ContentScaleFactor;
                GetWindow().ContentScaleFactor = retina_scale;
                GetWindow().Size = GetWindow().Size.Mul(relative_scale);
            }
        }



        GetTree().Root.SizeChanged += RenderLogic.OnSizeChanged;
        GetNode<Button>("%ExitButton").Pressed += () => { GetTree().Quit(); };
        GetNode<Button>("%ToggleFullScreen").Pressed += () =>
        {
            if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen)
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            else
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        };
        GetNode<Button>("%CreateLevelFromCurButton").Pressed += async () => { await EditorLogic.CreateSelfDefineLevel(true); };
        GetNode<Button>("%CreateNewLevelButton").Pressed += async () => { await EditorLogic.CreateSelfDefineLevel(false); };
        UpdateLevels();

        EditorLogic.InitEditorPanel();
        //GD.Print($"RenderingServer.CanvasItemZMax = {RenderingServer.CanvasItemZMax} min= {RenderingServer.CanvasItemZMin}");
    }

    public override async void _Process(double delta)
    {
        if (noticeHideTimer > 0)
        {
            noticeHideTimer -= delta;
            if (noticeHideTimer < 0) HideNotice();
        }

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
        var expandSize = new Vector2(size.X * 0.5f + ElementNode.Element_Size, size.Y * 0.25f + +ElementNode.Element_Size);
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

            var refreshFolderBtn = new Button();
            refreshFolderBtn.Text = "[refresh levels]";
            refreshFolderBtn.Pressed += () => { UpdateLevels(); };
            selfDefineNode.AddChild(refreshFolderBtn);

            var openFolderBtn = new Button();
            openFolderBtn.Text = "[open system folder]";
            openFolderBtn.Pressed += () =>
            {
                string path = ProjectSettings.GlobalizePath(GetSelfDefineLevelPath());
                DirAccess.MakeDirRecursiveAbsolute(path);
                OS.ShellOpen(path);
            };
            selfDefineNode.AddChild(openFolderBtn);

            string lastFileName = null;
            foreach (var fileName in Utils.ListFiles(GetSelfDefineLevelPath()))
            {
                string trueLastFileName = lastFileName;
                var hBox = new HBoxContainer();
                var fileBtn = new Button();
                fileBtn.Text = fileName;
                fileBtn.Pressed += async () => { await GameLogic.OpenLevel(fileName, false); };
                fileBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                hBox.AddChild(fileBtn);
                var upBtn = new Button();
                upBtn.Text = "UP";
                upBtn.Pressed += () =>
                {
                    GD.Print("trueLastFileName = " + trueLastFileName);
                    if (trueLastFileName != null)
                    {
                        Utils.ExchangeFileName(GetSelfDefineLevelPath(), fileName + ".json", trueLastFileName + ".json");
                        UpdateLevels();
                        SendNotice("exchange level name done!");
                    }
                };
                hBox.AddChild(upBtn);
                selfDefineNode.AddChild(hBox);
                lastFileName = fileName;
            }
        }
    }

    public void SendNotice(string notice)
    {
        Rect2 rect2 = GetViewport().GetVisibleRect();
        PopupPanel popupPanel = GetNode<PopupPanel>("%PopupPanel");
        popupPanel.GetChild<Label>(0).Text = notice;
        popupPanel.Popup();
        popupPanel.Position = new Vector2I((int)((rect2.Size.X - popupPanel.Size.X) / 2f), 0);
        noticeHideTimer = 1f;
    }

    public void HideNotice()
    {
        PopupPanel popupPanel = GetNode<PopupPanel>("%PopupPanel");
        popupPanel.Hide();
    }

    public static string GetSelfDefineLevelPath()
    {
        return "user://levels";
    }
}
