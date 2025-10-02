public class WorldGrid
{
    private Cell[,] _grid;
    private int _width;
    private int _height;

    public WorldGrid(WorldData data, int width, int height)
    {
        _width = width;
        _height = height;
        _grid = new Cell[width, height];
        foreach (var cell in data.Cells)
        {
            _grid[cell.Position.X, cell.Position.Y] = cell;
        }
    }

    public Cell? GetCell(Position position)
    {
        // Check for out-of-bounds access
        if (position.X < 0 || position.X >= _width || position.Y < 0 || position.Y >= _height)
        {
            return null;
        }
        return _grid[position.X, position.Y];
    }
    
    public Position FindNearCellOfType(Position startPos, string needName, Random rng, out Satisfaction satisfaction)
    {
        satisfaction = null;

        var suitableBuildings = new List<Tuple<Position, float, Building>>();
        
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Cell? currentCell = _grid[x, y];
                if (currentCell != null && currentCell.Building != null &&
                    currentCell.Building.Satisfactions!.Any(s => s.NeedName == needName))
                {
                    Position cellPosition = new Position { X = x, Y = y };
                    float distance = Position.Distance(startPos, cellPosition);
                    suitableBuildings.Add(Tuple.Create(cellPosition, distance, currentCell.Building));
                }
            }
        }

        if (suitableBuildings.Count == 0)
        {
            return Position.Invalid;
        }

        var top5Closest = suitableBuildings.OrderBy(b => b.Item2).Take(5).ToList();
        
        int randomIndex = rng.Next(top5Closest.Count);
        var chosenEntry = top5Closest[randomIndex];

        satisfaction = chosenEntry.Item3!.Satisfactions!.FirstOrDefault(s => s.NeedName == needName);

        return chosenEntry.Item1;
    }

    public WorldData GetWorldData()
    {
        var cells = new List<Cell>();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Cell? cell = _grid[x, y];
                if (cell != null && cell.Type != "Path")
                {
                    cells.Add(cell);
                }
            }
        }
        return new WorldData
        {
            Cells = cells
        };
    }
}