using System.Collections.Generic;


//Cell Data
public class Cell
{
    public required Position Position { get; set; }
    public required string Type { get; set; }
    public Building? Building { get; set; }
}

//Sim Settings
public class SimulationConfig
{
    public required SimulationSettings SimulationSettings { get; set; }
    public required List<Need> Needs { get; set; }
    public required List<Building> Buildings { get; set; }
}

public class SimulationSettings
{
    public required int TimeScale { get; set; } // Time multiplier in sim. Simulation minutes per real world minute
    public required int WorldWidth { get; set; }
    public required int WorldHeight { get; set; }
    public required int InitialNPCs { get; set; }
    public required int SaveIntervalInSimMinutes { get; set; } // How oten the sim saves it state
    public required int IdleNextTick { get; set; } // Amount of minutes for next tick time when the npc is idle
}


//World Data
public class WorldData
{
    public required List<Cell> Cells { get; set; }
}

public class SimSaveState
{
    public required float CurrentTime { get; set; }
    public required List<NPC> Npcs { get; set; }
    public required WorldData WorldState { get; set; }
}


//Need Config
public class Need
{
    public required string Name { get; set; } // Name of the need
    public required float DecayRate { get; set; } // How many points it drops every hour
    public required float Threshold { get; set; } // Threshold needed to be filled
    public float Amount { get; set; }
}

public class Satisfaction
{
    public required string NeedName { get; set; }
    public required float Amount { get; set; }
    public required float TimeCostInHours { get; set; }
}

public class Building
{
    public required string Name { get; set; }
    public required double GenerationChance { get; set; }
    public List<Satisfaction>? Satisfactions { get; set; }
}
