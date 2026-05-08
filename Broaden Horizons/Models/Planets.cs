using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace BroadenHorizons
{
    public class Planet
    {
        public string Name { get; set; }
        public int XPos { get; set; }
        public int YPos { get; set; }
        public int Dimens { get; set; }
        public int Population { get; set; } = 0;
        public int Temperature { get; set; }
        public List<int> Habitat { get; set; } = [.. new int[37]];
        public List<bool> HabitatPopulated { get; set; } = [.. new bool[37]];
        public List<int> Improvements { get; set; } = [.. new int[37]];
        public List<int> OccupiedByUnit { get; set; } = [.. new int[37]];
        public List<string> RegionBonuses { get; set; } = [];
        public List<int> RegionBonusRegions { get; set; } = [.. new int[37]];
        public PlanetStatus Status { get; set; } = PlanetStatus.Unexplored;
        public int Food { get; set; } = 0;
        public int Mat { get; set; } = 0;
        public int Energy { get; set; } = 0;
        public int TextureId { get; set; } = Constants.DEFAULT_PLANET_TEXTURE;
        public Color TintColor { get; set; }
    }

    public class TemperatureRange
    {
        public string Name { get; set; }
        public int MinTemp { get; set; }
        public int MaxTemp { get; set; }
        public float PopulationModifier { get; set; }
    }

    public enum PlanetStatus
    {
        Owned = 0,
        Unexplored = 1,
        Explored = 2,
        ProbeEnRoute = 3
    }

    public class HabitatType
    {
        public string Name { get; set; }
        public int FoodProd { get; set; }
        public int MatProd { get; set; }
        public int SciProd { get; set; }
        public int EnergyProd { get; set; }
        public int PopNeeded { get; set; }
        public int FirstProb { get; set; }
        public int SecProb { get; set; }
        public int ThirdProb { get; set; }
        public int TextureId { get; set; }
    }

    public class PlanetImprovement
    {
        public string Name { get; set; }
        public int FoodProd { get; set; }
        public int MatProd { get; set; }
        public int SciProd { get; set; }
        public int EnergyProd { get; set; }
        public string AllowedHabitat { get; set; }
        public string AllowedUnit { get; set; }
        public int TextureId { get; set; }
        public int TurnsToBuild { get; set; }
        public int MatCost { get; set; }
        public int RequiredTech { get; set; } = -1;
    }

    public class ProductionBreakdown
    {
        public int Food;
        public int Materials;
        public int Science;
        public int Energy;
        public List<string> BreakdownText = [];
    }
}