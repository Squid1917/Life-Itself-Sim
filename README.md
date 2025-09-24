# Life Simulation with WebSocket Server

A C# console application that runs a life simulation. NPCs with needs like hunger and social move around a grid-based world to interact with buildings and fulfill their needs. The simulation state is broadcast in real-time via a WebSocket server.

## Features
* **Grid-based World Simulation:** The simulation environment is a configurable grid, which includes different building types and paths.
* **Autonomous NPCs:** Non-Player Characters (NPCs) have needs (e.g., Hunger, Social) that decay over time. They autonomously navigate the world to find and use buildings to satisfy their needs.
* **WebSocket Server:** The application includes a WebSocket server that broadcasts the real-time simulation state, including NPC positions and needs, to connected clients.
* **Configurable Simulation Settings:** Simulation parameters, such as world size, number of initial NPCs, and need decay rates, can be easily configured using the `config.json` file.
* **State Persistence:** The simulation state, including all NPC and world data, is automatically saved and can be loaded upon restart.

## Getting Started
### Prerequisites
* .NET 6.0 or later.
* The project uses the `Newtonsoft.Json` library for JSON serialization and deserialization.

### Running the Application
1.  Navigate to the project directory.
2.  Restore the NuGet packages:
    ```
    dotnet restore
    ```
3.  Run the application:
    ```
    dotnet run
    ```
    The simulation will start, and the WebSocket server will begin listening on port 5000.

## File Structure
* `Program.cs`: The entry point of the application. Initializes and starts the simulation manager and the WebSocket server.
* `SimManager.cs`: Manages the main simulation loop, NPC actions, and the world state. It handles saving and loading the simulation state.
* `SimServer.cs`: Implements the WebSocket server responsible for broadcasting the simulation state to clients.
* `NPC.cs`: Defines the behavior of NPCs, including their needs, status, and logic for fulfilling those needs.
* `WorldGrid.cs`: Handles the grid-based world, managing cells and buildings.
* `DataClasses.cs`: Contains all the C# data classes used for the simulation's data structures, such as `Cell`, `Need`, and `Building`.
* `config.json`: A JSON file for configuring simulation settings.
* `world_state.json`: A JSON file that stores the saved state of the simulation.
