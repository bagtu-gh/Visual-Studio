using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BroadenHorizons
{
    public class ShipManager(List<TurnAction> turnAction, Planet[] planets, List<Tech> techs, MessageManager messageManager)
    {
        private readonly List<TurnAction> _turnAction = turnAction;
        private readonly Planet[] _planets = planets;
        private readonly List<Tech> _techs = techs;
        private readonly MessageManager _messageManager = messageManager;
        private readonly List<Ship> _ships = new List<Ship>();

        // Save/Load Support
        public IReadOnlyList<Ship> Ships => _ships;             // Read-only access
        public void SetShipsAndId(List<Ship> ships)  // For restoring
        {
            _ships.Clear();
            if (ships != null) _ships.AddRange(ships);
        }

        public void StartingShips(int planetId)
        {
            var startingShip = new Ship
            {
                ID = Constants.NEXT_ID++,
                Name = "Probe 1",
                TypeIndex = ShipTypeEnum.Probe,
                AssignedPlanet = planetId,
                Status = ShipStatus.Docked,
                CurrentPosition = new Vector2(_planets[planetId].XPos, _planets[planetId].YPos)
            };
            _ships.Add(startingShip);
            /*var startingShip2 = new Ship
            {
                Id = Constants.NEXT_ID++,
                Name = "Colony Ship 2",
                TypeIndex = ShipTypeEnum.ColonyShip,
                AssignedPlanet = planetId,
                Status = ShipStatus.Docked,
                CurrentPosition = new Vector2(_planets[planetId].XPos, _planets[planetId].YPos)
            };
            _ships.Add(startingShip2);
            /*var startingShip3 = new Ship
            {
                Id = Constants.NEXT_ID++,
                Name = "Freighter 1",
                TypeIndex = ShipTypeEnum.Freighter,
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
            return _ships.FindAll(u => u.AssignedPlanet == planetId);
        }

        public List<Ship> GetShipsInTransit()
        {
            return _ships.FindAll(u => u.Status == ShipStatus.InTransit);
        }

        public Ship GetShipById(int id)
        {
            return _ships.FirstOrDefault(s => s.ID == id);
        }

        public void StartBuildingShip(int planetId, ShipType st, int turn)
        {
            _planets[planetId].Mat -= st.MatCost;

            Ship ship = new Ship
            {
                ID = Constants.NEXT_ID++,
                Name = $"{st.Name} {Constants.NEXT_ID}",
                TypeIndex = st.Type,
                AssignedPlanet = planetId,
                Status = ShipStatus.UnderConstruction
            };
            _ships.Add(ship);
            _turnAction.Add(new TurnAction
            {
                ActionTurn = turn,
                TurnFinal = turn + st.TurnsToBuild,
                PlanetCode = planetId,
                ID = ship.ID,
                ActionType = ActionType.BuildingShip
            });
        }

        public List<string> ProcessEndTurn(int currentTurn)
        {
            var messages = new List<string>();

            foreach (var ship in GetActiveShips().ToList())
            {
                ProcessShip(ship, currentTurn, messages);
            }

            return messages;
        }

        private IEnumerable<Ship> GetActiveShips()
        {
            return _ships.Where(s =>
                s.Status == ShipStatus.InTransit ||
                (s.TypeIndex == ShipTypeEnum.Terraformer &&
                 s.Status == ShipStatus.Docked));
        }

        private void ProcessShip(Ship ship, int currentTurn, List<string> messages)
        {
            switch (ship.TypeIndex)
            {
                case ShipTypeEnum.Probe:
                    ProcessProbe(ship, currentTurn, messages);
                    break;

                case ShipTypeEnum.Freighter:
                    ProcessFreighter(ship, currentTurn, messages);
                    break;

                case ShipTypeEnum.ColonyShip:
                    ProcessColonyShip(ship, currentTurn, messages);
                    break;

                case ShipTypeEnum.Terraformer:
                    ProcessTerraformer(ship, currentTurn, messages);
                    break;
            }
        }

        private void ProcessProbe(Ship ship, int currentTurn, List<string> messages)
        {
            UpdateProbeMovement(ship, currentTurn);

            int midArrivalTurn = (ship.BeginTurnAction + ship.FinalTurnAction) / 2;

            // Reached target
            if (currentTurn == midArrivalTurn)
            {
                var targetPlanet = _planets[ship.TargetPlanet];

                if (targetPlanet.Status == PlanetStatus.ProbeEnRoute)
                {
                    targetPlanet.Status = PlanetStatus.Explored;
                }

                messages.Add($"You have explored {targetPlanet.Name}.");
            }

            // Returned home
            if (currentTurn == ship.FinalTurnAction)
            {
                var originPlanet = _planets[ship.AssignedPlanet];

                ship.Status = ShipStatus.Docked;
                ship.CurrentPosition = GetPlanetPosition(ship.AssignedPlanet);
                ship.TargetPlanet = -1;

                messages.Add($"{ship.Name} is back to {originPlanet.Name}.");
            }
        }

        private void UpdateProbeMovement(Ship ship, int currentTurn)
        {
            var origin = GetPlanetPosition(ship.AssignedPlanet);
            var target = GetPlanetPosition(ship.TargetPlanet);

            float progress = GetProgress(ship, currentTurn);

            ship.CurrentPosition =
                progress <= 0.5f
                    ? Vector2.Lerp(origin, target, progress * 2f)
                    : Vector2.Lerp(target, origin, (progress - 0.5f) * 2f);
        }

        private void ProcessFreighter(Ship ship, int currentTurn, List<string> messages)
        {
            UpdateLinearMovement(ship, currentTurn);

            if (currentTurn < ship.FinalTurnAction)
                return;

            var targetPlanet = _planets[ship.TargetPlanet];

            targetPlanet.Food += ship.CargoFood;
            targetPlanet.Mat += ship.CargoMat;

            messages.Add(
                $"Freighter arrived at {targetPlanet.Name}. " +
                $"Delivered {ship.CargoFood} Food and {ship.CargoMat} Materials.");

            DockShip(ship, ship.TargetPlanet);

            ship.CargoFood = 0;
            ship.CargoMat = 0;
        }

        private void ProcessColonyShip(Ship ship, int currentTurn, List<string> messages)
        {
            UpdateLinearMovement(ship, currentTurn);

            if (currentTurn < ship.FinalTurnAction)
                return;

            EstablishColony(ship, messages);
        }

        private void EstablishColony(Ship ship, List<string> messages)
        {
            var targetPlanet = _planets[ship.TargetPlanet];

            targetPlanet.Status = PlanetStatus.Owned;
            targetPlanet.Habitat[0] = 0; // City
            targetPlanet.HabitatPopulated[0] = true;
            targetPlanet.Improvements[0] = -1;
            targetPlanet.OccupiedByUnit[0] = -1;
            targetPlanet.Population = Constants.COLONY_POPULATION_COST;
            targetPlanet.Food = Constants.COLONY_FOOD_CARGO;
            targetPlanet.Mat = Constants.COLONY_MATERIAL_CARGO;
            targetPlanet.Energy = Constants.COLONY_STARTING_ENERGY;

            messages.Add(
                $"Colony established on {targetPlanet.Name}.\n" +
                $"{Constants.COLONY_POPULATION_COST} colonists founded the first city region\n" +
                $"with {Constants.COLONY_FOOD_CARGO} food and {Constants.COLONY_MATERIAL_CARGO} materials."
            );

            _ships.Remove(ship);
        }

        private void ProcessTerraformer(Ship ship, int currentTurn, List<string> messages)
        {
            if (ship.Status != ShipStatus.InTransit)
            {
                ProcessTerraforming(ship, messages);
                return;
            }

            UpdateLinearMovement(ship, currentTurn);

            if (currentTurn < ship.FinalTurnAction)
                return;

            var targetPlanet = _planets[ship.TargetPlanet];

            messages.Add($"Terraformer arrived at {targetPlanet.Name}");

            DockShip(ship, ship.TargetPlanet);
        }

        private void ProcessTerraforming(Ship ship, List<string> messages)
        {
            var planet = _planets[ship.AssignedPlanet];

            if (planet.Status != PlanetStatus.Owned &&
                planet.Status != PlanetStatus.Explored)
            {
                return;
            }

            int oldTemp = planet.Temperature;
            int delta = 0;
            var range = GameData.TemperatureRanges.FirstOrDefault(tr => tr.Name.Equals("Temperate", StringComparison.OrdinalIgnoreCase));

            if (planet.Temperature > range.MaxTemp)
            {
                delta = -Constants.TERRAFORMER_TEMP_CHANGE;
            }
            else if (planet.Temperature < range.MinTemp)
            {
                delta = Constants.TERRAFORMER_TEMP_CHANGE;
            }

            if (delta == 0)
                return;

            planet.Temperature += delta;

            messages.Add(
                $"{ship.Name} adjusted temperature on {planet.Name} " +
                $"(From {oldTemp} to {planet.Temperature})");
        }

        public Vector2 GetPlanetPosition(int planetId)
        {
            var planet = _planets[planetId];
            return new Vector2(planet.XPos, planet.YPos);
        }

        private static float GetProgress(Ship ship, int currentTurn)
        {
            if (currentTurn <= ship.BeginTurnAction)
                return 0f;

            if (currentTurn >= ship.FinalTurnAction)
                return 1f;

            int totalTurns = ship.FinalTurnAction - ship.BeginTurnAction;

            return (currentTurn - ship.BeginTurnAction) / (float)totalTurns;
        }

        private void UpdateLinearMovement(Ship ship, int currentTurn)
        {
            var origin = GetPlanetPosition(ship.AssignedPlanet);
            var target = GetPlanetPosition(ship.TargetPlanet);

            ship.CurrentPosition =
                Vector2.Lerp(origin, target, GetProgress(ship, currentTurn));
        }

        private void DockShip(Ship ship, int planetId)
        {
            ship.AssignedPlanet = planetId;
            ship.Status = ShipStatus.Docked;
            ship.CurrentPosition = GetPlanetPosition(planetId);
            ship.TargetPlanet = -1;
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
                    int turnsNeeded = (int)Math.Ceiling(distance / GameData.ShipTypes[ship.TypeIndex.GetHashCode()].Speed);
                    turnsNeeded = Math.Max(1, turnsNeeded);
                    int energyNeeded = turnsNeeded * GameData.ShipTypes[ship.TypeIndex.GetHashCode()].EnergyperTurn;
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
                    int turnsNeeded = (int)Math.Ceiling(distance / GameData.ShipTypes[ship.TypeIndex.GetHashCode()].Speed);
                    turnsNeeded = Math.Max(1, turnsNeeded);
                    int energyNeeded = turnsNeeded * GameData.ShipTypes[ship.TypeIndex.GetHashCode()].EnergyperTurn;
                    string optionString = $"{_planets[i].Name} ({distance:0} units) Turns to arrive: {turnsNeeded} Energy: {energyNeeded}";
                    planetData.Add((i, _planets[i].Name, distance, turnsNeeded, energyNeeded, energyNeeded <= _planets[ship.AssignedPlanet].Energy, optionString));
                }
            }

            if (planetData.Count == 0)
            {
                _messageManager.Show("No other owned planets to send cargo to", MessageType.Info, null, "INFO");
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
                    int turnsNeeded = (int)Math.Ceiling(distance / GameData.ShipTypes[ship.TypeIndex.GetHashCode()].Speed);
                    turnsNeeded = Math.Max(1, turnsNeeded);
                    int energyNeeded = turnsNeeded * GameData.ShipTypes[ship.TypeIndex.GetHashCode()].EnergyperTurn;
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
            int capacity = GameData.ShipTypes[ship.TypeIndex.GetHashCode()].Capacity;

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
                                $"It will arrive in {turnsNeeded} turns.", MessageType.Info, null, "INFO");
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
                                $"It will arrive in {turnsNeeded} turns.", MessageType.Info, null, "INFO");
        }

        public void ShowColonyLaunchMenu(Ship ship, int turn)
        {
            var origin = _planets[ship.AssignedPlanet];

            if (!CanPrepareColonyShip(ship, out string reason))
            {
                _messageManager.Show(reason, MessageType.Info, null, "WARNING", true);
                return;
            }

            var planetData = new List<(int PlanetIndex, string Name, float Distance, int TurnsNeeded, int EnergyNeeded, bool CanLaunch, string OptionString)>();

            for (int i = 0; i < _planets.Length; i++)
            {
                if (i != ship.AssignedPlanet && _planets[i].Status == PlanetStatus.Explored)
                {
                    float distance = Vector2.Distance(
                        new Vector2(origin.XPos, origin.YPos),
                        new Vector2(_planets[i].XPos, _planets[i].YPos)
                    );

                    int turnsNeeded = (int)Math.Ceiling(distance / GameData.ShipTypes[ship.TypeIndex.GetHashCode()].Speed);
                    turnsNeeded = Math.Max(1, turnsNeeded);
                    int energyNeeded = turnsNeeded * GameData.ShipTypes[ship.TypeIndex.GetHashCode()].EnergyperTurn;

                    bool canLaunch = CanLaunchColonyShip(ship, i, energyNeeded, out _);

                    string optionString =
                        $"{_planets[i].Name} ({distance:0} units) Turns: {turnsNeeded} " +
                        $"Energy: {energyNeeded} Pop: {Constants.COLONY_POPULATION_COST} " +
                        $"Food: {Constants.COLONY_FOOD_CARGO} Mat: {Constants.COLONY_MATERIAL_CARGO}";

                    planetData.Add((i, _planets[i].Name, distance, turnsNeeded, energyNeeded, canLaunch, optionString));
                }
            }

            if (planetData.Count == 0)
            {
                _messageManager.Show("No explored planets are available for colonization.", MessageType.Info, null, "WARNING");
                return;
            }

            if (!planetData.Any(data => data.CanLaunch))
            {
                _messageManager.Show("Not enough energy to reach any explored planet with this colony ship.", MessageType.Info, null, "WARNING");
                return;
            }

            planetData.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            var optionStrings = planetData.Select(data => data.OptionString).ToList();

            _messageManager.ShowSelection("Choose planet to colonize", optionStrings, selectedIndex =>
            {
                if (selectedIndex >= 0)
                {
                    var selected = planetData[selectedIndex];
                    LaunchColonyShip(ship, selected.PlanetIndex, turn, selected.EnergyNeeded, selected.TurnsNeeded);
                }
            }, planetData.Select(data => data.CanLaunch).ToList());
        }

        public void LaunchColonyShip(Ship ship, int targetPlanet, int turn, int energyCost, int turnsNeeded)
        {
            var origin = _planets[ship.AssignedPlanet];

            if (!CanLaunchColonyShip(ship, targetPlanet, energyCost, out string reason))
            {
                _messageManager.Show(reason, MessageType.Info, null, "WARNING", true);
                return;
            }

            origin.Population -= Constants.COLONY_POPULATION_COST;
            origin.Food -= Constants.COLONY_FOOD_CARGO;
            origin.Mat -= Constants.COLONY_MATERIAL_CARGO;
            origin.Energy -= energyCost;

            ship.BeginTurnAction = turn;
            ship.TargetPlanet = targetPlanet;
            ship.Status = ShipStatus.InTransit;
            ship.FinalTurnAction = turn + Math.Max(1, turnsNeeded);

            _messageManager.Show(
                $"Colony ship launched to {_planets[targetPlanet].Name}.\n" +
                $"It carries {Constants.COLONY_POPULATION_COST} colonists, {Constants.COLONY_FOOD_CARGO} food, " +
                $"{Constants.COLONY_MATERIAL_CARGO} materials and will arrive in {turnsNeeded} turns.",
                MessageType.Info, null, "INFO");
        }

        private bool CanLaunchColonyShip(Ship ship, int targetPlanet, int energyCost, out string reason)
        {
            reason = "";

            if (targetPlanet < 0 || targetPlanet >= _planets.Length)
            {
                reason = "Invalid colony destination.";
                return false;
            }

            if (!CanPrepareColonyShip(ship, out reason))
                return false;

            if (_planets[targetPlanet].Status != PlanetStatus.Explored)
            {
                reason = "Only explored, unowned planets can be colonized.";
                return false;
            }

            var origin = _planets[ship.AssignedPlanet];

            if (origin.Energy < energyCost)
            {
                reason = "Not enough energy to launch!";
                return false;
            }

            return true;
        }

        private bool CanPrepareColonyShip(Ship ship, out string reason)
        {
            reason = "";

            if (ship.Status != ShipStatus.Docked)
            {
                reason = "The colony ship must be docked before launch.";
                return false;
            }

            var origin = _planets[ship.AssignedPlanet];
            int freePopulation = Functions.GetPlanetPopulation(origin, "Unassigned");

            if (freePopulation < Constants.COLONY_POPULATION_COST || origin.Food < Constants.COLONY_FOOD_CARGO || origin.Mat < Constants.COLONY_MATERIAL_CARGO)
            {
                var populationStatus = freePopulation < Constants.COLONY_POPULATION_COST ? "Insufficient" : "Sufficient";
                var foodStatus = origin.Food < Constants.COLONY_FOOD_CARGO ? "Insufficient" : "Sufficient";
                var materialsStatus = origin.Mat < Constants.COLONY_MATERIAL_CARGO ? "Insufficient" : "Sufficient";

                reason = $"Not enough resources.\nColonization requires {Constants.COLONY_POPULATION_COST} unassigned colonists ({populationStatus}),\n{Constants.COLONY_FOOD_CARGO} food ({foodStatus}), and {Constants.COLONY_MATERIAL_CARGO} materials ({materialsStatus}).";
                return false;
            }

            return true;
        }

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
                _messageManager.Show("Not enough energy to launch!", MessageType.Info, null, "WARNING", true);
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
            _messageManager.Show($"Probe launched to {_planets[ship.TargetPlanet].Name}.\nIt will arrive there in {oneWayTurns} turns and come back at turn {ship.FinalTurnAction}", MessageType.Info, null, "INFO");
        }

        public void HandleShipClicked(Ship ship, List<TurnAction> turnActions, Planet[] planets, MessageManager messageManager)
        {
            if (ship.Status == ShipStatus.Docked)
            {
                switch (ship.TypeIndex)
                {
                    case ShipTypeEnum.Probe:
                        ShowProbeLaunchMenu(ship, Constants.TURN);
                        break;
                    case ShipTypeEnum.ColonyShip:
                        ShowColonyLaunchMenu(ship, Constants.TURN);
                        break;
                    case ShipTypeEnum.Freighter:
                        ShowFreighterLaunchMenu(ship, Constants.TURN);
                        break;
                    case ShipTypeEnum.Terraformer:
                        ShowTerraformerLaunchMenu(ship, Constants.TURN);
                        break;
                }
            }
            else if (ship.Status == ShipStatus.InTransit)
            {
                messageManager.Show($"Your {ship.Name} is travelling to {planets[ship.TargetPlanet].Name}", MessageType.Info, null, "INFO");
            }
            else if (ship.Status == ShipStatus.UnderConstruction)
            {
                var ta = turnActions.FirstOrDefault(t => t.ID == ship.ID);
                messageManager.Show($"Your {ship.Name} is being built. They will be available at turn {ta.TurnFinal}", MessageType.Info, null, "INFO");
            }
        }
    }
}