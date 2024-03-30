using Godot;
using Godot.Collections;
using System;
using System.Reflection.Metadata.Ecma335;

public partial class ElementNode : Node2D
{
    public const int Element_Size = 64;
    public const int Element_Border_Size = 5;
    public const int Element_Gap = 0;
    public const int Element_Unit_Size = Element_Size + Element_Gap;
    public static PackedScene element_object = GD.Load<PackedScene>("res://node/ElementNode.tscn");

    private Element element;

    [Export]
    public Texture2D PlayerTexture;
    [Export]
    public Texture2D BoxTexture;
    [Export]
    public Texture2D WallTexture;
    [Export]
    public Texture2D TargetTexture;
    [Export]
    public Texture2D EmptyTexture;
    [Export]
    public Texture2D GateTexture;


    public static ElementNode CreateElementNode(Element element, float x = 0f, float y = 0f)
    {
        GD.Print($"create elementNode {element.Type} {element.Position}");
        ElementNode node = element_object.Instantiate<ElementNode>();
        node.element = element;
        node.Position = node.Position with { X = x, Y = y };
        TextureRect mainSprite = node.GetNode<TextureRect>("Element");
        mainSprite.Texture = element.Type switch
        {
            Type.Player => node.PlayerTexture,
            Type.Box => node.BoxTexture,
            Type.Wall => node.WallTexture,
            Type.Target => node.TargetTexture,
            _ => node.EmptyTexture,
        };
        mainSprite.GuiInput += (ev) =>
        {
            if (ev is InputEventMouseButton e && e.Pressed)
            {
                if (e.ButtonIndex == MouseButton.Left)
                {
                    EditorLogic.OnElementClick(element);
                } else if (e.ButtonIndex == MouseButton.Middle)
                {
                    //EditorLogic.OnMouseScroll(e.d)
                }
            }
        };
        mainSprite.MouseEntered += () =>
        {
            if (EditorLogic.InEditorMode) node.GetNode<TextureRect>("Select").Visible = true;
        }; 
        mainSprite.MouseExited += () =>
        {
            if (EditorLogic.InEditorMode) node.GetNode<TextureRect>("Select").Visible = false;
        };

        foreach (var dir in Enum.GetValues<DIR>())
        {
            TextureRect sprite2D = node.GetNode<TextureRect>(dir.ToString());
            sprite2D.Texture = node.GateTexture;
            sprite2D.RotationDegrees = dir.ToRotation() - 90;
            if (element.GetGate(dir) != ' ')
            {
                sprite2D.Modulate = GetGateColor(element.GetGate(dir));
            }
            else
            {
                sprite2D.QueueFree();
            }
        }

        if (element.Type == Type.Target)
        {
            node.ZIndex = Res.Z_Target;
        }
        else
        {
            node.ZIndex = Res.Z_Ground;
        }



        return node;
    }


    public static ElementNode CreateElementNode(Element element, Vector2 pos)
    {
        return CreateElementNode(element, pos.X, pos.Y);
    }

    public ElementNode MakeCopy()
    {
        return CreateElementNode(element, Position.X, Position.Y);
    }

    public override void _Ready()
    {
        //GetNode<Area2D>("Area2D").InputEvent += (v, ev, i) =>
        //{
        //    GD.Print("detact on " + element.ToFullString());
        //    if (ev is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left && e.Pressed)
        //    {
        //        GD.Print("clicked on " + element.ToFullString());
        //    }
        //};
    }

    public override void _Process(double delta)
    {
    }

    public static Color GetGateColor(char ch)
    {
        switch (ch)
        {
            case 'b': return Colors.MediumBlue;
            case 'r': return Colors.DarkRed;
            case 'g': return Colors.LightGreen;
        }
        Random random = new(ch);
        return new Color((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
    }
}
