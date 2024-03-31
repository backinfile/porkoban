using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

public partial class EditorLogic : Node
{
    public static bool InEditorMode = false;
    public static bool DrawBoxMod = true;

    [Export]
    public Texture2D[] textures;

    private static Vector2I EditorMapSize
    {
        get
        {
            SpinBox wNode = Game.Instance.GetNode<SpinBox>("%MapWidth");
            SpinBox hNode = Game.Instance.GetNode<SpinBox>("%MapHeight");
            return new Vector2I((int)wNode.Value, (int)hNode.Value);
        }
        set
        {
            SpinBox wNode = Game.Instance.GetNode<SpinBox>("%MapWidth");
            SpinBox hNode = Game.Instance.GetNode<SpinBox>("%MapHeight");
            wNode.Value = value.X; hNode.Value = value.Y;
        }
    }


    public static async Task CreateSelfDefineLevel()
    {
        if (GameLogic.IsMoving)
        {
            GameLogic.IsMoving = false;
            await Game.Wait(RenderLogic.MOVE_INTERVAL * 2);
        }
        GameLogic.Clear();
        EditorMapSize = new Vector2I(5, 5);
        GameMap gameMap = GameMap.CreateEmpty(5, 5, true);
        GameLogic.theFirstGameMap = gameMap.MakeCopy();
        GameLogic.gameMap = gameMap;
        RenderLogic.RefreshRender(gameMap);

        InEditorMode = true;
        ShowEditorPanel();
    }

    public static void ShowEditorPanel(bool show = true)
    {
        Game.Instance.GetNode<BoxContainer>("%EditorPanel").Visible = show;
        Game.Instance.GetNode<BoxContainer>("%EditorModTip").Visible = show;
    }

    public static void CloseEditor()
    {
        InEditorMode = false;
        ShowEditorPanel(false);
    }

    public void _on_reset_level_button_pressed()
    {
        GD.Print("reset room");

        var size = EditorMapSize;
        GameLogic.gameMap = GameMap.CreateEmpty(size.X, size.Y, true);
        RenderLogic.RefreshRender(GameLogic.gameMap);
    }

    public static void OnMapSizeChanged()
    {
        if (!InEditorMode) return;

        var gameMap = GameLogic.gameMap;
        var size = EditorMapSize;
        gameMap.width = size.X;
        gameMap.height = size.Y;

        foreach (var e in gameMap.boxData.Keys.ToList())
        {
            if (!gameMap.InMapArea(e.Position))
            {
                gameMap.RemoveElement(e);
            }
        }
        foreach (var e in gameMap.floorData.Keys.ToList())
        {
            if (!gameMap.InMapArea(e.Position))
            {
                gameMap.RemoveElement(e);
            }
        }
        for (int x = 0; x < gameMap.width; x++)
        {
            for (int y = 0; y < gameMap.height; y++)
            {
                if (gameMap.GetElement(x, y) == null)
                {
                    gameMap.AddElement(Element.Create("     ", x, y));
                }
            }
        }
        RenderLogic.RefreshRender(gameMap);
    }

    private static Type GetCurSelectType()
    {
        int selected = Game.Instance.GetNode<OptionButton>("%DrawBoxOptionButton").Selected;
        return selected switch
        {
            0 => Type.None,
            1 => Type.Player,
            2 => Type.Box,
            3 => Type.Wall,
            4 => Type.Target,
            _ => Type.None,
        };
    }

    private static DIR GetCurSelectDIR()
    {
        int selected = Game.Instance.GetNode<OptionButton>("%DrawGateOptionButton").Selected;
        return (DIR)selected;
    }

    private static string GetCurFileName()
    {
        LineEdit lineEdit = Game.Instance.GetNode("%EditorPanel").GetNode("Save").GetNode<LineEdit>("LineEdit");
        return lineEdit.Text;
    }

    private static void ChangeCurSelectType(int dx)
    {
        GD.Print("ChangeCurSelectType " + dx);
        if (DrawBoxMod)
        {
            var optionBtn = Game.Instance.GetNode<OptionButton>("%DrawBoxOptionButton");
            optionBtn.Select((optionBtn.Selected + dx + optionBtn.ItemCount) % optionBtn.ItemCount);
        }
        else
        {
            var optionBtn = Game.Instance.GetNode<OptionButton>("%DrawGateOptionButton");
            optionBtn.Select((optionBtn.Selected + dx + optionBtn.ItemCount) % optionBtn.ItemCount);
        }
    }

    private static char GetCurGateChar()
    {
        string text = Game.Instance.GetNode<LineEdit>("%GateCharInput").Text;
        if (text == null || text.Length == 0) return ' ';
        return text[0];
    }


    public static void OnElementClick(Element element)
    {
        if (!InEditorMode) return;

        var gameMap = GameLogic.gameMap;
        var pos = element.Position;
        element = gameMap.GetElement(pos);
        GD.Print($"click element {element.ToFullString()}");

        if (DrawBoxMod)
        {
            Type type = GetCurSelectType();

            if (type == Type.Target)
            {
                Element floor = gameMap.GetFloorElement(pos);
                if (floor != null)
                {
                    gameMap.RemoveElement(floor);
                    RenderLogic.Remove(gameMap, floor);
                }
                else
                {
                    floor = Element.Create((char)type + "    ", pos.X, pos.Y);
                    gameMap.AddFloorElement(floor);
                    RenderLogic.CreateElementNodeRe(gameMap, floor);
                }
            }
            else if (type != element.Type)
            {
                gameMap.RemoveElement(element);
                RenderLogic.Remove(gameMap, element);
                element = Element.Create((char)type + "    ", pos.X, pos.Y);
                gameMap.AddElement(element);
                RenderLogic.CreateElementNodeRe(gameMap, element);
            } else
            {
                gameMap.RemoveElement(element);
                RenderLogic.Remove(gameMap, element);
                element = Element.Create("     ", pos.X, pos.Y);
                gameMap.AddElement(element);
                RenderLogic.CreateElementNodeRe(gameMap, element);
            }
        }
        else
        {
            DIR dir = GetCurSelectDIR();
            char gateChar = GetCurGateChar();
            if (element.Type == Type.Box || element.Type == Type.Wall || element.Type == Type.Player)
            {
                if (gateChar != ' ' && gateChar != element.gate[(int)dir])
                {
                    element.gate[(int)dir] = gateChar;
                    element.side[(int)dir] = Side.Gate;
                }
                else
                {
                    element.gate[(int)dir] = ' ';
                    element.side[(int)dir] = Side.None;
                }
                RenderLogic.Remove(gameMap, element);
                gameMap.AddElement(element);
                RenderLogic.CreateElementNodeRe(gameMap, element);
            }
        }

    }

    public static void InitEditorPanel()
    {
        EditorLogic.ShowEditorPanel(false);

        Game.Instance.GetNode<SpinBox>("%MapWidth").ValueChanged += (v) => { EditorLogic.OnMapSizeChanged(); };
        Game.Instance.GetNode<SpinBox>("%MapHeight").ValueChanged += (v) => { EditorLogic.OnMapSizeChanged(); };

        {
            CheckButton drawBoxCheck = Game.Instance.GetNode<CheckButton>("%DrawBoxCheck");
            drawBoxCheck.Pressed += () => ChangeDrawBoxMod(drawBoxCheck.ButtonPressed);
            CheckButton drawGateCheck = Game.Instance.GetNode<CheckButton>("%DrawGateCheck");
            drawGateCheck.Pressed += () => ChangeDrawBoxMod(!drawGateCheck.ButtonPressed);
        }

        {
            Game.Instance.GetNode<OptionButton>("%DrawBoxOptionButton").Pressed += () => ChangeDrawBoxMod(true);
            Game.Instance.GetNode<OptionButton>("%DrawGateOptionButton").Pressed += () => ChangeDrawBoxMod(false);
        }

        {
            Game.Instance.GetNode("%EditorPanel").GetNode<Button>("SaveButton").Pressed += async () => await SaveAndPlay(false);
            Game.Instance.GetNode("%EditorPanel").GetNode<Button>("SaveAndPlayButton").Pressed += async () => await SaveAndPlay(true);
        }
    }

    private static async Task SaveAndPlay(bool play)
    {
        string fileName = GetCurFileName();
        if (fileName == null || fileName.Length == 0)
        {
            OS.Alert("no file name!");
            return;
        }
        var gameMap = GameLogic.gameMap.MakeCopy();
        // clear empty node
        foreach (var e in gameMap.boxData.Keys.Concat(gameMap.floorData.Keys).ToList())
        {
            if (e.Type == Type.None) gameMap.RemoveElement(e);
        }

        // save
        if (gameMap.Save(fileName, Game.GetSelfDefineLevelPath()))
        {
            OS.Alert($"save {fileName} success!");
        } else
        {
            OS.Alert($"save {fileName} failed!");
            return;
        }
        Game.Instance.UpdateLevels();

        // run
        if (play)
        {
            await GameLogic.OpenLevel(fileName, false);
        }
    }

    private static void ChangeDrawBoxMod(bool drawBox)
    {
        Game.Instance.GetNode<CheckButton>("%DrawBoxCheck").ButtonPressed = drawBox;
        Game.Instance.GetNode<CheckButton>("%DrawGateCheck").ButtonPressed = !drawBox;
        EditorLogic.DrawBoxMod = drawBox;
        GD.Print("cur DrawBoxMod = " + EditorLogic.DrawBoxMod);
    }

    public static void Update()
    {
        if (!InEditorMode) return;

        if (Input.IsActionJustPressed("draw_type_up"))
        {
            ChangeCurSelectType(-1);
        }
        else if (Input.IsActionJustPressed("draw_type_down"))
        {
            ChangeCurSelectType(1);
        }
    }
}
