using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using Flecs.NET.Core;
using RaidsRewritten.Game;
using RaidsRewritten.Scripts.Conditions;
using RaidsRewritten.Utility;

namespace RaidsRewritten.Scripts.Encounters.UCOB;

public class TemperatureControl : Mechanic
{
    private struct HeatData
    {
        public float HeatValue;
        public int ID;
        public float DelaySeconds;
    }

    private readonly Dictionary<uint, HeatData> HeatDict = new Dictionary<uint, HeatData>
    {
        {
            9900, new HeatData { HeatValue = 50.0f, ID = 9900, DelaySeconds = 0.6f } //Fireball (Twin)
        },
        {
            9901, new HeatData { HeatValue = 20.0f, ID = 9901, DelaySeconds = 1.1f } //Liquid hell
        },
        {
            9914, new HeatData { HeatValue = 50.0f, ID = 9914, DelaySeconds = 3.6f} //Megaflare (Nael/AOE)
        },
        {
            9917, new HeatData { HeatValue = 40.0f, ID = 9917, DelaySeconds = 0.8f } //Thermeonic Beam
        },
        {
            9925, new HeatData { HeatValue = 50.0f, ID = 9925, DelaySeconds = 1.0f } //Fireball (Firehorn)
        },
        {
            9926, new HeatData { HeatValue = -40.0f, ID = 9926, DelaySeconds = 1.0f } //Iceball (Iceclaw)
        },
        {
            9937, new HeatData { HeatValue = 100.0f, ID = 9937, DelaySeconds = 0.1f } //Seventh Umbral Era
        },
        {
            9938, new HeatData { HeatValue = 20.0f, ID = 9938, DelaySeconds = 0.2f } //Calamitous Flame
        },
        {
            9939, new HeatData { HeatValue = 30.0f, ID = 9939, DelaySeconds = 0.2f } //Calamitous Blaze (Final hit)
        },
        { 
            9940, new HeatData { HeatValue = 20.0f, ID = 9940, DelaySeconds = 0.9f } //Flare Breath
        },
        { 
            9942, new HeatData { HeatValue = 50.0f, ID = 9942, DelaySeconds = 3.6f} //Gigaflare
        },
        { 
            9962, new HeatData { HeatValue = 10.0f, ID = 9962, DelaySeconds = 0.9f } //Ahk Morn (1st hit)
        },
        { 
            9963, new HeatData { HeatValue = 10.0f, ID = 9963, DelaySeconds = 0.9f } //Ahk Morn (2nd+ Hit)
        },
        {
            9964, new HeatData { HeatValue = 100.0f, ID = 9964, DelaySeconds = 0.9f } //Morn Afah
        }
        //9970 Flames of Rebirth (20s till targetable)
    };

    private readonly List<Entity> attacks = [];
    private Query<Player.Component, Temperature.Component>? playerTemperatureQuery;
    //private int AfahMultiplier = 1;

    public override void Reset()
    {
        foreach (var attack in this.attacks)
        {
            attack.Destruct();
        }
        this.attacks.Clear();

        World.DeleteWith<Temperature.Component>();
    }

    public override void OnFrameworkUpdate(IFramework framework)
    {
        // This looks like a null check?
        if (!playerTemperatureQuery.HasValue)
        { // Find all entities who have the player and temperature component
            //
            playerTemperatureQuery = World.QueryBuilder<Player.Component, Temperature.Component>()
                .TermAt(0).Up()
                .With<Player.LocalPlayer>().Up()
                .Cached().Build();
        }
        if (!playerTemperatureQuery.Value.IsTrue())
        {
            var player = CommonQueries.LocalPlayerQuery.First();
            if (player.IsValid())
            {
                Temperature.SetTemperature(player);
            }
        }
    }
   
    public override void OnDirectorUpdate(DirectorUpdateCategory a3)
    {
        if (a3 == DirectorUpdateCategory.Wipe ||
            a3 == DirectorUpdateCategory.Recommence)
        {
            Reset();
        }
    }

    public override void OnCombatEnd()
    {
        Reset();
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    { // Fires whenever a game action is found.
        try
        {
            // Filter out any actions that do are not.. an action with a source?
            // Im guessing this is looking for actions from some source which could be a boss doing an attack.
            if (set.Action == null) { return; }
            if (set.Source == null) { return; }

            // Check if the action is an action that applies heat, then save the value (amount) of heat to var Heat.
            if (!HeatDict.TryGetValue(set.Action.Value.RowId, out var Heat)) { return; }
            
            // localPLayer from dalamud.
            var localPlayer = Dalamud.ObjectTable.LocalPlayer;
            // Odd error catch which Im surprised if it ever runs but I guess it's not bad to have.
            // Perhaps the localPlayer does not exist on every single frame?
            if (localPlayer == null) { return; }

            // A delayed action, so heat will only apply Heat.DelaySeconds 
            var da = DelayedAction.Create(this.World, () =>     
            {   // 
                foreach (var targetEffects in set.TargetEffects)
                {   // targetEffects is a list of targets of the action (set.TargetEffects).
                    // If the localplayer is found in one of those targets, continue.
                    if (targetEffects.TargetID == localPlayer.GameObjectId)
                    {   
// This seems to be where ECS logic starts.
// Do a query (common query) over every single entity in the "World"
// World here low-key sounds like the name you'd get from a ECS tutorial since it sounds like game dev logic (tell me if Im wrong :D)
// Do a query selecting every single Entity that possesses the Player component (so entities that are players).
// Then update the heat value of every entity found.
                        using var q = World.Query<Player.Component>();
                        q.Each((Entity e, ref Player.Component pc) =>
                        {
                            float HeatDelta = Heat.HeatValue;
                            //if (Heat.ID == 9964)
                            //{
                            //    HeatDelta *= AfahMultiplier++;

                            //}
                            Temperature.HeatChangedEvent(e, HeatDelta, 0, Heat.ID);
                        });
                        return;
                    }
                }
            }, Heat.DelaySeconds);
// Lastly, attacks is basically the logic behind a specific mechanic.
// This mechanic is called TemperatureControl, and one of the attacks is updating heat when entities get hit.
            this.attacks.Add(da);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToStringFull());
        }
    }
}