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
    public Texture2D GateTexture;


    public static ElementNode CreateElementNode(Element element, float x = 0f, float y = 0f)
    {
        //GD.Print($"create elementNode {element.Type} {x} {y}");
        ElementNode node = element_object.Instantiate<ElementNode>();
        node.element = element;
        node.Position = node.Position with { X = x, Y = y };
        Sprite2D mainSprite = node.GetNode<Sprite2D>("Element");
        mainSprite.Texture = element.Type switch
        {
            Type.Player => node.PlayerTexture,
            Type.Box => node.BoxTexture,
            Type.Wall => node.WallTexture,
            Type.Target => node.TargetTexture,
            _ => Res.GetImageTexture(element.Type)
        };

        foreach (var dir in Enum.GetValues<DIR>())
        {
            Sprite2D sprite2D = node.GetNode<Sprite2D>(dir.ToString());
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
