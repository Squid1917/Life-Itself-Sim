using Newtonsoft.Json;

public class SimManager
{
    private PriorityQueue<NPC> _npcTickQueue = new();
    private WorldGrid? _worldGrid;
    private float _currentTime;
    private readonly object _lock = new();
    private readonly string _saveFilePath = "world_state.json";
    public readonly SimulationConfig? _config;
    public readonly Random rng;
    private readonly SimServer _server;

    public SimManager(string configPath, SimServer server)
    {
        rng = new Random();
        _config = LoadConfigData(configPath);
        _server = server;
        InitializeSimulation();
    }

    private void InitializeSimulation()
    {

        Console.Clear();
        if (_config == null)
        {
            throw new InvalidOperationException("Failed to load or parse the configuration file.");
        }

        if (File.Exists(_saveFilePath))
        {
            LoadSimState(_saveFilePath);
        }
        else
        {
            Console.WriteLine("Save file not found. Initializing new simulation from config.");
            WorldData worldData = GenerateInitialWorld();
            _worldGrid = new WorldGrid(worldData, _config.SimulationSettings.WorldWidth, _config.SimulationSettings.WorldHeight);
            _currentTime = 0;

            // NPC Creation
            for (int i = 0; i < _config!.SimulationSettings.InitialNPCs; i++)
            {
                // Create a deep copy of the needs list for each NPC
                List<Need> npcNeeds = _config.Needs.Select(n => new Need
                {
                    Name = n.Name,
                    DecayRate = Math.Clamp(n.DecayRate += rng.Next(-5, 5), 5, 20),
                    Threshold = Math.Clamp(n.Threshold += rng.Next(-5, 5), 10, 40),
                    Amount = 100
                }).ToList();

                int age = rng.Next(45, 50);

                NPC newNPC = new NPC(npcNeeds, _config.Buildings, this, age);
                newNPC.Position = GetRandomEmptyCell();
                newNPC.Name = Task.Run(GetRandomNPCNameAsync).Result;

                newNPC.NextTickTime = ((float)rng.Next(15, 45)) / 100;
                _npcTickQueue.Enqueue(newNPC);
            }

            Console.WriteLine("Simulation Config: ");
            Console.WriteLine($"Simulation Time Multiplier: {_config.SimulationSettings.TimeScale}");
            Console.WriteLine($"Simulation Save Interval: {_config.SimulationSettings.SaveIntervalInSimMinutes}");
            Console.WriteLine($"Simulation NPC COunt: {_config.SimulationSettings.InitialNPCs}");
            Console.WriteLine($"Simulation Wolrd Size: {_config.SimulationSettings.WorldWidth}, {_config.SimulationSettings.WorldHeight}");
            Console.WriteLine("Simulation Initalized from config.");
        }
    }

    public void RunSimulation()
    {
        const float simMinuteInHours = 1f / 60f;
        int RealWorldMinuteToSimMinute = _config!.SimulationSettings.TimeScale; // Example: 1 real minute = 60 sim minutes


        while (true)
        {
            int realMillisecondsPerSimMinute = (int)(60f / RealWorldMinuteToSimMinute * 1000f);

            Thread.Sleep(realMillisecondsPerSimMinute);

            lock (_lock)
            {
                _currentTime += simMinuteInHours;

                // Process all NPCs whose tick time is less than or equal to the current time
                while (_npcTickQueue.Count > 0 && _npcTickQueue.Peek()!.NextTickTime <= _currentTime)
                {
                    var npcToTick = _npcTickQueue.Dequeue();

                    npcToTick!.OnTick(this, _worldGrid!);

                    // Re-enqueue the NPC for its next scheduled tick
                    _npcTickQueue.Enqueue(npcToTick);
                }

                // Save state based on simulation time
                if (_config!.SimulationSettings.SaveIntervalInSimMinutes > 0 &&
                    (int)(_currentTime * 60) % _config.SimulationSettings.SaveIntervalInSimMinutes == 0)
                {
                    SaveSimState(_saveFilePath);
                }
            }

            var saveState = new SimSaveState
            {
                CurrentTime = _currentTime,
                Npcs = _npcTickQueue.ToList(),
                WorldState = _worldGrid!.GetWorldData()
            };

            saveState.Npcs.Sort((a, b) => string.Compare(a.Name, b.Name));
            _ = _server.BroadcastStateAsync(saveState);
        }
    }

    public void RequestTick(NPC npc, float timeInHours)
    {
        lock (_lock)
        {
            npc.NextTickTime = _currentTime + timeInHours;
        }
    }

    private void SaveSimState(string filePath)
    {
        var saveState = new SimSaveState
        {
            CurrentTime = _currentTime,
            Npcs = _npcTickQueue.ToList(),
            WorldState = _worldGrid!.GetWorldData()
        };
        string json = JsonConvert.SerializeObject(saveState, Formatting.Indented);
        File.WriteAllText(filePath, json);
        Console.WriteLine($"Simulation state saved at sim time: {_currentTime}");
    }

    private void LoadSimState(string filePath)
    {
        string json = File.ReadAllText(filePath);
        var saveState = JsonConvert.DeserializeObject<SimSaveState>(json)!;
        _currentTime = saveState.CurrentTime;
        _worldGrid = new WorldGrid(saveState.WorldState, _config.SimulationSettings.WorldWidth, _config.SimulationSettings.WorldHeight);
        _npcTickQueue = new PriorityQueue<NPC>();
        foreach (NPC npc in saveState.Npcs)
        {
            // Set the _Buildings list for each loaded NPC
            npc.SetBuildings(_config!.Buildings);
            npc.SetManager(this);
            _npcTickQueue.Enqueue(npc);
        }
        Console.WriteLine($"Simulation state loaded. Current sim time: {_currentTime}");
    }

    private SimulationConfig? LoadConfigData(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file not found at: {path}");
        }
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<SimulationConfig>(json);
    }

    private WorldData GenerateInitialWorld()
    {
        var cells = new List<Cell>();
        Random rand = new Random();
        int width = _config!.SimulationSettings.WorldWidth;
        int height = _config.SimulationSettings.WorldHeight;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var type = "Path"; // Default type is "Path"
                Building? building = null;
                foreach (var template in _config.Buildings)
                {
                    if (rand.NextDouble() < template.GenerationChance)
                    {
                        type = template.Name; // Use building name as cell type
                        building = new Building
                        {
                            Name = template.Name,
                            GenerationChance = template.GenerationChance,
                            Satisfactions = template.Satisfactions
                        };
                        break;
                    }
                }
                cells.Add(new Cell { Position = new Position { X = x, Y = y }, Type = type, Building = building });
            }
        }
        return new WorldData { Cells = cells };
    }

    private Position GetRandomEmptyCell()
    {
        var rand = new Random();
        int width = _config!.SimulationSettings.WorldWidth;
        int height = _config.SimulationSettings.WorldHeight;
        while (true)
        {
            int x = rand.Next(0, width);
            int y = rand.Next(0, height);
            Position pos = new Position { X = x, Y = y };
            if (_worldGrid!.GetCell(pos)?.Type == "Path") // Check for "Path" string
            {
                return pos;
            }
        }
    }

    private async Task<string> GetRandomNPCNameAsync()
    {
        try
        {
            // Make the actual API call
            HttpClient _httpClient = new HttpClient();
            string responseBody = await _httpClient.GetStringAsync("https://randomuser.me/api/");

            dynamic? data = JsonConvert.DeserializeObject(responseBody);
            if (data?.results != null && data!.results.Count > 0)
            {
                string firstName = data!.results[0].name.first;
                string lastName = data.results[0].name.last;
                return $"{firstName} {lastName}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting name from API: {ex.Message}");
        }
        return "Unnamed NPC";
    }

    public void KillNPC(NPC npc)
    {
        _ = _npcTickQueue.ToList().Remove(npc);
    }
}