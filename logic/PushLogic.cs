using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class PushLogic : RefCounted
{

    public static bool GetStepsByPlayerMove(GameMap gameMap, Element player, DIR dir, out List<Step> steps)
    {
        steps = new List<Step>();

        var pos = player.Position;
        var nextPos = pos.NextPos(dir);

        // check next pos
        if (!gameMap.InMapArea(nextPos)) return false;
        var e = gameMap.GetElement(nextPos);
        // next pos is empty
        if (e == null)
        {
            steps.Add(Step.CreateNormal(player, dir));
            return true;
        }
        // swallow next pos
        if (e != null && e.Type == Type.Box && player.CanSwallowOther(dir))
        {
            steps.Add(Step.CreateNormal(player, dir));
            steps.Add(Step.CreateEnter(e, dir.Opposite(), player, move: false));
            return true;
        }
        // enter
        if (e != null && e.CanEnterFrom(dir))
        {
            steps.Add(Step.CreateEnter(player, dir, e));
            return true;
        }
        // else: push
        if (GetStepsByPushForceOn(gameMap, player, dir, out var pushSteps))
        {
            steps.Add(Step.CreateNormal(player, dir));
            steps.AddRange(pushSteps);
            return true;
        }

        // push failed
        return false;
    }

    public static bool GetStepsByLeaveGate(GameMap gameMap, Element gate, DIR dir, out List<Step> steps)
    {
        steps = new List<Step>();
        var source = gate.swallow;
        if (source == null) return false;
        if (gate.GetGate(dir) != gate.swallowGate) return false;
        var gatePos = gate.Position;

        var nextPos = gatePos.NextPos(dir);
        var e = gameMap.GetElement(nextPos);

        // check next pos
        if (!gameMap.InMapArea(nextPos)) return false;
        // next pos is empty
        if (e == null)
        {
            steps.Add(Step.CreateLeave(source, dir, gate));
            return true;
        }
        // swallow next pos
        if (source.CanSwallowOther(dir) && e != null && e.Type == Type.Box)
        {
            steps.Add(Step.CreateLeave(source, dir, gate));
            steps.Add(Step.CreateEnter(e, dir.Opposite(), source, move: false));
            return true;
        }
        // enter
        if (e != null && e.CanEnterFrom(dir))
        {
            steps.Add(Step.CreateLeaveAndEnter(source, dir, gate, e));
            return true;
        }
        // else: push
        if (GetStepsByPushForceOn(gameMap, source, dir, out var pushSteps))
        {
            steps.Add(Step.CreateLeave(source, dir, gate));
            steps.AddRange(pushSteps);
            return true;
        }

        // push failed
        return false;
    }

    // apply force form element.Pos -> element.Pos.NextPos
    // asset nextPos'element is not null
    public static bool GetStepsByPushForceOn(GameMap gameMap, Element source, DIR dir, out List<Step> steps)
    {
        steps = new List<Step>();
        var pos = source.Position.NextPos(dir); // cur force source
        var lastElement = source;
        // loop check next element
        while (true)
        {
            if (!gameMap.InMapArea(pos)) return false;
            var e = gameMap.GetElement(pos);
            // move to empty pos (this branch cannot happen)
            if (e == null) return true;
            // stop move
            if (e.Type == Type.Wall || e.Type == Type.Player) return false;

            var nextPos = pos.NextPos(dir);
            Element nextElement = gameMap.GetElement(nextPos);

            // check swallow (do not check the source of force)
            // noly box can be swallowed
            if (e != null && e.Type == Type.Box && e.CanSwallowOther(dir) && nextElement != null && nextElement.Type == Type.Box)
            {
                steps.Add(Step.CreateNormal(e, dir));
                steps.Add(Step.CreateEnter(nextElement, dir.Opposite(), e, move: false));
                return true;
            }
            // enter next
            if (nextElement != null && nextElement.CanEnterFrom(dir))
            {
                steps.Add(Step.CreateEnter(e, dir, nextElement));
                return true;
            }

            // else: pushable
            steps.Add(Step.CreateNormal(e, dir));

            // to check nextPos
            lastElement = e;
            pos = pos.NextPos(dir);
        }
    }
}
