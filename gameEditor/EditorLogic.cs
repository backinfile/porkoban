using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class EditorLogic : Node
{
    public static bool InEditorMode = false;

    [Export]
    public Texture2D[] textures;

    

    public void _on_reset_level_button_pressed()
    {
        GD.Print("reset room");

        int width = (int)GetNode<SpinBox>("%WidthSpinBox").Value;
        int height = (int)GetNode<SpinBox>("%HeightSpinBox").Value;

        var gridEditor = GetNode<GridEditor>("%GridEditor");
        foreach(var child in gridEditor.GetChildren())
        {
            gridEditor.RemoveChild(child);
            child.QueueFree();
        }
        gridEditor.Columns = width;
        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                TextureRect node = new TextureRect();
                node.Texture = textures[0];
                gridEditor.AddChild(node);
            }
        }
    }
}
