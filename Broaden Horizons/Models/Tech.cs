using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace BroadenHorizons
{
    public class Tech
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; } // total science points needed
        public int MinScience { get; set; } // minimum total science to start researching
        public List<int> Prerequisites { get; set; } = new List<int>();
        public bool IsResearched { get; set; } = false;
        public bool IsInProgress { get; set; } = false;
        public int ResearchProgress { get; set; } = 0; // accumulated science points
        public List<HabitatBonus> BonusUnlocks { get; set; } = new List<HabitatBonus>();
        public Vector2 GridPosition { get; set; }
        public Vector2 UiPosition { get; set; }
        public bool CanResearchTech(List<Tech> allTechs, int currentScience)
        {
            if (IsResearched || IsInProgress) return false;
            if (currentScience < MinScience) return false;
            return Prerequisites.All(p => allTechs[p].IsResearched);
        }

        public static bool HasTechTreeActions(List<Tech> allTechs, int currentScience)
        {
            if (allTechs.Any(tech => tech.IsInProgress))
                return false; // Already researching something, no new actions

            return allTechs.Any(tech => !tech.IsResearched && tech.CanResearchTech(allTechs, currentScience));
        }

        public static string GetItemsUnlockedByTech(int techID)
        {
            var items = new List<string>();
            foreach (var UT in GameData.UnitTypes)
            {
                if (UT.RequiredTech == techID)
                {
                    items.Add(UT.Name);
                }
            }

            foreach (var ST in GameData.ShipTypes)
            {
                if (ST.RequiredTech == techID)
                {
                    items.Add(ST.Name);
                }
            }

            foreach (var IMP in GameData.PlanetImprovements)
            {
                if (IMP.RequiredTech == techID)
                {
                    items.Add(IMP.Name);
                }
            }
            return items.Count > 0 ? $"{string.Join(", ", items)}" : "";
        }

        public static string GetBonusesUnlockedByTech(int techID)
        {
            var bonuses = new List<string>();
            var tech = GameData.Technologies.FirstOrDefault(t => t.ID == techID);
            foreach (var bonus in tech.BonusUnlocks)
            {
                var bonusDesc = "";
                if (bonus.FoodProd > 0) bonusDesc += $"{bonus.FoodProd} Extra Food";
                if (bonus.MatProd > 0) bonusDesc += $"{bonus.MatProd} Extra Materials";
                if (bonus.SciProd > 0) bonusDesc += $"{bonus.SciProd} Extra Science";
                if (bonus.EnergyProd > 0) bonusDesc += $"{bonus.EnergyProd} Extra Energy";
                bonuses.Add($"{bonus.Habitat}: {bonusDesc}");
            }
            return bonuses.Count > 0 ? $"\nBonuses: {string.Join(", ", bonuses)}" : "";
        }

    }
    public struct HabitatBonus
    {
        public string Habitat { get; set; }
        public int FoodProd { get; set; }
        public int MatProd { get; set; }
        public int SciProd { get; set; }
        public int EnergyProd { get; set; }
    }
}