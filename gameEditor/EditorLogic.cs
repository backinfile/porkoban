using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public partial class EditorLogic : Node
{
    public static bool InEditorMode = false;

    [Export]
    public Texture2D[] textures;

    

    public static async Task CreateSelfDefineLevel()
    {
        if (GameLogic.IsMoving)
        {
            GameLogic.IsMoving = false;
            await Game.Wait(RenderLogic.MOVE_INTERVAL * 2);
        }
        GameLogic.Clear();
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

        int width = (int)GetNode<SpinBox>("%WidthSpinBox").Value;
        int height = (int)GetNode<SpinBox>("%HeightSpinBox").Value;

        var gridEditor = GetNode<GridEditor>("%GridEditor");
        foreach (var child in gridEditor.GetChildren())
        {
            gridEditor.RemoveChild(child);
            child.QueueFree();
        }
        gridEditor.Columns = width;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                TextureRect node = new TextureRect();
                node.Texture = textures[0];
                gridEditor.AddChild(node);
            }
        }
    }
}
