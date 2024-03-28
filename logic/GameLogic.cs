using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public partial class GameLogic : RefCounted
{
    public const double MOVE_INTERVAL = 0.13;
    public static readonly Stack<StepGroup> history = new();


    public static void Clear()
    {
        history.Clear();
    }

    public static void PlayBack()
    {
        if (history.Count > 0)
        {
            StepGroup steps = history.Pop();
            foreach (var stepOnce in steps.steps)
            {
                Game.gameMap.ApplyStep(stepOnce, true);
                foreach (var step in stepOnce)
                {
                    var p = Game.CalcNodePosition(Game.gameMap, step.from.X, step.from.Y);
                    step.e.node.Position = p;
                }
            }
        }
    }

    public static async Task Move(int dx, int dy)
    {
        GD.Print($"start move {dx} {dy}");
        var gameMap = Game.gameMap;
        var curStepHistory = new StepGroup();

        MoveResult playerMoveResult = FindMovePath(gameMap, dx, dy);
        GD.Print($"find move path {playerMoveResult}");
        if (playerMoveResult.moveSuccess)
        {
            curStepHistory.steps.AddRange(playerMoveResult.steps.steps);
            history.Push(curStepHistory);
            foreach (var stepOnce in playerMoveResult.steps.steps)
            {
                await DoMove(stepOnce);
            }
        }

        if (!playerMoveResult.moveSuccess)
        {
            return;
        }

        // check gate in full state
        bool loop = true;
        while (loop)
        {
            loop = false;
            foreach (var pair in gameMap.gateData)
            {
                var gateChar = pair.Key;
                var e = pair.Value;
                MoveResult moveResult = CheckGateAround(gameMap, e, gateChar);
                if (moveResult.moveSuccess)
                {
                    GD.Print($"move out of gate {moveResult}");
                    curStepHistory.steps.AddRange(moveResult.steps.steps);
                    foreach (var stepOnce in moveResult.steps.steps)
                    {
                        await DoMove(stepOnce);
                    }
                    loop = true;
                    break;
                }
            }
        }
    }
    public static async Task DoMove(List<Step> steps)
    {
        GameMap gameMap = Game.gameMap;

        gameMap.ApplyStep(steps);
        foreach (var step in steps)
        {
            GD.Print($"DOMOVE step:{step}");

           

            var p = Game.CalcNodePosition(gameMap, step.to);
            Tween tween = step.e.node.CreateTween();
            if (step.outGate)
            {
                tween.TweenProperty(step.e.node, "position", Game.CalcNodePosition(gameMap, step.from), 0.01);
            }
            tween.TweenProperty(step.e.node, "position", p, MOVE_INTERVAL);
            if (step.intoGate)
            {
                tween.TweenProperty(step.e.node, "scale", new Vector2(0.8f, 0.8f), MOVE_INTERVAL);
                tween.TweenProperty(step.e.node, "z_index", 1, MOVE_INTERVAL);
            }
            if (step.outGate)
            {
                tween.TweenProperty(step.e.node, "scale", new Vector2(1f, 1f), MOVE_INTERVAL);
                //tween.TweenProperty(step.e.node, "z_index", 0, MOVE_INTERVAL);
            }
        }
        Game gameNode = gameMap.GetPlayer().node.GetNode<Game>("/root/Game");
        gameNode.SetProcess(false);
        await gameNode.Wait(MOVE_INTERVAL);
        gameNode.SetProcess(true);
    }

    public static MoveResult CheckGateAround(GameMap gameMap, Element e, char gateChar)
    {
        GD.Print($"check gate:{gateChar}");
        foreach (var gate in gameMap.boxData.Values)
        {
            foreach (var dir in Enum.GetValues<DIR>())
            {
                if (gate.GetGate(dir) == gateChar)
                {
                    Vector2I pos = gameMap.GetElementPos(gate);
                    var nextPos = pos.NextPos(dir);
                    if (!gameMap.InMapArea(nextPos) || gameMap.GetElement(nextPos) != null) continue;

                    var step = new Step(e, pos, pos.NextPos(dir), false, true, gateChar);
                    return new MoveResult(true, new List<List<Step>> { new List<Step> { step } }, null);
                }
            }
        }
        return new MoveResult(false);
    }


    public static MoveResult FindMovePath(GameMap map, int dx, int dy)
    {
        var player = map.GetPlayer();
        var playerPos = map.GetElementPos(player);
        var dir = Utils.GetDirection(dx, dy);

        // find relate box
        List<Step> boxElements = FindContinuousBox(map, playerPos, dx, dy, out var nextPos);
        if (CheckIfIntoGate(map, boxElements, dir, nextPos, out var gate, out var gateDir, out var isSwallow))
        {
            var gateChar = gate.GetGate(gateDir);
            MoveResult moveResult = new MoveResult(true);
            // enter gate
            if (isSwallow)
            {
                var swallowPos = map.GetElementPos(gate).NextPos(dir);
                var swallowEle = map.GetElement(swallowPos);
                var index = IndexOf(gate, boxElements);
                moveResult.steps.steps.Add(new List<Step>(boxElements.Take(index + 1))
                {
                    new Step(swallowEle, swallowPos, swallowPos, true, false, gateChar)
                });
            }
            else
            {
                var index = IndexOf(gate, boxElements);
                if (index < 0) // push all e into wall
                {
                    boxElements.Last().intoGate = true;
                    boxElements.Last().gate = gateChar;
                    moveResult.steps.steps.Add(boxElements);
                }
                else
                {
                    boxElements[index - 1].intoGate = true;
                    boxElements[index - 1].gate = gateChar;
                    moveResult.steps.steps.Add(new List<Step>(boxElements.Take(index)));
                }
            }
            return moveResult;

        }
        else // no enter gate
        {
            if (!map.InMapArea(nextPos))
            {
                return new MoveResult(false, null, boxElements);
            }

            var e = map.GetElement(nextPos);
            if (e != null && e.type == Type.Wall)
            {
                return new MoveResult(false, null, boxElements);
            }
            return new MoveResult(true, new List<List<Step>> { boxElements }, null);
        }
    }

    private static int IndexOf(Element e, List<Step> steps)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            Step step = steps[i];
            if (step.e == e) return i;
        }
        return -1;
    }

    private static bool CheckIfIntoGate(GameMap map, List<Step> relateBoxes, DIR dir, Vector2I nextPos, out Element gate, out DIR gateDir, out bool isSwallow)
    {
        // check if has gate
        for (int i = 0; i < relateBoxes.Count; i++)
        {
            Step step = relateBoxes[i];
            var e = step.e;
            if (map.IsGateFull(e)) continue;
            // the first cube can not push into
            if (i != 0 && e.GetGate(dir.Opposite()) != ' ')
            {
                // push into gate of this ele
                gate = e;
                gateDir = dir.Opposite();
                isSwallow = false;
                return true;

            }
            // the last cube can not swallow
            if (i + 1 != relateBoxes.Count && e.GetGate(dir) != ' ')
            {
                gate = e;
                gateDir = dir;
                isSwallow = true;
                return true;
            }
        }
        { // can push into wall?
            var e = map.GetElement(nextPos);
            if (e != null && !map.IsGateFull(e) && e.GetGate(dir.Opposite()) != ' ')
            {
                gate = e;
                gateDir = dir.Opposite();
                isSwallow = false;
                return true;
            }
        }
        gate = null;
        gateDir = dir;
        isSwallow = false;
        return false;
    }

    private static List<Step> FindContinuousBox(GameMap map, Vector2I from, int dx, int dy, out Vector2I nextPos)
    {
        var result = new List<Step>();
        nextPos = from;
        while (true)
        {
            if (!map.InMapArea(nextPos)) break;
            var e = map.GetElement(nextPos);
            if (e != null && (e.type == Type.Box || e.type == Type.Player))
            {
                result.Add(new Step(e, nextPos, nextPos.NextPos(dx, dy)));
            }
            else
            {
                break;
            }
            nextPos.X += dx;
            nextPos.Y += dy;
        }
        return result;
    }

}




