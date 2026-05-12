using System;
using Dalamud.Plugin.Services;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using Flecs.NET.Core;
using RaidsRewritten.Scripts.Conditions;
using Player = RaidsRewritten.Game.Player;
using System.Linq;
using System.Collections.Generic;

namespace RaidsRewritten.Scripts.Encounters.E1S;

public class CleaveCleanse : Mechanic
{
    private const float DebuffApplicationTime = 41.0f;

    private readonly List<uint> CleaveList = [
        28073, 28076, // Out / In dunno which is which
        28071, 28074, // Left and Right
        28070, 28075, // Also left and right, but maybe right>left?
        28072, 28077 // In then Out I think
    ];

    private readonly List<Entity> attacks = [];

    public override void OnCombatStart()
    {
        Logger.Info("OnCombatStart()");

        //Give the player a debuff 10 seconds in.
        var da = DelayedAction.Create(this.World, delay : DebuffApplicationTime, action : () =>
        {
            CommonQueries.LocalPlayerQuery.Each((Entity e, ref Player.Component _) =>
            {
                Conditions.Pacify.ApplyToTarget(e, float.PositiveInfinity);
            });
        });
        this.attacks.Add(da);
    }

    public override void OnFrameworkUpdate(IFramework framework)
    {
        
    }

    void removeDebuffFromLocalPlayer()
    {
        Logger.Info("RemoveDebuffFromLocalPlayer()");

        CommonQueries.LocalPlayerQuery.Each((Entity e, ref Player.Component pc) => {
            
            using var query = World.QueryBuilder<Condition.Component, Condition.Id>().With(Ecs.ChildOf, e).Build();

            World.DeferBegin();
            query.Each((Entity conditionEntity, ref Condition.Component cc, ref Condition.Id id) =>
            {
                if (id.Value == Pacify.Id)
                {
                    conditionEntity.Destruct();
                }
            });
            World.DeferEnd();
        });
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        //Logger.Info("OnActionEffectEvent");
        try
        {
            // Some null catching
            var localPlayer = Dalamud.ObjectTable.LocalPlayer;
            if (localPlayer == null){return ;}
            if (set.Action == null){return ; }

            Logger.Info($"Action found: {set.Action.Value.Name} with ID {set.Action.Value.RowId}, is it any of {string.Join(", ", CleaveList)}");
            Logger.Info($"Did this target have targets?: {set.TargetEffects.Any(player => player.TargetID == localPlayer.GameObjectId)}");
            if (!CleaveList.Contains(set.Action.Value.RowId)){return;}

            if (set.TargetEffects.Any(player => player.TargetID == localPlayer.GameObjectId))
            {
                removeDebuffFromLocalPlayer();
            }



        } catch (Exception e)
        {
            Logger.Info(e.ToString());
        }


    }

    //Below here are all reset code related things
    public override void OnCombatEnd()
    {
        Logger.Info("OnCombatEnd()");

        Reset();
    }
    public override void OnDirectorUpdate(DirectorUpdateCategory a3)
    {
        if (a3 == DirectorUpdateCategory.Wipe ||
            a3 == DirectorUpdateCategory.Recommence)
        {
            Reset();
        }
    }
    public override void Reset()
    {
        Logger.Info("Reset()");

        foreach (var attack in this.attacks)
        {
            attack.Destruct();
        }
        this.attacks.Clear();
    }
}