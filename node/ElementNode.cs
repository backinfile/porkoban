using Godot;
using Godot.Collections;
using System;
using System.Reflection.Metadata.Ecma335;

public partial class ElementNode : Node2D
{
    public const int Element_Size = 64;
    public const int Element_Border_Size = 5;
    public const int Element_Gap = 0;
    public static PackedScene element_object = GD.Load<PackedScene>("res://node/ElementNode.tscn");


    public static float ui_offset_x = 0.0f;
    public static float ui_offset_y = 0.0f;
    private static readonly Dictionary<Type, ImageTexture> box_texture_cache = new();
    private static readonly Dictionary<Type, ImageTexture> side_texture_cache = new();

    private Element element;

    public static ElementNode CreateElementNode(Element element, float x = 0f, float y = 0f, bool assign = true)
    {
        GD.Print($"create elementNode {element.type} {x} {y}");
        ElementNode node = element_object.Instantiate<ElementNode>();
        node.element = element;
        node.Position = node.Position with { X = x, Y = y };
        node.GetNode<Sprite2D>("Element").Texture = Res.GetImageTexture(element.type);
        if (assign) element.node = node;

        foreach (var dir in Enum.GetValues<DIR>())
        {
            Sprite2D sprite2D = node.GetNode<Sprite2D>(dir.ToString());
            sprite2D.Texture = Res.GetImageTexture(dir);
            if (element.GetGate(dir) != ' ')
            {
                sprite2D.Modulate = GetGateColor(element.GetGate(dir));
            }
            else
            {
                sprite2D.Hide();
            }
        }


        return node;
    }
    public static ElementNode CreateElementNode(Element element, Vector2 pos)
    {
        return CreateElementNode(element, pos.X, pos.Y);
    }

    public ElementNode MakeCopy()
    {
        return CreateElementNode(element, Position.X, Position.Y, false);
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
