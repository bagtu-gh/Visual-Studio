using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BroadenHorizons
{
    public class EndTurnManager(BH game)
    {
        private readonly BH _game = game;
        private bool _techResearchedThisTurn;

        public void EndTurn(GameTime gameTime)
        {
            _game.Turn++;

            ResetRecruitmentFlags();
            ProcessPlanetProduction();

            List<string> summary = new List<string>();

            ProcessResearch(summary);
            ProcessTurnActions(summary);
            ProcessShips(summary);
            CheckResearchWarnings(summary);

            if (Constants.EVENTS_ON)
                _game._eventManager.TryTriggerEvent(summary);

            ProcessShortages(summary);
            CheckRegionsPopulation(summary);

            string summaryText = BuildSummaryText(summary);

            _game._messageManager.Show(summaryText, MessageType.Info);

            LogTurnToFile(summaryText);
        }

        private void ResetRecruitmentFlags()
        {
            for (int i = 0; i < Constants.NUM_PLANETS; i++)
            {
                _game.hasRecruitedThisTurn[i] = false;
            }
        }

        private void ProcessPlanetProduction()
        {
            for (int p = 0; p < Constants.NUM_PLANETS; p++)
            {
                var production = _game._productionManager.CalculateProduction(p);

                _game.Planets[p].Food += production.Food;
                _game.Planets[p].Mat += production.Materials;
                _game.Planets[p].Energy += production.Energy;

                _game.Planets[p].Population += int.Parse(
                    Functions.GetPopModifier(
                        _game.Planets[p],
                        int.Parse(_game._productionManager.CalculateProductionTurn(p, "Food"))
                    )
                );
            }
        }

        private void ProcessResearch(List<string> summary)
        {
            int scienceProduced =
                int.Parse(_game._productionManager.CalculateProductionTurn(-1, "Science"));

            _game._techManager.ProcessTurnResearch(
                scienceProduced,
                summary,
                out var techResearchedThisTurn
            );

            _techResearchedThisTurn = techResearchedThisTurn;
        }

        private void ProcessTurnActions(List<string> summary)
        {
            for (int i = _game.TurnActions.Count - 1; i >= 0; i--)
            {
                var ta = _game.TurnActions[i];

                var selectedUnit = _game._unitManager.GetUnitById(ta.UnitID);

                if (_game.Turn < ta.TurnFinal)
                    continue;

                switch (ta.UnitActionType)
                {
                    case UnitActionType.Building:
                        HandleBuildingAction(ta, summary);
                        break;

                    case UnitActionType.Recruiting:
                        HandleRecruitingAction(ta, selectedUnit, summary);
                        break;

                    case UnitActionType.MovingOrExploring:
                        HandleMovementAction(ta, selectedUnit, summary);
                        break;
                }

                selectedUnit.Status = UnitStatus.Idle;
                _game.TurnActions.RemoveAt(i);
            }
        }

        private void HandleBuildingAction(TurnAction ta, List<string> summary)
        {
            _game.Planets[ta.PlanetCode].Improvements[ta.TargetReg] =
                ta.ImprovementIndex;

            var improvement = _game.PlanetImprovements[ta.ImprovementIndex];

            string planetName = _game.Planets[ta.PlanetCode].Name;

            summary.Add(
                $"Your builder has finished {improvement.Name} at {planetName},\n" +
                $"it will produce {improvement.FoodProd} food, " +
                $"{improvement.MatProd} materials, " +
                $"{improvement.SciProd} science,\n" +
                $"a {improvement.AllowedUnit} can occupy the building to increase production."
            );
        }

        private void HandleRecruitingAction(
            TurnAction ta,
            Unit selectedUnit,
            List<string> summary)
        {
            string unitName = selectedUnit.Name;
            string planetName = _game.Planets[ta.PlanetCode].Name;

            summary.Add(
                $"A new unit of {unitName} has been recruited\n" +
                $"at {planetName} and is now ready for action!"
            );
        }

        private void HandleMovementAction(
            TurnAction ta,
            Unit selectedUnit,
            List<string> summary)
        {
            int unitCode = selectedUnit.TypeIndex;

            string unitName = _game.UnitTypes[unitCode].Name;
            string planetName = _game.Planets[ta.PlanetCode].Name;

            int targetRegion = ta.TargetReg;

            if (unitCode == (int)UnitTypeEnum.Explorer &&
                _game.Planets[ta.PlanetCode].Habitat[targetRegion] < 0)
            {
                HandleExplorerDiscovery(ta, targetRegion, planetName, summary);
            }
            else
            {
                int habitatIndex =
                    _game.Planets[ta.PlanetCode].Habitat[targetRegion];

                var habitat = _game.HabitatTypes[habitatIndex];

                summary.Add(
                    $"{unitName} has arrived at {habitat.Name} " +
                    $"(Region {targetRegion}) on {planetName}!\n" +
                    $"They are now ready to work or move again."
                );
            }
        }

        private void HandleExplorerDiscovery(
            TurnAction ta,
            int targetRegion,
            string planetName,
            List<string> summary)
        {
            var planet = _game.Planets[ta.PlanetCode];

            int hab = planet.Habitat[targetRegion];

            planet.Habitat[targetRegion] = Math.Abs(hab);

            var habitat = _game.HabitatTypes[Math.Abs(hab)];

            if (Functions.GetPlanetPopulation(planet, "Unassigned")
                >= habitat.PopNeeded)
            {
                summary.Add($"A new habitat {habitat.Name} has been discovered at {planetName},\nit will yield {habitat.FoodProd} food, {habitat.MatProd} materials, and {habitat.SciProd} science.\nIt has been automatically populated with {habitat.PopNeeded} colonists\nYour explorer is now free to explore more!");
                planet.HabitatPopulated[targetRegion] = true;
            }
            else
            {
                summary.Add($"A new habitat {habitat.Name} has been discovered at {planetName},\nit would yield {habitat.FoodProd} food, {habitat.MatProd} materials, and {habitat.SciProd} science\nif populated with {habitat.PopNeeded} colonists that are not currently available.\nYour explorer is now free to explore more!");
            }

            int regionBonusIndex =
                planet.RegionBonusRegions[targetRegion];

            if (regionBonusIndex >= 0)
            {
                var regionBonus =
                    _game._regionBonusManager.RegionBonusTypes[regionBonusIndex];

                summary.Add(
                    $"A {regionBonus.Name} was discovered in the {habitat.Name} at {planetName}!" +
                    $"\nIt will provide +{regionBonus.BaseBonus} {regionBonus.BonusType} to the region's production."
                );
            }
        }

        private void ProcessShips(List<string> summary)
        {
            var shipMessages =
                _game._shipManager.ProcessEndTurn(_game.Turn);

            foreach (var msg in shipMessages)
            {
                summary.Add(msg);
            }
        }

        private void CheckRegionsPopulation(List<string> summary)
        {
            foreach (var planet in _game.Planets)
            {
                while (planet.Population < Functions.GetPlanetPopulation(planet, "Assigned"))
                {
                    int highestPopulatedIndex = planet.HabitatPopulated
                        .Select((populated, index) => new { populated, index })
                        .Where(x => x.populated)
                        .Select(x => x.index)
                        .DefaultIfEmpty(-1) // in case no true values are found
                        .Max();
                    planet.HabitatPopulated[highestPopulatedIndex] = false;
                    summary.Add($"Warning! Region {highestPopulatedIndex} ({GameData.HabitatTypes[planet.Habitat[highestPopulatedIndex]].Name}) at {planet.Name} is now unpopulated\n due to population loss.") ;
                }
            }
        }

        private void ProcessShortages(List<string> summary)
        {
            foreach (var planet in _game.Planets)
            {
                if (planet.Status != PlanetStatus.Owned)
                    continue;
                    
                if (planet.Food < 0)
                {
                    int foodShortage = -planet.Food;
                    int populationLoss = (int)Math.Ceiling(foodShortage / 10.0);
                    planet.Population = Math.Max(0, planet.Population - populationLoss);

                    summary.Add($"Food shortage at {planet.Name}! Population decreased by {populationLoss}.");
                }

                if (planet.Energy < 0)
                {
                    int energyShortage = -planet.Energy;
                    int productionLoss = (int)Math.Ceiling(energyShortage / 10.0);
                    planet.Mat = Math.Max(0, planet.Mat - productionLoss);

                    summary.Add($"Energy shortage at {planet.Name}! Material production decreased by {productionLoss}.");
                }
            }
        }

        private void CheckResearchWarnings(List<string> summary)
        {
            if (_game._techManager?.CurrentResearch == -1 &&
                !_techResearchedThisTurn &&
                (_game._techManager?.HasAvailableTechs() ?? false))
            {
                summary.Add(
                    "Warning! No research in progress.\n" +
                    "Visit the Tech Tree to start a new research project."
                );
            }
        }

        private string BuildSummaryText(List<string> summary)
        {
            if (summary.Count > 0)
            {
                return $"Turn {_game.Turn - 1} Summary:\n\n" +
                       string.Join("\n\n", summary);
            }

            return $"Turn {_game.Turn - 1} completed. No actions finished this turn.";
        }

        private void LogTurnToFile(string summaryText)
        {
            try
            {
                string filePath = "TurnLog.txt";
                string logEntry = $"{summaryText}\n\n";

                if (_game.Turn == 2)
                {
                    System.IO.File.WriteAllText(filePath, logEntry);
                }
                else
                {
                    string existingContent =
                        System.IO.File.ReadAllText(filePath);

                    string newContent = logEntry + existingContent;

                    System.IO.File.WriteAllText(filePath, newContent);
                }
            }
            catch
            {
            }
        }

        public void ShowTurnLog()
        {
            try
            {
                string filePath = "TurnLog.txt";

                if (System.IO.File.Exists(filePath))
                {
                    string content =
                        System.IO.File.ReadAllText(filePath);

                    _game._messageManager.Show(
                        $"=== TURN LOG ===\n\n{content}",
                        MessageType.Help
                    );
                }
                else
                {
                    _game._messageManager.Show(
                        "No turn log yet.\nPlay a few turns first!",
                        MessageType.Info
                    );
                }
            }
            catch
            {
                _game._messageManager.Show(
                    "Could not read turn log.",
                    MessageType.Info
                );
            }
        }
    }
}