using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Step : RefCounted
{
    public readonly Element e;
    public readonly Vector2I from;
    public readonly Vector2I to;
    public bool intoGate = false;
    public bool outGate = false;
    public Element gateElement = null;
    public DIR gateDIR = DIR.LEFT;

    public Step(Element e, Vector2I from, Vector2I to, bool intoGate = false, bool outGate = false, Element gateElement = null, DIR gateDIR = default)
    {
        this.e = e;
        this.from = from;
        this.to = to;
        this.intoGate = intoGate;
        this.outGate = outGate;
        this.gateElement = gateElement;
        this.gateDIR = gateDIR;
    }

    override public string ToString()
    {
        string inOut = (intoGate ? "IN" : "") + (outGate ? "OUT" : "");
        if (intoGate || outGate)
        {
            inOut += " " + gateElement.GetGate(gateDIR);
        }
        return $"[{e.type}:{from}->{to} {inOut}]";
    }

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
    public StepGroup steps = new ();
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
