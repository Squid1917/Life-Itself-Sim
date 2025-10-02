using MessagePack;


//Cell Data
[MessagePackObject]
public class Cell
{
    [Key(0)]
    public required Position Position { get; set; }
    [Key(1)]
    public required string Type { get; set; }
    [Key(2)]
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
[MessagePackObject]
public class WorldData
{
    [Key(0)]
    public required List<Cell> Cells { get; set; }
}

[MessagePackObject]
public class SimSaveState
{
    [Key(0)]
    public required float CurrentTime { get; set; }
    [Key(1)]
    public required List<NPC> Npcs { get; set; }
    [Key(2)]
    public required WorldData WorldState { get; set; }
}


//Need Config
[MessagePackObject]
public class Need
{
    [Key(0)]
    public required string Name { get; set; } // Name of the need
    [Key(1)]
    public required float DecayRate { get; set; } // How many points it drops every hour
    [Key(2)]
    public required float Threshold { get; set; } // Threshold needed to be filled
    [Key(3)]
    public float Amount { get; set; }
}

[MessagePackObject]
public class Satisfaction
{
    [Key(0)]
    public required string NeedName { get; set; }
    [Key(1)]
    public required float Amount { get; set; }
    [Key(2)]
    public required float TimeCostInHours { get; set; }
}

[MessagePackObject]
public class Building
{
    [Key(0)]
    public required string Name { get; set; }
    [Key(1)]
    public required double GenerationChance { get; set; }
    [Key(2)]
    public List<Satisfaction>? Satisfactions { get; set; }
}
