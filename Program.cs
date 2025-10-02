class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Life-Itself Simulation and Server...");
        try
        {
            int serverPort = 5050;
            var server = new SimServer(serverPort);
            
            // Start the server in the background
            _ = server.StartAsync();

            var simManager = new SimManager("config.json", server);
            simManager.RunSimulation();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
