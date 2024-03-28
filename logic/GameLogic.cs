using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

public partial class GameLogic : RefCounted
{
    public const double MOVE_INTERVAL = 0.13;
    public static readonly Stack<StepGroup> history = new();
    private static readonly Queue<KeyValuePair<Element, DIR>> checkGateFirstQueue = new();


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
                ApplyStep(Game.gameMap, stepOnce, true);
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
        while (true)
        {
            // find one move to move
            MoveResult moveResult = null;
            while (checkGateFirstQueue.Count != 0)
            {
                var pair = checkGateFirstQueue.Dequeue();
                var gate = pair.Key;
                var dir = pair.Value;
                var gateChar = gate.GetGate(dir);
                GD.Print($"check checkGateFirstQueue gate:{gate} dir:{dir}");
                if (!gate.swallows.TryGetValue(gateChar, out var e)) continue;
                moveResult = CheckGateAround(gameMap, gate, gateChar, e, dir);
                if (moveResult.moveSuccess)
                {
                    GD.Print("find success in checkGateFirstQueue");
                    break;
                }
            }
            if (moveResult == null || !moveResult.moveSuccess)
            {
                foreach (var pair in gameMap.boxData)
                {
                    var gate = pair.Value;
                    if (gate.swallows.Count == 0) continue;
                    foreach (var item in gate.swallows)
                    {
                        var gateChar = item.Key;
                        moveResult = CheckGateAround(gameMap, gate, gateChar, item.Value);
                        if (!moveResult.moveSuccess) continue;
                        break;
                    }
                    if (moveResult?.moveSuccess ?? false) break;
                }
            }
            if (moveResult != null && moveResult.moveSuccess)
            {
                curStepHistory.steps.AddRange(moveResult.steps.steps);
                foreach (var stepOnce in moveResult.steps.steps)
                {
                    await DoMove(stepOnce);
                }
            }
            else
            {
                break;
            }
        }
        checkGateFirstQueue.Clear();
    }
    public static async Task DoMove(List<Step> steps)
    {
        GameMap gameMap = Game.gameMap;

        ApplyStep(gameMap, steps);
        foreach (var step in steps)
        {
            GD.Print($"DOMOVE step:{step}");
            var p = Game.CalcNodePosition(gameMap, step.to);
            {
                Tween tween = step.e.node.CreateTween();
                if (step.outGate)
                {
                    tween.TweenProperty(step.e.node, "position", Game.CalcNodePosition(gameMap, step.from), 0.01);
                }
                tween.TweenProperty(step.e.node, "position", p, MOVE_INTERVAL);
                if (step.intoGate)
                {
                    tween.TweenProperty(step.e.node, "scale", Res.Scale_Swallow, MOVE_INTERVAL);
                    tween.TweenProperty(step.e.node, "z_index", Res.Z_Swallow, MOVE_INTERVAL);
                }
                if (step.outGate)
                {
                    tween.TweenProperty(step.e.node, "scale", Res.Scale_Normal, MOVE_INTERVAL);
                    tween.TweenProperty(step.e.node, "z_index", Res.Z_Ground, MOVE_INTERVAL);
                }
            }
            MapAllSawllowedElement(step.e, (e, layer) =>
            {
                if (e.node != null)
                {
                    Tween tween = e.node.CreateTween();
                    tween.TweenProperty(e.node, "position", p, MOVE_INTERVAL);
                    tween.TweenProperty(e.node, "z_index", Res.Z_Swallow + layer, MOVE_INTERVAL);
                }
            }, step.intoGate ? 1 : 0);
        }
        Game.Instance.SetProcess(false);
        await Game.Instance.Wait(MOVE_INTERVAL);
        Game.Instance.SetProcess(true);

        // create node for swallow copy
        foreach (var gate in gameMap.boxData.Values)
        {
            Vector2I pos = gameMap.GetElementPos(gate);
            MapAllSawllowedElement(gate, (e, layer) =>
            {
                if (e.node == null)
                {
                    var node = ElementNode.CreateElementNode(e, Game.CalcNodePosition(gameMap, pos));
                    node.Scale = Res.Scale_Normal * Mathf.Pow(Res.Scale_Swallow_f, layer + 1);
                    node.ZIndex = Res.Z_Swallow + layer;
                    Game.Instance.AddElementNode(node);
                    e.node = node;
                }
            });
        }
    }

    private static void MapAllSawllowedElement(Element e, Action<Element, int> action, int layerStart = 0)
    {
        foreach (var s in e.swallows.Values)
        {
            action.Invoke(s, layerStart);
            MapAllSawllowedElement(s, action, layerStart + 1);
        }
    }

    private static IEnumerable<Element> GetAllSwallowedElement(Element e)
    {
        return e.swallows.Values.Concat(e.swallows.Values.SelectMany(e => GetAllSwallowedElement(e)));
    }

    public static void ApplyStep(GameMap map, List<Step> steps, bool reverse = false)
    {
        if (!reverse)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                Step step = steps[i];
                map.RemoveElement(step.e);
                if (!step.intoGate) map.SetElement(step.e, step.to.X, step.to.Y);
                if (step.intoGate)
                {
                    char gateChar = step.gateElement.GetGate(step.gateDIR);
                    step.gateElement.swallows[gateChar] = step.e;
                    foreach (var dir in step.gateElement.GetDIRByGate(gateChar, step.gateDIR))
                    {
                        checkGateFirstQueue.Enqueue(new KeyValuePair<Element, DIR>(step.gateElement, dir));
                    }
                    foreach (var e in map.FindGateElements(gateChar, step.gateElement))
                    {
                        List<DIR> DIRs = e.GetDIRByGate(gateChar);
                        if (DIRs.Count > 0)
                        {
                            Element copy = step.e.MakeCopy();
                            copy.Rotate(step.gateDIR, DIRs.FirstOrDefault());
                            e.swallows[gateChar] = copy;
                        }
                        foreach (var dir in e.GetDIRByGate(gateChar))
                        {
                            checkGateFirstQueue.Enqueue(new KeyValuePair<Element, DIR>(e, dir));
                        }
                    }
                }
                if (step.outGate)
                {
                    char gateChar = step.gateElement.GetGate(step.gateDIR);
                    step.gateElement.swallows.Remove(gateChar);
                    foreach (var e in map.FindGateElements(gateChar, step.gateElement))
                    {
                        if (e.swallows.Remove(gateChar, out var copy))
                        {
                            foreach (var swallowItem in copy.swallows.Values)
                            {
                                Game.Instance.RemoveElementNode(swallowItem.node);

                            }
                            Game.Instance.RemoveElementNode(copy.node);
                        }
                    }
                }
            }
        }
        else
        {
            //for (int i = steps.Count - 1; i >= 0; i--)
            //{
            //    Step step = steps[i];
            //    RemoveElement(step.e);
            //    SetElement(step.e, step.from.X, step.from.Y);

            //    if (step.intoGate)
            //    {
            //        gateData.Remove(step.gate);
            //    }
            //    if (step.outGate)
            //    {
            //        gateData[step.gate] = step.e;
            //    }
            //}
        }
    }

    public static MoveResult CheckGateAround(GameMap gameMap, Element gate, char gateChar, Element e, DIR? specDir = null)
    {
        GD.Print($"check gate:{gateChar} specDir?:{specDir}");
        foreach (var dir in Enum.GetValues<DIR>())
        {
            if ((specDir == null || specDir == dir) && gate.GetGate(dir) == gateChar)
            {
                Vector2I pos = gameMap.GetElementPos(gate);
                var nextPos = pos.NextPos(dir);
                if (!gameMap.InMapArea(nextPos) || gameMap.GetElement(nextPos) != null) continue;

                var step = new Step(e, pos, pos.NextPos(dir), false, true, gate, dir);
                return new MoveResult(true, new List<List<Step>> { new List<Step> { step } }, null);
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
                    new Step(swallowEle, swallowPos, swallowPos, true, false, gate, gateDir)
                });
            }
            else
            {
                var index = IndexOf(gate, boxElements);
                if (index < 0) // push all e into wall
                {
                    boxElements.Last().intoGate = true;
                    boxElements.Last().gateElement = gate;
                    boxElements.Last().gateDIR = gateDir;
                    moveResult.steps.steps.Add(boxElements);
                }
                else
                {
                    boxElements[index - 1].intoGate = true;
                    boxElements[index - 1].gateElement = gate;
                    boxElements[index - 1].gateDIR = gateDir;
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




