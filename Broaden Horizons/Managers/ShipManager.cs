using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BroadenHorizons
{
    public class ShipManager
    {
        private readonly Planet[] _planets;
        private readonly List<Tech> _techs;
        private readonly MessageManager _messageManager;
        private readonly List<TurnAction> _turnActions;
        private readonly List<Ship> _ships = new List<Ship>();
        private int nextShipId = 0;

        // Save/Load Support
        public IReadOnlyList<Ship> Ships => _ships;             // Read-only access
        public int NextShipId => nextShipId;                    // Read-only
        public void SetShipsAndId(List<Ship> ships, int nextId)  // For restoring
        {
            _ships.Clear();
            if (ships != null) _ships.AddRange(ships);
            nextShipId = nextId;
        }

        public ShipManager(Planet[] planets, List<Tech> techs, MessageManager messageManager, List<TurnAction> turnActions)
        {
            _planets = planets;
            _techs = techs;
            _messageManager = messageManager;
            _turnActions = turnActions;
        }

        public void StartingShips(int planetId)
        {
            var startingShip = new Ship
            {
                Id = nextShipId++,
                Name = "Probe 1",
                TypeIndex = ShipTypeEnum.Probe.GetHashCode(),
                AssignedPlanet = planetId,
                Status = ShipStatus.Docked,
                CurrentPosition = new Vector2(_planets[planetId].XPos, _planets[planetId].YPos)
            };
            _ships.Add(startingShip);
            var startingShip2 = new Ship
            {
                Id = nextShipId++,
                Name = "Terraformer 2",
                TypeIndex = ShipTypeEnum.Terraformer.GetHashCode(),
                AssignedPlanet = planetId,
                Status = ShipStatus.Docked,
                CurrentPosition = new Vector2(_planets[planetId].XPos, _planets[planetId].YPos)
            };
            _ships.Add(startingShip2);
            /*var startingShip3 = new Ship
            {
                Id = nextShipId++,
                Name = "Freighter 1",
                TypeIndex = ShipTypeEnum.Freighter.GetHashCode(),
                AssignedPlanet = planetId,
                Status = ShipStatus.Docked,
                CurrentPosition = new Vector2(_planets[planetId].XPos, _planets[planetId].YPos)
            };
            _ships.Add(startingShip3);*/
        }

        public List<int> GetAvailableShipTypes()
        {
            var available = new List<int>();
            for (int i = 0; i < GameData.ShipTypes.Count; i++)
            {
                var st = GameData.ShipTypes[i];
                if (st.RequiredTech == -1 || _techs[st.RequiredTech].IsResearched)
                    available.Add(i);
            }
            return available;
        }

        public List<Ship> GetShipsOnPlanet(int planetId)
        {
            return _ships.FindAll(u => u.AssignedPlanet == planetId && u.Status != ShipStatus.Building);
        }

        public List<Ship> GetShipsInTransit()
        {
            return _ships.FindAll(u => u.Status == ShipStatus.InTransit);
        }

        public void StartBuildingShip(int planetId, int typeIndex, int turn)
        {
            var st = GameData.ShipTypes[typeIndex];
            _planets[planetId].Mat -= st.MatCost;

            var ship = new Ship
            {
                Id = nextShipId++,
                Name = $"{GameData.ShipTypes[typeIndex].Name} {nextShipId}",
                TypeIndex = typeIndex,
                AssignedPlanet = planetId,
                Status = ShipStatus.Building,
                FinalTurnAction = turn + st.TurnsToBuild
            };
            _ships.Add(ship);
        }

        public List<string> ProcessEndTurn(int currentTurn)
        {
            var messages = new List<string>();

            // Process completed builds
            var completedBuilds = _ships.Where(s => s.Status == ShipStatus.Building && s.FinalTurnAction == currentTurn).ToList();
            foreach (var ship in completedBuilds)
            {
                ship.Status = ShipStatus.Docked;
                ship.CurrentPosition = new Vector2(_planets[ship.AssignedPlanet].XPos, _planets[ship.AssignedPlanet].YPos);
                messages.Add($"{ship.Name} built on {_planets[ship.AssignedPlanet].Name}.");
            }

            // Update in-transit ships
            var inTransitShips = _ships.Where(s => s.Status == ShipStatus.InTransit).ToList();
            var DockedTerraformerShips = _ships.Where(s => s.TypeIndex == (int)ShipTypeEnum.Terraformer && s.Status == ShipStatus.Docked).ToList();
            var combinedShips = inTransitShips.Concat(DockedTerraformerShips).ToList();
            foreach (var ship in combinedShips)
            {
                if (GameData.ShipTypes[ship.TypeIndex].Type == ShipTypeEnum.Probe)
                {
                    var originPos = new Vector2(_planets[ship.AssignedPlanet].XPos, _planets[ship.AssignedPlanet].YPos);
                    var targetPos = new Vector2(_planets[ship.TargetPlanet].XPos, _planets[ship.TargetPlanet].YPos);
                    int midArrivalTurn = (ship.FinalTurnAction + ship.BeginTurnAction) / 2;
                    int totalTripTurns = ship.FinalTurnAction - ship.BeginTurnAction;

                    // Simpler interpolation: compute progress ratio based on remaining turns to return.
                    // When FinalTurnAction == currentTurn => progress = 1 (back at origin).
                    // When currentTurn == midArrivalTurn => progress ~ 0.5 (at target).
                    float progress;
                    if (currentTurn <= ship.BeginTurnAction) progress = 0f;
                    else if (currentTurn >= ship.FinalTurnAction) progress = 1f;
                    else progress = (currentTurn - ship.BeginTurnAction) / (float)totalTripTurns;

                    // Move along origin->target->origin path using progress in [0,1].
                    Vector2 currentPos;
                    if (progress <= 0.5f)
                    {
                        float p = progress / 0.5f; // 0..1 origin->target
                        currentPos = Vector2.Lerp(originPos, targetPos, p);
                    }
                    else
                    {
                        float p = (progress - 0.5f) / 0.5f; // 0..1 target->origin
                        currentPos = Vector2.Lerp(targetPos, originPos, p);
                    }
                    ship.CurrentPosition = currentPos;

                    // Mid-arrival handling (probe reaches the target)
                    if (currentTurn == midArrivalTurn)
                    {
                        if (_planets[ship.TargetPlanet].Status == PlanetStatus.ProbeEnRoute)
                        {
                            _planets[ship.TargetPlanet].Status = PlanetStatus.Explored;
                        }
                        messages.Add($"You have explored {_planets[ship.TargetPlanet].Name}.");
                    }

                    // Return docking handling (probe back at origin)
                    if (currentTurn == ship.FinalTurnAction)
                    {
                        // Finalize return: dock at origin
                        ship.Status = ShipStatus.Docked;
                        ship.CurrentPosition = originPos;
                        // Keep AssignedPlanet as origin; do not set AssignedPlanet = TargetPlanet
                        messages.Add($"{ship.Name} is back to {_planets[ship.AssignedPlanet].Name}.");
                        // Clear target as the mission is complete
                        ship.TargetPlanet = -1;
                    }
                }
                else if (GameData.ShipTypes[ship.TypeIndex].Type == ShipTypeEnum.Freighter)
                {
                    var originPos = new Vector2(_planets[ship.AssignedPlanet].XPos, _planets[ship.AssignedPlanet].YPos);
                    var targetPos = new Vector2(_planets[ship.TargetPlanet].XPos, _planets[ship.TargetPlanet].YPos);
                    int totalTripTurns = ship.FinalTurnAction - ship.BeginTurnAction;
                    float progress;
                    if (currentTurn <= ship.BeginTurnAction) progress = 0f;
                    else if (currentTurn >= ship.FinalTurnAction) progress = 1f;
                    else progress = (currentTurn - ship.BeginTurnAction) / (float)totalTripTurns;

                    Vector2 currentPos = Vector2.Lerp(originPos, targetPos, progress);
                    ship.CurrentPosition = currentPos;

                    if (currentTurn >= ship.FinalTurnAction)
                    {
                        var targetPlanet = _planets[ship.TargetPlanet];
                        targetPlanet.Food += ship.CargoFood;
                        targetPlanet.Mat += ship.CargoMat;

                        string msg = $"Freighter arrived at {targetPlanet.Name}. Delivered {ship.CargoFood} Food and {ship.CargoMat} Materials.";
                        messages.Add(msg);

                        ship.AssignedPlanet = ship.TargetPlanet;
                        ship.Status = ShipStatus.Docked;
                        ship.CurrentPosition = targetPos;
                        ship.TargetPlanet = -1;
                        ship.CargoFood = 0;
                        ship.CargoMat = 0;
                    }
                }
                else if (GameData.ShipTypes[ship.TypeIndex].Type == ShipTypeEnum.Terraformer)
                {
                    // Adjust temperature
                    var planet = _planets[ship.AssignedPlanet];

                    if (planet.Status == PlanetStatus.Owned || planet.Status == PlanetStatus.Explored)
                    {
                        int oldTemp = planet.Temperature;
                        int difTemp = 0;
                        if (planet.Temperature > GameData.TemperatureRanges[2].MaxTemp)
                        {
                            difTemp = -Constants.TERRAFORMER_TEMP_CHANGE;
                        }
                        else if (planet.Temperature < GameData.TemperatureRanges[2].MinTemp)
                        {
                            difTemp = Constants.TERRAFORMER_TEMP_CHANGE;
                        }
                        if (difTemp != 0)
                        {
                            planet.Temperature += difTemp;
                            messages.Add($"{ship.Name} adjusted temperature on {planet.Name} (From {oldTemp} to {planet.Temperature})");
                        }
                    }
                    //Update position
                    if (ship.Status == ShipStatus.InTransit)
                    {
                        var originPos = new Vector2(_planets[ship.AssignedPlanet].XPos, _planets[ship.AssignedPlanet].YPos);
                        var targetPos = new Vector2(_planets[ship.TargetPlanet].XPos, _planets[ship.TargetPlanet].YPos);
                        int totalTripTurns = ship.FinalTurnAction - ship.BeginTurnAction;
                        float progress;
                        if (currentTurn <= ship.BeginTurnAction) progress = 0f;
                        else if (currentTurn >= ship.FinalTurnAction) progress = 1f;
                        else progress = (currentTurn - ship.BeginTurnAction) / (float)totalTripTurns;

                        Vector2 currentPos = Vector2.Lerp(originPos, targetPos, progress);
                        ship.CurrentPosition = currentPos;

                        if (currentTurn >= ship.FinalTurnAction)
                        {
                            var targetPlanet = _planets[ship.TargetPlanet];
                            messages.Add($"Terraformer arrived at {targetPlanet.Name}");

                            ship.AssignedPlanet = ship.TargetPlanet;
                            ship.Status = ShipStatus.Docked;
                            ship.CurrentPosition = targetPos;
                            ship.TargetPlanet = -1;
                        }
                    }
                }
            }
            return messages;
        }

        public void ShowProbeLaunchMenu(Ship ship, int turn)
        {
            var planetData = new List<(int PlanetIndex, string name, float Distance, int TurnsNeeded, int EnergyNeeded, bool HasEnoughEnergy, string OptionString)>();

            for (int i = 0; i < _planets.Length; i++)
            {
                if (i != ship.AssignedPlanet && _planets[i].Status == PlanetStatus.Unexplored)
                {
                    float distance = Vector2.Distance(
                        new Vector2(_planets[ship.AssignedPlanet].XPos, _planets[ship.AssignedPlanet].YPos),
                        new Vector2(_planets[i].XPos, _planets[i].YPos)
                    );
                    int turnsNeeded = (int)Math.Ceiling(distance / GameData.ShipTypes[ship.TypeIndex].Speed);
                    turnsNeeded = Math.Max(1, turnsNeeded);
                    int energyNeeded = turnsNeeded * GameData.ShipTypes[ship.TypeIndex].EnergyperTurn;
                    string optionString = $"{_planets[i].Name} ({distance:0} units) Turns to come back: {turnsNeeded * 2} Energy: {energyNeeded * 2}";

                    planetData.Add((i, _planets[i].Name, distance, turnsNeeded, energyNeeded, energyNeeded * 2 <= _planets[ship.AssignedPlanet].Energy, optionString));
                }
            }

            planetData.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            var optionStrings = planetData.Select(data => data.OptionString).ToList();
            var targetPlanets = planetData.Select(data => data.PlanetIndex).ToList();
            var turns = planetData.Select(data => data.TurnsNeeded).ToList();
            var energies = planetData.Select(data => data.EnergyNeeded).ToList();

            _messageManager.ShowSelection($"Choose planet to explore", optionStrings, selectedIndex =>
            {
                if (selectedIndex >= 0)
                {
                    int selectedPlanetIndex = targetPlanets[selectedIndex];
                    LaunchData launchData = new LaunchData
                    {
                        Turn = turn,
                        TurnsNeeded = turns[selectedIndex],
                        EnergyNeeded = energies[selectedIndex],
                        TargetPlanet = selectedPlanetIndex
                    };
                    LaunchProbeShip(ship, launchData);
                }
            }, planetData.Select(data => data.HasEnoughEnergy).ToList());
        }

        public void ShowFreighterLaunchMenu(Ship ship, int turn)
        {
            var planetData = new List<(int PlanetIndex, string name, float Distance, int TurnsNeeded, int EnergyNeeded, bool HasEnoughEnergy, string OptionString)>();
            for (int i = 0; i < _planets.Length; i++)
            {
                if (i != ship.AssignedPlanet && _planets[i].Status == PlanetStatus.Owned)
                {
                    float distance = Vector2.Distance(
                        new Vector2(_planets[ship.AssignedPlanet].XPos, _planets[ship.AssignedPlanet].YPos),
                        new Vector2(_planets[i].XPos, _planets[i].YPos)
                    );
                    int turnsNeeded = (int)Math.Ceiling(distance / GameData.ShipTypes[ship.TypeIndex].Speed);
                    turnsNeeded = Math.Max(1, turnsNeeded);
                    int energyNeeded = turnsNeeded * GameData.ShipTypes[ship.TypeIndex].EnergyperTurn;
                    string optionString = $"{_planets[i].Name} ({distance:0} units) Turns to arrive: {turnsNeeded} Energy: {energyNeeded}";
                    planetData.Add((i, _planets[i].Name, distance, turnsNeeded, energyNeeded, energyNeeded <= _planets[ship.AssignedPlanet].Energy, optionString));
                }
            }

            if (planetData.Count == 0)
            {
                _messageManager.Show("No other owned planets to send cargo to", MessageType.Info);
                return;
            }

            planetData.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            var optionStrings = planetData.Select(data => data.OptionString).ToList();

            _messageManager.ShowSelection("Choose destination planet for Freighter", optionStrings, selectedIndex =>
            {
                if (selectedIndex >= 0)
                {
                    int targetPlanet = planetData[selectedIndex].PlanetIndex;
                    StartFreighterCargoSelection(ship, targetPlanet, turn, planetData[selectedIndex].EnergyNeeded, planetData[selectedIndex].TurnsNeeded);
                }
            }, planetData.Select(data => data.HasEnoughEnergy).ToList());
        }

        public void ShowTerraformerLaunchMenu(Ship ship, int turn)
        {
            var planetData = new List<(int PlanetIndex, string name, float Distance, int TurnsNeeded, int EnergyNeeded, bool HasEnoughEnergy, string OptionString)>();

            for (int i = 0; i < _planets.Length; i++)
            {
                if (i != ship.AssignedPlanet && (_planets[i].Status == PlanetStatus.Explored || _planets[i].Status == PlanetStatus.Owned))
                {
                    float distance = Vector2.Distance(
                        new Vector2(_planets[ship.AssignedPlanet].XPos, _planets[ship.AssignedPlanet].YPos),
                        new Vector2(_planets[i].XPos, _planets[i].YPos)
                    );
                    int turnsNeeded = (int)Math.Ceiling(distance / GameData.ShipTypes[ship.TypeIndex].Speed);
                    turnsNeeded = Math.Max(1, turnsNeeded);
                    int energyNeeded = turnsNeeded * GameData.ShipTypes[ship.TypeIndex].EnergyperTurn;
                    string optionString = $"{_planets[i].Name} ({distance:0} units) Temp: {_planets[i].Temperature} Turns: {turnsNeeded} Energy: {energyNeeded}";

                    planetData.Add((i, _planets[i].Name, distance, turnsNeeded, energyNeeded, energyNeeded <= _planets[ship.AssignedPlanet].Energy, optionString));
                }
            }

            planetData.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            var optionStrings = planetData.Select(data => data.OptionString).ToList();
            var targetPlanets = planetData.Select(data => data.PlanetIndex).ToList();
            var turns = planetData.Select(data => data.TurnsNeeded).ToList();
            var energies = planetData.Select(data => data.EnergyNeeded).ToList();

            _messageManager.ShowSelection($"Choose planet to travel to", optionStrings, selectedIndex =>
            {
                if (selectedIndex >= 0)
                {
                    int selectedPlanetIndex = targetPlanets[selectedIndex];
                    LaunchData launchData = new LaunchData
                    {
                        Turn = turn,
                        TurnsNeeded = turns[selectedIndex],
                        EnergyNeeded = energies[selectedIndex],
                        TargetPlanet = selectedPlanetIndex
                    };
                    LaunchTerraformerShip(ship, selectedPlanetIndex, turn, energies[selectedIndex], turns[selectedIndex]);
                }
            }, planetData.Select(data => data.HasEnoughEnergy).ToList());
        }

        private void StartFreighterCargoSelection(Ship ship, int targetPlanet, int turn, int energyCost, int turnsNeeded)
        {
            var originPlanet = _planets[ship.AssignedPlanet];
            int maxFood = originPlanet.Food;
            int maxMat = originPlanet.Mat;
            int capacity = GameData.ShipTypes[ship.TypeIndex].Capacity;

            _messageManager.ShowFreighterCargoSelection($"Select cargo for freighter to {_planets[targetPlanet].Name} (Capacity: {capacity})", maxFood, maxMat, capacity, (foodAmount, matAmount) =>
            {
                LaunchFreighter(ship, targetPlanet, foodAmount, matAmount, turn, energyCost, turnsNeeded);
            });
        }

        public void LaunchFreighter(Ship ship, int targetPlanet, int foodAmount, int matAmount, int turn, int energyCost, int turnsNeeded)
        {
            var origin = _planets[ship.AssignedPlanet];

            // Deduct resources
            origin.Food -= foodAmount;
            origin.Mat -= matAmount;
            origin.Energy -= energyCost;

            // Setup ship
            ship.BeginTurnAction = turn;
            ship.TargetPlanet = targetPlanet;
            ship.Status = ShipStatus.InTransit;
            ship.FinalTurnAction = turn + turnsNeeded;
            ship.CargoFood = foodAmount;
            ship.CargoMat = matAmount;

            _messageManager.Show($"Freighter launched to {_planets[targetPlanet].Name} with {foodAmount} Food and {matAmount} Materials.\n" +
                                $"It will arrive in {turnsNeeded} turns.", MessageType.Info);
        }

        public void LaunchTerraformerShip(Ship ship, int targetPlanet, int turn, int energyCost, int turnsNeeded)
        {
            var origin = _planets[ship.AssignedPlanet];

            // Setup ship
            ship.BeginTurnAction = turn;
            ship.TargetPlanet = targetPlanet;
            ship.Status = ShipStatus.InTransit;
            ship.FinalTurnAction = turn + turnsNeeded;
            origin.Energy -= energyCost;
            _messageManager.Show($"Terraformer launched to {_planets[targetPlanet].Name}.\n" +
                                $"It will arrive in {turnsNeeded} turns.", MessageType.Info);
        }

        /*public void ShowColonyLaunchMenu(Ship ship, int turn)
        {
            var planetData = new List<(int PlanetIndex, float Distance, int TurnsNeeded, int EnergyNeeded, string OptionString)>();

            for (int i = 0; i < _planets.Length; i++)
            {
                if (i != ship.AssignedPlanet && _planets[i].Status == PlanetStatus.Explored)
                {
                    float distance = Vector2.Distance(
                        new Vector2(_planets[ship.AssignedPlanet].XPos, _planets[ship.AssignedPlanet].YPos),
                        new Vector2(_planets[i].XPos, _planets[i].YPos)
                    );
                    int turnsNeeded = (int)Math.Ceiling(distance / GameData.ShipTypes[ship.TypeIndex].Speed);
                    turnsNeeded = Math.Max(1, turnsNeeded);
                    int energyNeeded = turnsNeeded * GameData.ShipTypes[ship.TypeIndex].EnergyperTurn;
                    string optionString = $"{_planets[i].Name} ({distance:0} units) Turns to arrive: {turnsNeeded} Energy: {energyNeeded * 2}";

                    planetData.Add((i, distance, turnsNeeded, energyNeeded, optionString));
                }
            }

            planetData.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            var optionStrings = planetData.Select(data => data.OptionString).ToList();
            var targetPlanets = planetData.Select(data => data.PlanetIndex).ToList();
            var turns = planetData.Select(data => data.TurnsNeeded).ToList();
            var energies = planetData.Select(data => data.EnergyNeeded).ToList();

            _messageManager.ShowSelection($"Choose planet to colonise:", optionStrings, selectedIndex =>
            {
                if (selectedIndex >= 0)
                {
                    int selectedPlanetIndex = targetPlanets[selectedIndex];
                    LaunchData launchData = new LaunchData
                    {
                        Turn = turn,
                        TurnsNeeded = turns[selectedIndex],
                        EnergyNeeded = energies[selectedIndex],
                        TargetPlanet = selectedPlanetIndex
                    };
                    LaunchShip(ship, launchData);
                }
            });
        }*/

        public void LaunchProbeShip(Ship ship, LaunchData data, List<int> loadUnits = null)
        {
            // Compute round-trip values (one-way from data)
            int oneWayTurns = Math.Max(1, data.TurnsNeeded);
            int oneWayEnergy = Math.Max(0, data.EnergyNeeded);
            int roundTripTurns = oneWayTurns * 2;
            int roundTripEnergy = oneWayEnergy * 2;

            // Check energy on origin planet (deduct from AssignedPlanet)
            if (_planets[ship.AssignedPlanet].Energy < roundTripEnergy)
            {
                _messageManager.Show("Not enough energy to launch!", MessageType.Info);
                return;
            }

            _planets[ship.AssignedPlanet].Energy -= roundTripEnergy;

            ship.BeginTurnAction = data.Turn;
            ship.TargetPlanet = data.TargetPlanet;
            ship.Status = ShipStatus.InTransit;
            ship.FinalTurnAction = data.Turn + roundTripTurns;

            if (_planets[ship.TargetPlanet].Status == PlanetStatus.Unexplored)
            {
                _planets[ship.TargetPlanet].Status = PlanetStatus.ProbeEnRoute;
            }
            _messageManager.Show($"Probe launched to {_planets[ship.TargetPlanet].Name}.\nIt will arrive there in {oneWayTurns} turns and come back at turn {ship.FinalTurnAction}", MessageType.Info);
        }
    }
}
