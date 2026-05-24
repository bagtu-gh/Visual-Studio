using System;
using System.Collections.Generic;
using System.Linq;

namespace BroadenHorizons
{
    public class ProductionManager
    {
        private Planet[] _planets;
        private List<HabitatType> _habitatTypes;
        private List<UnitType> _unitTypes;
        private List<PlanetImprovement> _planetImprovements;
        private RegionBonusManager _regionBonusManager;
        private UnitManager _unitManager;
        private ShipManager _shipManager;
        private List<Tech> _techs;

        public ProductionManager(
            Planet[] planets,
            List<HabitatType> habitatTypes,
            List<UnitType> unitTypes,
            List<PlanetImprovement> planetImprovements,
            RegionBonusManager regionBonusManager,
            UnitManager unitManager,
            ShipManager shipManager,
            List<Tech> techs)
        {
            _planets = planets;
            _habitatTypes = habitatTypes;
            _unitTypes = unitTypes;
            _planetImprovements = planetImprovements;
            _regionBonusManager = regionBonusManager;
            _unitManager = unitManager;
            _shipManager = shipManager;
            _techs = techs;
        }

        /// <summary>
        /// Calculates total production for a planet, including maintenance costs.
        /// </summary>
        public ProductionBreakdown CalculateProduction(int planetIndex)
        {
            var result = AggregatePlanetProduction(planetIndex);

            // Initialize breakdown list if null (safety)
            if (result.BreakdownText == null) result.BreakdownText = new List<string>();

            // Unit maintenance: active units consume maintenance
            foreach (var unit in _unitManager.GetUnitsOnPlanet(planetIndex))
            {
                if (unit.Status != UnitStatus.InImprovement) // only active units count toward maintenance here
                {
                    int unitType = unit.TypeIndex;
                    int foodMaint = _unitTypes[unitType].FoodMaint;
                    int matMaint = _unitTypes[unitType].MatMaint;

                    result.Food -= foodMaint;
                    result.Materials -= matMaint;

                    if (foodMaint != 0) result.BreakdownText.Add($"{_unitTypes[unitType].Name} Maintenance: -{foodMaint} Food");
                    if (matMaint != 0) result.BreakdownText.Add($"{_unitTypes[unitType].Name} Maintenance: -{matMaint} Materials");
                }
            }

            // Ship maintenance: maintenance is applied to the planet the ship is assigned to
            foreach (var ship in _shipManager.GetShipsOnPlanet(planetIndex))
            {
                var st = GameData.ShipTypes[ship.TypeIndex];
                int maint = st.MaintCost;
                if (maint != 0)
                {
                    result.Materials -= maint;
                    string shipLabel = !string.IsNullOrEmpty(ship.Name) ? ship.Name : st.Name;
                    result.BreakdownText.Add($"{shipLabel} Maintenance: -{maint} Materials");
                }
            }

            return result;
        }

        /// <summary>
        /// Aggregates production from habitats and improvements for a planet.
        /// </summary>
        private ProductionBreakdown AggregatePlanetProduction(int planetIndex)
        {
            var result = new ProductionBreakdown();

            // Regions and improvements
            for (int t = 0; t <= Constants.MAX_PLANET_DIMENS; t++)
            {
                int habIndex = _planets[planetIndex].Habitat[t];

                if (habIndex < 0 || !_planets[planetIndex].HabitatPopulated[t])
                    continue;

                // Base habitat production
                int hab = Math.Abs(habIndex);
                var habitat = _habitatTypes[hab];
                int baseFood = habitat.FoodProd;
                int baseMat = habitat.MatProd;
                int baseSci = habitat.SciProd;
                int baseEng = habitat.EnergyProd;

                int modFood = 0;
                int modMat = 0;
                int modSci = 0;
                int modEng = 0;

                // Tech bonuses that apply to habitats
                foreach (var tech in _techs)
                {
                    if (!tech.IsResearched) continue;

                    foreach (var bonus in tech.BonusUnlocks)
                    {
                        if (bonus.Habitat == habitat.Name)
                        {
                            modFood += bonus.FoodProd;
                            modMat += bonus.MatProd;
                            modSci += bonus.SciProd;
                            modEng += bonus.EnergyProd;
                        }
                    }
                }

                // Improvements and occupied units on top of habitats
                int impIndex = _planets[planetIndex].Improvements[t];
                if (impIndex >= 0 && _planets[planetIndex].HabitatPopulated[t])
                {
                    var improvement = _planetImprovements[impIndex];
                    modFood += improvement.FoodProd;
                    modMat += improvement.MatProd;
                    modSci += improvement.SciProd;
                    modEng += improvement.EnergyProd;

                    int occupiedUnitId = _planets[planetIndex].OccupiedByUnit[t];
                    if (occupiedUnitId >= 0)
                    {
                        var occupiedUnit = _unitManager.GetUnitById(occupiedUnitId);
                        if (occupiedUnit != null)
                        {
                            int unitTypeIndex = occupiedUnit.TypeIndex;
                            modFood += _unitTypes[unitTypeIndex].ExtraFoodProd;
                            modMat += _unitTypes[unitTypeIndex].ExtraMatProd;
                            modSci += _unitTypes[unitTypeIndex].ExtraSciProd;
                        }
                    }
                }

                result.Food += baseFood + modFood;
                result.Materials += baseMat + modMat;
                result.Science += baseSci + modSci;
                result.Energy += baseEng + modEng;

                var baseFoodLine = FormatResourceLine("Food", habitat.Name, t, baseFood, modFood);
                if (baseFoodLine != null) result.BreakdownText.Add(baseFoodLine);

                var baseMatLine = FormatResourceLine("Materials", habitat.Name, t, baseMat, modMat);
                if (baseMatLine != null) result.BreakdownText.Add(baseMatLine);

                var baseSciLine = FormatResourceLine("Science", habitat.Name, t, baseSci, modSci);
                if (baseSciLine != null) result.BreakdownText.Add(baseSciLine);

                var baseEngLine = FormatResourceLine("Energy", habitat.Name, t, baseEng, modEng);
                if (baseEngLine != null) result.BreakdownText.Add(baseEngLine);
            }

            // Apply non-maintenance region bonuses that are implemented by RegionBonusManager
            _regionBonusManager.CalculateRegionBonuses(_planets[planetIndex], ref result);

            return result;
        }

        // Helper function to format the resource line
        private static string FormatResourceLine(string resourceName, string habitatName, int region, int baseAmount, int modAmount)
        {
            if (baseAmount == 0 && modAmount == 0)
                return null;

            string baseStr = Functions.GetSignedValue(baseAmount);
            string modStr = Functions.GetSignedValue(modAmount);

            if (baseAmount != 0 && modAmount != 0)
                return $"{habitatName} (Region {region}): {baseStr} ({modStr}) {resourceName}";
            else if (baseAmount != 0)
                return $"{habitatName} (Region {region}): {baseStr} {resourceName}";
            else // only modAmount != 0
                return $"{habitatName} (Region {region}): {modStr} {resourceName}";
        }

        /// <summary>
        /// Calculates production for a specific resource type for one planet or globally.
        /// planetIndex = -1 calculates global production.
        /// </summary>
        public string CalculateProductionTurn(int planetIndex, string productionType)
        {
            if (planetIndex == -1)
            {
                int total = 0;
                for (int i = 0; i < Constants.NUM_PLANETS; i++)
                {
                    if (_planets[i].Status == PlanetStatus.Owned)
                    {
                        var rb = CalculateProduction(i);
                        switch (productionType)
                        {
                            case "Food": total += rb.Food; break;
                            case "Materials": total += rb.Materials; break;
                            case "Science": total += rb.Science; break;
                            case "Energy": total += rb.Energy; break;
                        }
                    }
                }
                return Functions.GetSignedValue(total);
            }
            else
            {
                var rb = CalculateProduction(planetIndex);
                switch (productionType)
                {
                    case "Food": return Functions.GetSignedValue(rb.Food);
                    case "Materials": return Functions.GetSignedValue(rb.Materials);
                    case "Science": return Functions.GetSignedValue(rb.Science);
                    case "Energy": return Functions.GetSignedValue(rb.Energy);
                    default: return "0";
                }
            }
        }

        /// <summary>
        /// Gets a tooltip showing production breakdown for a specific resource type.
        /// </summary>
        public string GetProductionTooltip(int planetIndex, string productionType)
        {
            var production = CalculateProduction(planetIndex);
            int total = productionType switch
            {
                "Food" => production.Food,
                "Materials" => production.Materials,
                "Science" => production.Science,
                "Energy" => production.Energy,
                _ => 0
            };
            var relevantLines = production.BreakdownText.Where(line => line.Contains(productionType)).ToList();
            relevantLines.Add($"Total {productionType} per Turn: {Functions.GetSignedValue(total)}");
            return $"{productionType}:\n" + string.Join("\n", relevantLines);
        }

        /// <summary>
        /// Gets a tooltip showing region production and improvements.
        /// </summary>
        public string GetRegTooltip(int planetIndex, int regIndex)
        {
            List<string> tooltipLines = new List<string>();
            int hab = Math.Abs(_planets[planetIndex].Habitat[regIndex]);
            var habitat = _habitatTypes[hab];
            tooltipLines.Add($"Region {regIndex}: {habitat.Name}");
            tooltipLines.Add($"Base Production:");
            tooltipLines.Add($" Food: {Functions.GetSignedValue(habitat.FoodProd)}");
            tooltipLines.Add($" Materials: {Functions.GetSignedValue(habitat.MatProd)}");
            tooltipLines.Add($" Science: {Functions.GetSignedValue(habitat.SciProd)}");
            tooltipLines.Add($" Energy: {Functions.GetSignedValue(habitat.EnergyProd)}");

            // Tech bonuses that apply to habitats
            foreach (var tech in _techs)
            {
                if (!tech.IsResearched) continue;

                foreach (var bonus in tech.BonusUnlocks)
                {
                    if (bonus.Habitat == habitat.Name)
                    {
                        if (bonus.FoodProd != 0)
                            tooltipLines.Add($"  Tech Bonus Food: {Functions.GetSignedValue(bonus.FoodProd)}");
                        if (bonus.MatProd != 0)
                            tooltipLines.Add($"  Tech Bonus Materials: {Functions.GetSignedValue(bonus.MatProd)}");
                        if (bonus.SciProd != 0)
                            tooltipLines.Add($"  Tech Bonus Science: {Functions.GetSignedValue(bonus.SciProd)}");
                        if (bonus.EnergyProd != 0)
                            tooltipLines.Add($"  Tech Bonus Energy: {Functions.GetSignedValue(bonus.EnergyProd)}");
                    }
                }
            }

            if (_planets[planetIndex].HabitatPopulated[regIndex])
            {
                tooltipLines.Add($"Population: {GameData.HabitatTypes[_planets[planetIndex].Habitat[regIndex]].PopNeeded}");
            }
            else
            {
                tooltipLines.Add("Population: Unpopulated");
            }

            // Add region bonus info
            string regionBonusInfo = _regionBonusManager.GetRegionBonusTooltipInfo(_planets[planetIndex], regIndex);
            if (!string.IsNullOrEmpty(regionBonusInfo))
            {
                var regionBonusLines = regionBonusInfo.Split('\n');
                tooltipLines.AddRange(regionBonusLines);
            }

            int imp = _planets[planetIndex].Improvements[regIndex];
            if (imp >= 0)
            {
                var improvement = _planetImprovements[imp];
                tooltipLines.Add($"Improvement: {improvement.Name}");
                tooltipLines.Add($"  Food: {Functions.GetSignedValue(improvement.FoodProd)}");
                tooltipLines.Add($"  Materials: {Functions.GetSignedValue(improvement.MatProd)}");
                tooltipLines.Add($"  Science: {Functions.GetSignedValue(improvement.SciProd)}");
                tooltipLines.Add($"  Energy: {Functions.GetSignedValue(improvement.EnergyProd)}");
                int occupiedUnitId = _planets[planetIndex].OccupiedByUnit[regIndex];
                if (occupiedUnitId >= 0)
                {
                    var occupiedUnit = _unitManager.GetUnitById(occupiedUnitId);
                    if (occupiedUnit != null)
                    {
                        int unitTypeIndex = occupiedUnit.TypeIndex;
                        tooltipLines.Add($"Occupied by: {occupiedUnit.Name} ({_unitTypes[unitTypeIndex].Name})");
                        tooltipLines.Add($"Extra Production:");
                        tooltipLines.Add($"  Food: {Functions.GetSignedValue(_unitTypes[unitTypeIndex].ExtraFoodProd)}");
                        tooltipLines.Add($"  Materials: {Functions.GetSignedValue(_unitTypes[unitTypeIndex].ExtraMatProd)}");
                        tooltipLines.Add($"  Science: {Functions.GetSignedValue(_unitTypes[unitTypeIndex].ExtraSciProd)}");
                    }
                }
            }
            else
            {
                tooltipLines.Add("No Improvement");
            }

            return string.Join("\n", tooltipLines);
        }

        /// <summary>
        /// Gets a global production tooltip showing all owned planets' production.
        /// </summary>
        public string BuildGlobalProductionTooltip(string productionType)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{productionType} (per Turn):");

            for (int i = 0; i < Constants.NUM_PLANETS; i++)
            {
                if (_planets[i].Status != PlanetStatus.Owned) continue;

                int total = productionType switch
                {
                    "Food" => _planets[i].Food,
                    "Materials" => _planets[i].Mat,
                    "Science" => 0,
                    "Energy" => _planets[i].Energy,
                    _ => 0
                };
                string delta = CalculateProductionTurn(i, productionType); // returns signed string

                if (productionType == "Science")
                {
                    sb.AppendLine($"{_planets[i].Name}: {delta}");
                }
                else
                {
                    sb.AppendLine($"{_planets[i].Name}: {total} ({delta})");
                }
            }

            return sb.ToString();
        }
    }
}
