using System;
using Dalamud.Plugin.Services;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using Flecs.NET.Core;
using RaidsRewritten.Scripts.Conditions;
using Player = RaidsRewritten.Game.Player;
using System.Linq;

namespace RaidsRewritten.Scripts.Encounters.E1S;

public class AutoCleanseDebuff : Mechanic
{
    private const uint E1SAutoAttackId = 871;

    

    public override void OnCombatStart()
    {
        Logger.Info("OnCombatStart()");

        //This lambda is just a fancy way of saying for each entity found in the query.
        // It's a bit of a hassle to learn but it's also unavoidable 
        // Anyway, we apply a stun at tehs start of the fight to the localPlayer.
        CommonQueries.LocalPlayerQuery.Each((Entity e, ref Player.Component _) =>
        {
            Conditions.Stun.ApplyToTarget(e, 6.0f);
        });
        
    }

    public override void OnFrameworkUpdate(IFramework framework)
    {
        
    }

    void removeStunFromLocalPlayer()
    {
        Logger.Info("RemoveStunFromPlayer()");
        /* for the longest time I didnt understsand what refs are, but its literally
        just like.. a pointer? but not a pointer? Its a reference to the original object,
        If you didnt use "ref" you would not be able to modify it.
        Although.. I'd say you could just get rid of it here, but flecs seems to hate it when i do that.
        So maybe I dont understand it at all. Well, I'll just use it for consistenccy.
        */
        CommonQueries.LocalPlayerQuery.Each((Entity e, ref Player.Component pc) => {
            // okay so this one is a bit complex.
            // I believe we are querying for all Condition Components (and their IDs) who happen to 
            // be a child.. entity? of e, which will always be our local player due to the query above.
            using var query = World.QueryBuilder<Condition.Component, Condition.Id>().With(Ecs.ChildOf, e).Build();
            // Now the query is built, so we can do calls on it.
        
            // Foreach conditionEntity that has the same ID as stun, destruct.
            World.DeferBegin();
            query.Each((Entity conditionEntity, ref Condition.Component cc, ref Condition.Id id) =>
            {
                if (id.Value == Stun.Id)
                {
                    conditionEntity.Destruct();
                }
            });
            World.DeferEnd();
        });
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        Logger.Info("OnActionEffectEvent");
        try
        {
            // Some null catching
            var localPlayer = Dalamud.ObjectTable.LocalPlayer;
            if (localPlayer == null){return ;}
            if (set.Action == null){return ; }
            if (set.Action == null){return ; }
            
            //check if the action is the auto attack
            Logger.Info($"Action found: {set.Action.Value.Name} with ID {set.Action.Value.RowId}, is it {E1SAutoAttackId}?");
            if (set.Action.Value.RowId != E1SAutoAttackId) {return ;}

            /*Old logic that does a foreach, but we only care about the local player so it doesnt seem necessary?
            foreach(var targetEffect in set.TargetEffects)
            {
                if (targetEffect.TargetID != localPlayer.GameObjectId) continue;
                // requries "using" since Flecs queries dont destroy themselves without it.
                applyStunToLocalPlayer();
            }*/

            // Instead, check if theres target in the list who has the same id as the localplayer.
            if (set.TargetEffects.Any(player => player.TargetID == localPlayer.GameObjectId))
            {
                removeStunFromLocalPlayer();
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
    }
}