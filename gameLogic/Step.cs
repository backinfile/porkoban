using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Step : RefCounted
{
    public readonly StepType StepType;
    public Element Element { get; private set; }
    public Vector2I From { get; private set; }
    public Vector2I To { get; private set; }
    public DIR DIR { get; private set; } = DIR.LEFT; // move direction
    public Element Gate { get; private set; } = null;
    public Element GateSecond { get; private set; } = null;

    private Step(StepType stepType)
    {
        this.StepType = stepType;
    }

    public static Step CreateNormal(Element element, DIR dir)
    {
        return new Step(StepType.Normal)
        {
            Element = element,
            From = element.Position,
            To = element.Position.NextPos(dir),
            DIR = dir,
        };
    }

    public static Step CreateEnter(Element element, DIR dir, Element gate, bool move = true)
    {
        return new Step(StepType.Enter)
        {
            Element = element,
            From = element.Position,
            To = move ? element.Position.NextPos(dir) : element.Position,
            DIR = dir,
            Gate = gate,
        };
    }
    public static Step CreateLeave(Element element, DIR dir, Element gate, bool move = true)
    {
        return new Step(StepType.Leave)
        {
            Element = element,
            From = element.Position,
            To = move ? element.Position.NextPos(dir) : element.Position,
            DIR = dir,
            Gate = gate,
        };
    }
    public static Step CreateLeaveAndEnter(Element element, DIR dir, Element gate, Element secondGate)
    {
        return new Step(StepType.LeaveAndEnter)
        {
            Element = element,
            From = element.Position,
            To = element.Position.NextPos(dir),
            DIR = dir,
            Gate = gate,
            GateSecond = secondGate,
        };
    }

    public override string ToString()
    {
        return $"[{StepType} {Element}: {From}=>{To}]";
    }
}

public enum StepType
{
    Normal,
    Enter,
    Leave,
    LeaveAndEnter,
}

public class StepGroup
{
    public readonly List<List<Step>> steps = new List<List<Step>>(); // divided by animation
    public StepGroup()
    {
    }

    public StepGroup(List<List<Step>> steps)
    {
        this.steps = steps;
    }
}

public class MoveResult
{
    public bool moveSuccess = false;
    public StepGroup steps = new();
    public List<Step> stepsShake = new List<Step>();

    public MoveResult(bool moveSuccess)
    {
        this.moveSuccess = moveSuccess;
    }

    public MoveResult(bool moveSuccess, List<List<Step>> steps, List<Step> stepsShake)
    {
        this.moveSuccess = moveSuccess;
        if (steps != null) this.steps.steps.AddRange(steps);
        if (stepsShake != null) this.stepsShake.AddRange(stepsShake);
    }

    public override string ToString()
    {
        var stepsStr = string.Join(", ", steps.steps.Select(s => string.Join(",", s)).ToArray());
        return (moveSuccess ? "success" : "failed") + $"steps:{stepsStr}, shakes:{stepsShake.Count}";
    }
}
