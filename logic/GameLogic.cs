using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

public partial class GameLogic : RefCounted
{
    public static bool IsMoving { get; set; } = false;
    public static GameMap gameMap { get; private set; }


    public static readonly Stack<GameMap> history = new();
    private static bool hasMoveStepThisMove = false;

    private static readonly Queue<KeyValuePair<Element, DIR>> checkGateFirstQueue = new();
    private static int enterGateIndex = 0;

    public static void Clear()
    {
        checkGateFirstQueue.Clear();
        enterGateIndex = 0;
    }

    public static async Task Move(int dx, int dy)
    {
        IsMoving = true;
        var gameMap = GameLogic.gameMap;
        GD.Print($"Move {dx}, {dy}");
        hasMoveStepThisMove = false;

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

        //GD.Print($"checkGateFirstQueue = {string.Join("", checkGateFirstQueue.Select(e => e.Key.ToFullString()))}");
        while (true && IsMoving)
        {
            bool moved = false;

            while (!moved && checkGateFirstQueue.Count > 0)
            {
                var pair = checkGateFirstQueue.Dequeue();
                var e = pair.Key;
                var dir = pair.Value;
                //GD.Print($"check {e.ToFullString()} {dir}");
                if (PushLogic.GetStepsByLeaveGate(gameMap, e, dir, out var steps))
                {
                    GD.Print($"GetStepsByLeaveGate1 {string.Join(",", steps)}");
                    moved = true;
                    await DoMove(gameMap, steps);
                }
            }

            List<Element> gateCheckList = gameMap.boxData.Keys.Where(e => e.swallow != null).OrderByDescending(e => e.enterGateIndex).ToList();
            foreach (var e in gateCheckList)
            {
                if (moved) break; // check for next gate
                if (e.swallow == null) continue;
                //GD.Print($"check {e.ToFullString()}");
                foreach (var dir in Enum.GetValues<DIR>())
                {
                    if (PushLogic.GetStepsByLeaveGate(gameMap, e, dir, out var steps))
                    {
                        GD.Print($"GetStepsByLeaveGate3 {string.Join(",", steps)}");
                        moved = true;
                        await DoMove(gameMap, steps);
                    }
                }
            }
            if (!moved) break; // move finish
            GD.Print("Leave gate move");
        }
        checkGateFirstQueue.Clear();
        IsMoving = false;
    }

    public static async Task DoMove(GameMap gameMap, List<Step> steps)
    {
        if (!hasMoveStepThisMove)
        {
            hasMoveStepThisMove = true;
            history.Push(gameMap.MakeCopy());
            GD.Print("save history");
        }
        List<Element> removed = ApplyStep(gameMap, steps);
        await RenderLogic.UpdateGameMap(gameMap, removed);
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
                            e.swallow.Rotate(step.DIR, e.GetDIRByGate(gateChar)[0]);
                            e.swallow.Position = e.Position;
                            e.swallowGate = gateChar;
                            foreach (var dir in e.GetDIRByGate(gateChar)) checkGateFirstQueue.Enqueue(new KeyValuePair<Element, DIR>(e, dir));
                            e.enterGateIndex = enterGateIndex++;
                        }
                        foreach (var dir in step.Gate.GetDIRByGate(gateChar, step.DIR.Opposite())) checkGateFirstQueue.Enqueue(new KeyValuePair<Element, DIR>(step.Gate, dir));
                        step.Gate.enterGateIndex = enterGateIndex++;
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
                        }
                        break;
                    }
                case StepType.LeaveAndEnter:
                    {

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
                            }
                        }

                        {
                            gameMap.RemoveElement(step.Element);
                            char secondGateChar = step.GateSecond.GetGate(step.DIR.Opposite());
                            step.GateSecond.swallow = step.Element;
                            step.GateSecond.swallowGate = secondGateChar;
                            foreach (var e in gameMap.FindGateElements(secondGateChar, step.GateSecond))
                            {
                                e.swallow = step.Element.MakeCopy();
                                e.swallow.Rotate(step.DIR, e.GetDIRByGate(secondGateChar)[0]);
                                e.swallow.Position = e.Position;
                                e.swallowGate = secondGateChar;
                                foreach (var dir in e.GetDIRByGate(secondGateChar)) checkGateFirstQueue.Enqueue(new KeyValuePair<Element, DIR>(e, dir));
                                e.enterGateIndex = enterGateIndex++;
                            }
                            foreach (var dir in step.GateSecond.GetDIRByGate(secondGateChar, step.DIR.Opposite())) checkGateFirstQueue.Enqueue(new KeyValuePair<Element, DIR>(step.GateSecond, dir));
                            step.GateSecond.enterGateIndex = enterGateIndex++;
                        }
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

    public static async Task Update()
    {
        int dx = 0;
        int dy = 0;

        if (Input.IsActionJustPressed("restart"))
        {
            await Restart();
        }
        else if (Input.IsActionJustPressed("back"))
        {
            await PlayBack();
        }
        else if (Input.IsActionJustPressed("move_left"))
        {
            dx -= 1;
        }
        else if (Input.IsActionJustPressed("move_right"))
        {
            dx += 1;
        }
        else if (Input.IsActionJustPressed("move_up"))
        {
            dy -= 1;
        }
        else if (Input.IsActionJustPressed("move_down"))
        {
            dy += 1;
        }

        if (dx != 0 || dy != 0)
        {
            if (!GameLogic.IsMoving)
            {
                await GameLogic.Move(dx, dy);
            }
        }
    }

    public static async Task Restart()
    {
        if (GameLogic.IsMoving)
        {
            GameLogic.IsMoving = false;
            await Game.Wait(RenderLogic.MOVE_INTERVAL * 2);
        }
        else if (gameMap != null)
        {
            GameLogic.history.Push(gameMap.MakeCopy());
        }

        GameLogic.Clear();
        GameLogic.gameMap = null;


        gameMap = GameMap.ParseFile(Path.GetDirectoryName(OS.GetExecutablePath()) + "/level1.json");
        gameMap ??= GameMap.ParseFile("res://mapResource/level1.json");
        GD.Print(gameMap.boxData.Count);
        RenderLogic.RefreshRender(gameMap);
    }

    public static async Task PlayBack()
    {
        if (GameLogic.IsMoving)
        {
            GameLogic.IsMoving = false;
            await Game.Wait(RenderLogic.MOVE_INTERVAL * 2);
        }

        if (GameLogic.history.Count > 0)
        {
            GD.Print("take out history");
            GameMap gameMap = GameLogic.history.Pop();
            GameLogic.gameMap = gameMap;
            RenderLogic.RefreshRender(gameMap);
        }

    }


}




