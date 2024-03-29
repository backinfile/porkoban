using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

public partial class GameLogic : RefCounted
{
    public static readonly Stack<StepGroup> history = new();

    private static readonly Queue<KeyValuePair<Element, DIR>> checkGateFirstQueue = new();
    private static readonly List<Element> checkGateOrderStack = new();
    public static void Clear()
    {
    }

    public static async Task Move(int dx, int dy)
    {
        var gameMap = Game.gameMap;
        GD.Print($"Move {dx}, {dy}");

        { // move player
            var dir = Utils.GetDirection(dx, dy);
            foreach (var player in gameMap.GetPlayers())
            {
                if (PushLogic.GetStepsByPlayerMove(gameMap, player, dir, out var steps))
                {
                    GD.Print($"GetStepsByPlayerMove {string.Join(",", steps)}");
                    await DoMove(gameMap, steps);
                }
                else
                {
                    GD.Print($"GetStepsByPlayerMove failed");
                }
            }
        }

        GD.Print($"checkGateFirstQueue = {string.Join("", checkGateFirstQueue.Select(e=>e.Key.ToFullString()))}");
        while (true)
        {
            bool moved = false;

            while (!moved && checkGateFirstQueue.Count > 0)
            {
                var pair = checkGateFirstQueue.Dequeue();
                var e = pair.Key;
                var dir = pair.Value;
                GD.Print($"check {e.ToFullString()} {dir}");
                if (PushLogic.GetStepsByLeaveGate(gameMap, e, dir, out var steps))
                {
                    moved = true;
                    await DoMove(gameMap, steps);
                }
            }

            foreach (var e in checkGateOrderStack.Concat(gameMap.boxData.Keys))
            {
                if (moved) break; // check for next gate
                if (e.swallow == null) continue;
                GD.Print($"check {e.ToFullString()}");
                foreach (var dir in Enum.GetValues<DIR>())
                {
                    if (PushLogic.GetStepsByLeaveGate(gameMap, e, dir, out var steps))
                    {
                        moved = true;
                        await DoMove(gameMap, steps);
                    }
                }
            }
            if (!moved) break; // move finish
        }
        checkGateFirstQueue.Clear();
    }

    public static async Task DoMove(GameMap gameMap, List<Step> steps)
    {
        ApplyStep(gameMap, steps);
        await RenderLogic.UpdateGameMap(gameMap);
    }

    private static List<Element> ApplyStep(GameMap gameMap, List<Step> steps)
    {
        List<Element> removed = new List<Element>();
        foreach (var step in steps)
        {
            SetPositionRe(step.Element, step.To);
            switch (step.StepType)
            {
                case StepType.Normal:
                    break;
                case StepType.Enter:
                    {
                        gameMap.RemoveElement(step.Element);
                        char gateChar = step.Gate.GetGate(step.DIR.Opposite());
                        GD.Print($"enter char:{gateChar} {step.Gate.ToFullString()} {step.DIR}");
                        step.Gate.swallow = step.Element;
                        step.Gate.swallowGate = gateChar;
                        foreach (var e in gameMap.FindGateElements(gateChar, step.Gate))
                        {
                            e.swallow = step.Element.MakeCopy();
                            e.swallow.Position = e.Position;
                            e.swallowGate = gateChar;
                            foreach (var dir in e.GetDIRByGate(gateChar)) checkGateFirstQueue.Enqueue(new KeyValuePair<Element, DIR>(e, dir));
                            if (!checkGateOrderStack.Contains(e)) checkGateOrderStack.Add(e);
                        }
                        if (!checkGateOrderStack.Contains(step.Gate)) checkGateOrderStack.Add(step.Gate);
                        break;
                    }
                case StepType.Leave:
                    {
                        gameMap.AddElement(step.Element);
                        step.Gate.swallow = null;
                        step.Gate.swallowGate = ' ';
                        char gateChar = step.Gate.GetGate(step.DIR);
                        foreach (var e in gameMap.FindGateElements(gateChar, step.Gate))
                        {
                            if (e.swallow != null) removed.Add(e.swallow);
                            e.swallow = null;
                            e.swallowGate = ' ';
                            checkGateOrderStack.Remove(e);
                        }
                        checkGateOrderStack.Remove(step.Gate);
                        break;
                    }
                case StepType.LeaveAndEnter:
                    {
                        gameMap.AddElement(step.Element);
                        char gateChar = step.Gate.GetGate(step.DIR);
                        step.Gate.swallow = null;
                        step.Gate.swallowGate = ' ';
                        foreach (var e in gameMap.FindGateElements(gateChar, step.Gate))
                        {
                            if (e.swallow != null) removed.Add(e.swallow);
                            e.swallow = null;
                            e.swallowGate = ' ';
                            checkGateOrderStack.Remove(e);
                        }
                        checkGateOrderStack.Remove(step.Gate);

                        char secondGateChar = step.GateSecond.GetGate(step.DIR.Opposite());
                        step.GateSecond.swallow = step.Element;
                        step.GateSecond.swallowGate = secondGateChar;
                        foreach (var e in gameMap.FindGateElements(secondGateChar, step.GateSecond))
                        {
                            e.swallow = step.Element.MakeCopy();
                            e.swallow.Position = e.Position;
                            e.swallowGate = secondGateChar;
                            foreach (var dir in e.GetDIRByGate(gateChar)) checkGateFirstQueue.Enqueue(new KeyValuePair<Element, DIR>(e, dir));
                            if (!checkGateOrderStack.Contains(e)) checkGateOrderStack.Add(e);
                        }
                        if (!checkGateOrderStack.Contains(step.GateSecond)) checkGateOrderStack.Add(step.GateSecond);
                        break;
                    }
            }
        }
        return removed;
    }

    private static void SetPositionRe(Element element, Vector2I pos)
    {
        if (element == null) return;
        element.Position = pos;
        SetPositionRe(element.swallow, pos);
    }

    public static void PlayBack()
    {
    }
}




