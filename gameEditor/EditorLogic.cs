using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public partial class EditorLogic : Node
{
    public static bool InEditorMode = false;

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
        RenderLogic.RefreshRender(gameMap);
    }

    private static Type GetCurSelectType()
    {
        int selected = Game.Instance.GetNode<OptionButton>("%OptionButton").Selected;
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


    public static void OnElementClick(Element element)
    {
        if (!InEditorMode) return;

        var gameMap = GameLogic.gameMap;
        var pos = element.Position;

        Type type = GetCurSelectType();
        GD.Print($"click element {element.ToFullString()} type={type}");

        if (type == Type.Target)
        {
            Element floor = gameMap.GetFloorElement(pos);
            if (floor != null)
            {
                gameMap.RemoveElement(floor);
                RenderLogic.Remove(gameMap, floor);
            } else
            {
                floor = Element.Create((char)type + "    ", pos.X, pos.Y);
                gameMap.AddFloorElement(floor);
                RenderLogic.CreateElementNodeRe(gameMap, floor);
            }
        } else
        {
            gameMap.RemoveElement(element);
            element = Element.Create((char)type + "    ", pos.X, pos.Y);
            gameMap.AddElement(element);
            RenderLogic.CreateElementNodeRe(gameMap, element);
        }
    }
}
