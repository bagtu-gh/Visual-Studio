using System.Collections.Generic;
using System.Linq;

namespace BroadenHorizons
{
    public class UnitManager(BH game, List<UnitType> unitTypes, MessageManager messageManager)
    {
        private readonly BH _game = game;
        public Planet[] _planets => _game.Planets;
        private readonly List<UnitType> _unitTypes = unitTypes;
        private readonly MessageManager _messageManager = messageManager;
        public List<Unit> _units = new List<Unit>();
        private int nextUnitId = 0;

        // Save/Load Support
        public IReadOnlyList<Unit> Units => _units;
        public int NextUnitId => nextUnitId;
        public void SetUnitsAndId(List<Unit> units, int nextId)
        {
            _units.Clear();
            if (units != null) _units.AddRange(units);
            nextUnitId = nextId;
        }

        public Unit GetUnitById(int id)
        {
            return _units.FirstOrDefault(u => u.ID == id);
        }

        public List<Unit> GetUnitsOnPlanet(int planetId)
        {
            return _units.FindAll(u => u.Planet == planetId);
        }

        public static List<int> GetAvailableUnitTypes(List<Tech> techs)
        {
            var result = new List<int>();
            for (int i = 0; i < GameData.UnitTypes.Count; i++)
            {
                var ut = GameData.UnitTypes[i];
                if (ut.RequiredTech == -1 || techs[ut.RequiredTech].IsResearched)
                {
                    result.Add(i);
                }
            }
            return result;
        }
        
        public void StartingUnits(int PlanetId)
        {
            // Add a starting explorer to the home planet
            var startingUnit = new Unit
            {
                ID = nextUnitId++,
                Name = "Explorer",
                TypeIndex = UnitTypeEnum.Explorer.GetHashCode(),
                Planet = PlanetId,
                Region = 0,
                Status = UnitStatus.Idle
            };
            _units.Add(startingUnit);
            var startingUnit2 = new Unit
            {
                ID = nextUnitId++,
                Name = "Builder",
                TypeIndex = UnitTypeEnum.Builder.GetHashCode(),
                Planet = PlanetId,
                Region = 0,
                Status = UnitStatus.Idle
            };
            _units.Add(startingUnit2);
        }

        public static List<int> GetAvailableDestinations(Unit unit, Planet planet, int[] neighbors, List<HabitatType> habitatTypes, List<PlanetImprovement> improvements)
        {
            var destinations = new List<int>();
            int currentReg = unit.Region;
            int unitCode = unit.TypeIndex;

            for (int j = 0; j < 6; j++)
            {
                int neigh = neighbors[currentReg * 6 + j];
                if (neigh >= 0 && neigh <= Constants.MAX_PLANET_DIMENS)
                {
                    bool add = false;
                    if (planet.Habitat[neigh] >= 0)
                        add = true;
                    if (unitCode == 0 && planet.Habitat[neigh] < 0)
                        add = true;
                    if (add)
                        destinations.Add(neigh);
                }
            }
            int hab = planet.Habitat[currentReg];
            int imp = planet.Improvements[currentReg];
            if (unitCode == 4 && hab >= 0 && imp == -1)// && habitatTypes[hab].Name != "City")
            {
                var available = improvements
                    .Select((pi, idx) => idx)
                    .Where(idx => improvements[idx].AllowedHabitat == habitatTypes[hab].Name)
                    .ToList();

                if (available.Count > 0)
                    destinations.Add(currentReg);
            }
            if (imp >= 0 && planet.OccupiedByUnit[currentReg] == -1 &&
                habitatTypes[hab].Name == improvements[imp].AllowedHabitat)
            {
                destinations.Add(currentReg);
            }
            return destinations;
        }

        public void RecruitUnit(int planetId, int unitTypeIndex, int currentTurn)
        {
            var unitType = _unitTypes[unitTypeIndex];
            if (_planets[planetId].Food < unitType.FoodCost || _planets[planetId].Mat < unitType.MatCost)
            {
                _messageManager.Show("Not enough resources!", MessageType.Info);
                return;
            }

            _planets[planetId].Food -= unitType.FoodCost;
            _planets[planetId].Mat -= unitType.MatCost;

            var unit = new Unit
            {
                ID = nextUnitId++,
                Name = unitType.Name,
                TypeIndex = unitTypeIndex,
                Planet = planetId,
                Region = 0,
                Status = UnitStatus.Busy
            };

            _units.Add(unit);

            _game.TurnActions.Add(new TurnAction
            {
                ActionTurn = currentTurn,
                TurnFinal = currentTurn + unitType.RecruitTurns,
                PlanetCode = planetId,
                UnitID = unit.ID,
                UnitActionType = UnitActionType.Recruiting
            });
        }

        public void HandleUnitClicked(
            Unit unit,
            int currentPlanetIndex,
            List<TurnAction> turnActions,
            List<UnitType> unitTypes,
            List<PlanetImprovement> improvements,
            Planet planet,
            int[] neighbors,
            List<HabitatType> habitatTypes,
            ref int selectedUnitIndex,
            ref List<int> possibleDestinations,
            MessageManager messageManager)
        {
            if (unit.Status == UnitStatus.Busy)
            {
                var ta = turnActions.FirstOrDefault(t => t.PlanetCode == currentPlanetIndex && t.UnitID == unit.ID);
                if (ta != null)
                {
                    int availableTurn = ta.TurnFinal;
                    string actionDesc = ta.UnitActionType switch
                    {
                        UnitActionType.Building => $"building {improvements[ta.ImprovementIndex].Name}",
                        UnitActionType.Recruiting => "being recruited",
                        UnitActionType.MovingOrExploring => unit.TypeIndex == (int)UnitTypeEnum.Explorer && planet.Habitat[ta.TargetReg] < 0
                            ? "surveying a new region"
                            : "moving to new region",
                        _ => "busy"
                    };

                    messageManager.Show(
                        $"{unitTypes[unit.TypeIndex].Name} are {actionDesc}\nThey will be available at turn {availableTurn}",
                        MessageType.Info);
                }
                else
                {
                    messageManager.Show("Unit busy", MessageType.Info);
                }
            }
            else // Free unit
            {
                selectedUnitIndex = unit.ID;
                possibleDestinations = GetAvailableDestinations(unit, planet, neighbors, habitatTypes, improvements);

                if (possibleDestinations.Count == 0)
                {
                    messageManager.Show("No potential actions", MessageType.Info);
                    selectedUnitIndex = -1;
                }
            }
        }
    }
}