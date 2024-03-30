using Godot;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public partial class Game : Node
{
    public static Game Instance { get; private set; }

    public override async void _Ready()
    {
        Instance = this;
        Res.Init();

        await GameLogic.Restart();
    }

    public override async void _Process(double delta)
    {
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



    public void AddElementNode(Node node)
    {
        GetNode("World").AddChild(node);
    }
    public void RemoveElementNode(Node node)
    {
        GetNode("World").RemoveChild(node);
        node.QueueFree();
    }
}
