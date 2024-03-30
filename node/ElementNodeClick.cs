using Godot;
using System;

public partial class ElementNodeClick : Area2D
{
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
            //GD.Print("_InputEvent" + e.GlobalPosition + "  " + e.Position);
        //if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left && e.Pressed)
        //{
        //    GD.Print("_InputEvent" + e.GlobalPosition + "  " + e.Position);
        //}
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        base._InputEvent(viewport, @event, shapeIdx);
        //if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left)
        //{
        //    GD.Print("_InputEvent");
        //}
        GD.Print("_InputEvent");
        if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left && e.Pressed)
        {
            //GD.Print("_InputEvent" + e.GlobalPosition + "  " + e.Position);
        }
    }


}
