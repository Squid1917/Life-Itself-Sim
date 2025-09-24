using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class NPC
{
    public string Name { get; set; }
    public Position Position { get; set; }
    public string CurrentStatus { get; set; }
    public float NextTickTime { get; set; }

    private Timer? _timer;
    
    public List<Need> _Needs { get; set; }
    private List<Building> _Buildings;

    public NPC(List<Need> Needs, List<Building> Buildings)
    {
        _Needs = Needs;
        _Buildings = Buildings;
        CurrentStatus = "Idle";
    }
    
    public void SetBuildings(List<Building> buildings)
    {
        _Buildings = buildings;
    }

    public void OnTick(SimManager manager, WorldGrid world)
    {
        DecayNeeds();

        // Order needs by amount, filter needs that are below threshold, get a list of at most the top 5
        List<Need> urgentNeeds = _Needs.OrderBy(need => need.Amount)
            .ToList()
            .FindAll(need => need.Amount <= need.Threshold);

        Need? fillingNeed = null;
        if (urgentNeeds.Count > 0)
        {
            fillingNeed = urgentNeeds[manager.rng.Next(urgentNeeds.Count)];
        }


        if (fillingNeed != null)
        {
            // Find buildings that can satisfy the chosen need
            List<Building> buildings = _Buildings.FindAll(
                x => x.Satisfactions.Any(y => y.NeedName == fillingNeed.Name));

            Satisfaction? satisfaction = null;
            Position targetPos = Position.Invalid;

            if (buildings.Count > 0)
            {
                targetPos = world.FindNearCellOfType(Position, fillingNeed.Name, manager.rng, out satisfaction);
            }

            if (targetPos != Position.Invalid)
            {
                float travelTime = Position.Distance(Position, targetPos) / 10f;
                Position = targetPos;

                float finalAmount = fillingNeed.Amount + satisfaction!.Amount;

                // Calculate total time in simulation seconds
                float totalTimeInSimSeconds = (travelTime + satisfaction.TimeCostInHours) * 3600f;

                // Calculate incremental amount per simulation second
                float incrementalAmountPerSimSecond = satisfaction.Amount / totalTimeInSimSeconds;

                // Adjust incremental amount for the real-world timer
                float incrementalAmountPerRealSecond = (int)MathF.Ceiling(incrementalAmountPerSimSecond * manager._config.SimulationSettings.TimeScale);

                _timer = new Timer(_ =>
                {
                    if (fillingNeed.Amount >= finalAmount)
                    {
                        _timer?.Dispose();
                        _timer = null;
                        CurrentStatus = "Idle";
                        return;
                    }

                    // Increment the need by the per-real-second amount
                    fillingNeed.Amount = Math.Clamp(fillingNeed.Amount + incrementalAmountPerRealSecond,0,100);

                }, null, 0, 1000); // Trigger every 1000 milliseconds (1 second)

                CurrentStatus = $"Filling {fillingNeed.Name}";

                // This line is correct as it already uses simulation hours
                manager.RequestTick(this, travelTime + satisfaction.TimeCostInHours);
            }
            else
            {
                Console.WriteLine($"{Name} couldn't find a place to fill {fillingNeed.Name}. Retrying in 15 minutes");
                manager.RequestTick(this, 0.25f);
            }
        }
        //NPC is idle
        manager.RequestTick(this, manager._config.SimulationSettings.IdleNextTick / 60f);
    }
    
    private void DecayNeeds()
    {
        foreach (Need need in _Needs)
        {
            need.Amount = Math.Clamp(need.Amount - need.DecayRate, 0, 100);
        }
    }
}