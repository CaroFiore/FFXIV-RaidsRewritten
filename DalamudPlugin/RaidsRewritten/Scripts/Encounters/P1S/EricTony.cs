//public uint TerritoryId => 1003;

using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using RaidsRewritten.Network;
using RaidsRewritten.Scripts.Encounters.E1S;
using RaidsRewritten.Utility;


namespace RaidsRewritten.Scripts.Encounters.P1S;



// Most of this is just copied from EdenPrimeTest.cs (Thanks Ricimon)
public class EricTony(
    Mechanic.Factory mechanicFactory,
    DalamudServices dalamud,
    Configuration configuration,
    NetworkClientUi networkClientUi) : IEncounter
{
    public uint TerritoryId => 1003;

    public string Name => "Eric Tony";
    private string RngSeedKey => $"{Name}.RngSeed";
    
    // Mechanics:
    private string CleaveCleanseKey => $"{Name}.CleaveCleanse";
    private readonly List<Mechanic> mechanics = [];
    
    public IEnumerable<Mechanic> GetMechanics()
    {
        return this.mechanics;
    }

    public void RefreshMechanics()
    {
        this.mechanics.Clear();
        
        this.mechanics.Add(mechanicFactory.Create<CleaveCleanse>());
    }

    public void Unload()
    {
        foreach (var mechanic in this.mechanics)
        {
            mechanic.Reset();;
        }
        this.mechanics.Clear();
    }

    public void IncrementRngSeed()
    {
        string rngSeed = configuration.GetEncounterSetting(RngSeedKey, string.Empty);
        rngSeed = EncounterUtilities.IncrementRngSeed(rngSeed);
        configuration.EncounterSettings[RngSeedKey] = rngSeed;
        configuration.Save();
        dalamud.ChatGui.PrintSystemMessage($"RNG seed is now {rngSeed}", PluginInitializer.Name);
        RefreshMechanics();
    }

    public void DrawConfig()
    {
        networkClientUi.DrawConfig();

        ImGui.PushItemWidth(120);
        string rngSeed = configuration.GetEncounterSetting(RngSeedKey, string.Empty);
        if (ImGui.InputText("RNG Seed", ref rngSeed, 100))
        {
            configuration.EncounterSettings[RngSeedKey] = rngSeed;
            configuration.Save();
            RefreshMechanics();
        }
        ImGui.PopItemWidth();
    }
}