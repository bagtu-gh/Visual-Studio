using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BroadenHorizons
{
    public class RegionBonus
    {
        public string Name { get; set; }
        public List<string> AllowedHabitats { get; set; } = new List<string>();
        public string BonusType { get; set; } // "Food", "Materials", "Science", or "Energy"
        public int BaseBonus { get; set; }
        public int RarityWeight { get; set; } // Lower = rarer
        public int TextureId { get; set; }
    }

    public class RegionBonusManager
    {
        private readonly Random _rand;
        private readonly List<RegionBonus> _regionBonusTypes;
        private List<HabitatType> _HabitatTypes;

        public List<RegionBonus> RegionBonusTypes => _regionBonusTypes;

        public RegionBonusManager(Game game, Random rand, List<HabitatType> HabitatTypes)
        {
            _rand = rand;
            _regionBonusTypes = new List<RegionBonus>();
            _HabitatTypes = HabitatTypes ?? new List<HabitatType>();
        }

        public void UpdateHabitats(List<HabitatType> HabitatTypes)
        {
            _HabitatTypes = HabitatTypes ?? new List<HabitatType>();
        }

        public void InitializeRegionBonuses()
        {
            _regionBonusTypes.Clear();

            _regionBonusTypes.AddRange(
            [
                // Common bonuses (high probability)
                new RegionBonus { Name = "Mineral Vein", AllowedHabitats = new List<string> { "Mountains", "Valley" }, BonusType = "Materials", BaseBonus = 1, RarityWeight = 80, TextureId = 40 },
                new RegionBonus { Name = "Fertile Soil", AllowedHabitats = new List<string> { "Prairie", "Valley", "Forest" }, BonusType = "Food", BaseBonus = 1, RarityWeight = 75, TextureId = 41 },
                new RegionBonus { Name = "Crystal Deposits", AllowedHabitats = new List<string> { "Mountains", "Desert" }, BonusType = "Science", BaseBonus = 1, RarityWeight = 70, TextureId = 42 },
                
                // Medium rarity
                new RegionBonus { Name = "Iron Ore", AllowedHabitats = new List<string> { "Mountains", "Volcano" }, BonusType = "Materials", BaseBonus = 2, RarityWeight = 50, TextureId = 43 },
                new RegionBonus { Name = "Ancient Settlement", AllowedHabitats = new List<string> { "Ruins" }, BonusType = "Science", BaseBonus = 2, RarityWeight = 45, TextureId = 44 },
                new RegionBonus { Name = "High-Class Timber", AllowedHabitats = new List<string> { "Forest", "Valley" }, BonusType = "Materials", BaseBonus = 2, RarityWeight = 60, TextureId = 45 },
                
                // Rare bonuses (low probability, high bonus)
                new RegionBonus { Name = "Gold Vein", AllowedHabitats = new List<string> { "Mountains", "Desert" }, BonusType = "Materials", BaseBonus = 3, RarityWeight = 20, TextureId = 46 },
                new RegionBonus { Name = "Exotic Flora", AllowedHabitats = new List<string> { "Forest", "Valley" }, BonusType = "Food", BaseBonus = 2, RarityWeight = 30, TextureId = 47 },
                new RegionBonus { Name = "Quantum Crystals", AllowedHabitats = new List<string> { "Arctic", "Ruins" }, BonusType = "Science", BaseBonus = 3, RarityWeight = 15, TextureId = 48 },
                new RegionBonus { Name = "Geothermal Vents", AllowedHabitats = new List<string> { "Volcano" }, BonusType = "Energy", BaseBonus = 2, RarityWeight = 25, TextureId = 49 }
            ]);
        }

        public Texture2D CreateRegionBonusCircleTexture(int regionBonusId, GraphicsDevice graphicsDevice)
        {
            Texture2D texture = new Texture2D(graphicsDevice, 32, 32);
            Color[] data = new Color[32 * 32];

            // Simple colored circle based on region bonus type
            Color color = regionBonusId switch
            {
                40 => new Color(184, 115, 51), // Copper (brownish)
                41 => Color.Green,              // Fertile Soil
                42 => Color.Cyan,               // Crystals
                43 => Color.Gray,               // Iron
                44 => new Color(255, 140, 0),   // Ancient Ruins (orange)
                45 => Color.Brown,              // Rich Timber
                46 => Color.Gold,               // Gold
                47 => Color.LimeGreen,          // Exotic Flora
                48 => Color.Violet,             // Quantum Crystals
                49 => Color.Red,                // Geothermal Vents
                50 => Color.Magenta,            // Precious Gems
                _ => Color.White
            };

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    float dist = (float)Math.Sqrt((x - 16) * (x - 16) + (y - 16) * (y - 16));
                    data[x + y * 32] = dist < 12 ? color : Color.Transparent;
                }
            }

            texture.SetData(data);
            return texture;
        }

        public void AssignRegionBonusesToPlanet(Planet planet, bool isStartingPlanet = false)
        {
            // Calculate max bonuses based on planet size
            int maxBonuses = Math.Max(1, planet.Dimens / Constants.REGION_BONUS_MAX_PER_PLANET);

            // For starting planet, ensure minimum bonuses
            int minBonuses = isStartingPlanet ? Constants.STARTING_PLANET_MIN_REGION_BONUSES : Constants.MIN_REGION_BONUSES;
            int targetBonuses = Math.Max(minBonuses, _rand.Next(0, maxBonuses + 1));

            // Get all valid habitat regions
            List<int> validRegions = new List<int>();
            for (int r = 1; r <= 36; r++)
            {
                if (planet.Habitat[r] != Constants.NON_EXISTING_HABTITAT && planet.RegionBonusRegions[r] == -1)
                {
                    validRegions.Add(r);
                }
            }

            if (validRegions.Count == 0) return;

            // Calculate total probability weight
            int totalWeight = _regionBonusTypes.Sum(r => r.RarityWeight);

            // Select bonuses using cumulative probability (like habitat selection)
            List<int> selectedBonusIndices = new List<int>();
            HashSet<string> usedBonusNames = new HashSet<string>(); // Prevent duplicates

            for (int i = 0; i < targetBonuses && validRegions.Count > 0; i++)
            {
                // Select a bonus using cumulative probability
                int randomValue = _rand.Next(1, totalWeight + 1);
                int cumulativeWeight = 0;
                int selectedBonusIndex = -1;

                foreach (var bonus in _regionBonusTypes)
                {
                    if (usedBonusNames.Contains(bonus.Name)) continue; // Skip already used bonuses

                    cumulativeWeight += bonus.RarityWeight;
                    if (randomValue <= cumulativeWeight)
                    {
                        selectedBonusIndex = _regionBonusTypes.IndexOf(bonus);
                        break;
                    }
                }

                if (selectedBonusIndex == -1) continue; // No valid bonus found

                var selectedBonus = _regionBonusTypes[selectedBonusIndex];
                usedBonusNames.Add(selectedBonus.Name);

                // Find a suitable region for this bonus
                bool bonusPlaced = false;
                for (int j = 0; j < validRegions.Count && !bonusPlaced; j++)
                {
                    int region = validRegions[j];
                    int habitatIndex = Math.Abs(planet.Habitat[region]);
                    string habitatName = _HabitatTypes[habitatIndex].Name;

                    // Check if this habitat matches the bonus requirements
                    if (selectedBonus.AllowedHabitats.Contains(habitatName))
                    {
                        // Place the bonus
                        planet.RegionBonusRegions[region] = selectedBonusIndex;
                        planet.RegionBonuses.Add(selectedBonus.Name);
                        selectedBonusIndices.Add(selectedBonusIndex);
                        validRegions.RemoveAt(j);
                        bonusPlaced = true;

                        System.Diagnostics.Debug.WriteLine($"Placed {selectedBonus.Name} in Region {region} ({habitatName}) on {planet.Name}");
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine($"Assigned {selectedBonusIndices.Count} bonuses to {planet.Name} (Target: {targetBonuses}, Max: {maxBonuses})");
        }

        public void CalculateRegionBonuses(Planet planet, ref ProductionBreakdown result)
        {
            for (int t = 0; t <= 36; t++)
            {
                int bonusIndex = planet.RegionBonusRegions[t];
                if (bonusIndex >= 0 && planet.Habitat[t] >= 0 && planet.HabitatPopulated[t]) // Only count explored bonuses and populated habitats
                {
                    var bonus = _regionBonusTypes[bonusIndex];
                    switch (bonus.BonusType)
                    {
                        case "Food":
                            result.Food += bonus.BaseBonus;
                            result.BreakdownText.Add($"{bonus.Name} (Region {t}): +{bonus.BaseBonus} Food");
                            break;
                        case "Materials":
                            result.Materials += bonus.BaseBonus;
                            result.BreakdownText.Add($"{bonus.Name} (Region {t}): +{bonus.BaseBonus} Materials");
                            break;
                        case "Science":
                            result.Science += bonus.BaseBonus;
                            result.BreakdownText.Add($"{bonus.Name} (Region {t}): +{bonus.BaseBonus} Science");
                            break;
                        case "Energy":
                            result.Energy += bonus.BaseBonus;
                            result.BreakdownText.Add($"{bonus.Name} (Region {t}): +{bonus.BaseBonus} Energy");
                            break;
                    }
                }
            }
        }

        public string GetRegionBonusTooltipInfo(Planet planet, int regionIndex)
        {
            int bonusIndex = planet.RegionBonusRegions[regionIndex];

            if (bonusIndex >= 0)
            {
                var bonus = _regionBonusTypes[bonusIndex];
                return $"Bonus: {bonus.Name}\nYield: +{bonus.BaseBonus} {bonus.BonusType}";
            }

            return "";
        }

        public void DrawRegionBonus(Planet planet, int regionIndex, Vector2 hexCenter, SpriteBatch spriteBatch, Dictionary<int, Texture2D> textures)
        {
            int bonusIndex = planet.RegionBonusRegions[regionIndex];
            if (bonusIndex >= 0)
            {
                var bonus = _regionBonusTypes[bonusIndex];
                if (textures.TryGetValue(bonus.TextureId, out Texture2D bonusTexture))
                {
                    Vector2 bonusCenter = new Vector2(
                        hexCenter.X,
                        hexCenter.Y - Constants.HEX_SIZE / 3
                    );

                    float bonusScale = Constants.HEX_SIZE / 2.2f / bonusTexture.Width;
                    spriteBatch.Draw(bonusTexture, bonusCenter, null, Color.White, 0f,
                        new Vector2(bonusTexture.Width / 2, bonusTexture.Height / 2),
                        bonusScale, SpriteEffects.None, 0f);
                }
            }
        }
    }
}