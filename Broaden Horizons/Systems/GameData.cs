using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using System;

namespace BroadenHorizons
{
    public static class GameData
    {
        public static readonly List<string> PlanetNames =
        [
            "Ceti Alpha one", "Orionis I", "Alioth", "Fussbar", "Cumo", "Mirak", "Izar", "Arcturus", "Canes Ceti", "Draco",
            "Altair", "Albireo", "Mekbuda", "Sol", "Vega", "Taulus", "Tholia", "Lynch", "Talos", "Gorn",
            "Vendor", "Romulus", "Remus", "Gothos", "Rator", "Memory Alpha", "Time Planet", "Ross", "Anto", "Cygnet",
            "Delta", "Wrigley", "Kzin", "Regula", "Gamma", "Omicron", "Gamma Hydra", "Beta Xela", "Tarsus", "Vela",
            "Rigel", "Organia", "Ka'Hat", "Toman", "Ikaal", "Kandru", "Klamuth", "Kahless", "Graf", "Arkto",
            "Kazh", "Kumar", "Korvus", "Sif", "Kanthu", "Karhammur", "Krell", "Kura", "Orion", "Sirius",
            "Bellatrix", "Saiph", "RomII", "Lacerta", "Praesepe", "Castor", "Fornax", "Mira", "Delphi", "Cleopatra",
            "Lynx Ceti", "Beta Auriga", "Caeladium", "Eranda", "Ceros", "Alycyone", "Procyon", "Innes", "Achernar", "Pyxis",
            "Corvus", "Bernices", "Cassiopeia", "Taurus", "Crete", "Aries Ceti", "Libra", "Bootes", "Arigo", "Caelum",
            "Grus", "Equuleus", "Capricornus", "Phoenix", "Kzoz", "Lyra", "Corona", "Scorpius", "Australis", "Sagitta",
            "Shaddan", "Kameon", "Rappaport", "Chanda", "Chirpsithra", "Sheegupt", "Antilia", "Forel", "Lottl", "Zarop-Opar",
            "Castolia", "Sarol", "Ftokteek", "Kchipreesee", "Hallone", "Kthistlmup", "Packlidia", "Leshy", "Ordadz", "Cylon",
            "Cimtar", "Toltek", "Galaxtia", "Madagon", "Carillon", "Kobol", "Uri", "Ovion", "Camelot", "Tataween",
            "Dagaba", "Cleandria", "Endor", "Kugo", "Holstein", "Jernsey", "Heiffer", "New Bohemia", "Amazonia", "Tesla",
            "Casablanca", "Azores", "Chrome", "Orkney World", "Mills", "Limball", "Bazal", "Bonzi", "Zippi", "Yoggoth", "Krypton"
        ];

        public static readonly List<TemperatureRange> TemperatureRanges =
        [
            new TemperatureRange { Name = "Frigid", MinTemp = -50, MaxTemp = -21, PopulationModifier = 0.3f, TintColor = new Color(100, 180, 255) },
            new TemperatureRange { Name = "Cold", MinTemp = -20, MaxTemp = 15, PopulationModifier = 0.6f, TintColor = new Color(135, 206, 250) },
            new TemperatureRange { Name = "Temperate", MinTemp = 16, MaxTemp = 40, PopulationModifier = 1.0f, TintColor = new Color(144, 238, 144) },
            new TemperatureRange { Name = "Hot", MinTemp = 41, MaxTemp = 65, PopulationModifier = 0.7f, TintColor = new Color(255, 165, 0) },
            new TemperatureRange { Name = "Scorching", MinTemp = 66, MaxTemp = 100, PopulationModifier = 0.4f, TintColor = new Color(220, 50, 50) }
        ];

        public static readonly int[] NeighborsData = new int[]
        {
            1, 2, 3, 4, 5, 6,
            0, 2, 6, 7, 8, 18,
            0, 1, 3, 8, 9, 10,
            0, 2, 4, 10, 11, 12,
            0, 3, 5, 12, 13, 14,
            0, 4, 6, 14, 15, 16,
            0, 1, 5, 16, 17, 18,
            1, 8, 18, 19, 20, 36,
            1, 2, 7, 9, 20, 21,
            2, 8, 10, 21, 22, 23,
            2, 3, 9, 11, 23, 24,
            3, 10, 12, 24, 25, 26,
            3, 4, 11, 13, 26, 27,
            4, 12, 14, 27, 28, 29,
            4, 5, 13, 15, 29, 30,
            5, 14, 16, 30, 31, 32,
            5, 6, 15, 17, 32, 33,
            6, 16, 18, 33, 34, 35,
            1, 6, 7, 17, 35, 36,
            7, 20, 36, -1, -1, -1,
            7, 8, 19, 21, -1, -1,
            8, 9, 20, 22, -1, -1,
            9, 21, 23, -1, -1, -1,
            9, 10, 22, 24, -1, -1,
            10, 11, 23, 25, -1, -1,
            11, 24, 26, -1, -1, -1,
            11, 12, 25, 27, -1, -1,
            12, 13, 26, 28, -1, -1,
            13, 27, 29, -1, -1, -1,
            13, 14, 28, 30, -1, -1,
            14, 15, 29, 31, -1, -1,
            15, 30, 32, -1, -1, -1,
            15, 16, 31, 33, -1, -1,
            16, 17, 32, 34, -1, -1,
            17, 33, 35, -1, -1, -1,
            17, 18, 34, 36, -1, -1,
            7, 18, 19, 35, -1, -1
        };

        public static readonly List<HabitatType> HabitatTypes =
        [
            new HabitatType { Name = "City", FoodProd = 3, MatProd = 3, SciProd = 1, EnergyProd = 1, PopNeeded = 50, FirstProb = 0, SecProb = 0, ThirdProb = 0, TextureId = 10 },
            new HabitatType { Name = "Forest", FoodProd = 2, MatProd = 3, SciProd = 1, EnergyProd = 0, PopNeeded = 25, FirstProb = 15, SecProb = 12, ThirdProb = 8, TextureId = 11 },
            new HabitatType { Name = "Ocean", FoodProd = 3, MatProd = 0, SciProd = 2, EnergyProd = 0, PopNeeded = 25, FirstProb = 30, SecProb = 24, ThirdProb = 16, TextureId = 12 },
            new HabitatType { Name = "Mountains", FoodProd = 1, MatProd = 3, SciProd = 1, EnergyProd = 0, PopNeeded = 25, FirstProb = 45, SecProb = 36, ThirdProb = 24, TextureId = 13 },
            new HabitatType { Name = "Prairie", FoodProd = 3, MatProd = 1, SciProd = 1, EnergyProd = 0, PopNeeded = 25, FirstProb = 60, SecProb = 48, ThirdProb = 32, TextureId = 14 },
            new HabitatType { Name = "Valley", FoodProd = 3, MatProd = 2, SciProd = 1, EnergyProd = 0, PopNeeded = 25, FirstProb = 72, SecProb = 60, ThirdProb = 40, TextureId = 15 },
            new HabitatType { Name = "Desert", FoodProd = 1, MatProd = 2, SciProd = 2, EnergyProd = 0, PopNeeded = 25, FirstProb = 84, SecProb = 70, ThirdProb = 55, TextureId = 16 },
            new HabitatType { Name = "Ruins", FoodProd = 1, MatProd = 1, SciProd = 3, EnergyProd = 0, PopNeeded = 25, FirstProb = 92, SecProb = 84, ThirdProb = 70, TextureId = 17 },
            new HabitatType { Name = "Volcano", FoodProd = 1, MatProd = 3, SciProd = 2, EnergyProd = 0, PopNeeded = 25, FirstProb = 96, SecProb = 92, ThirdProb = 85, TextureId = 18 },
            new HabitatType { Name = "Arctic", FoodProd = 1, MatProd = 1, SciProd = 2, EnergyProd = 0, PopNeeded = 25, FirstProb = 100, SecProb = 100, ThirdProb = 100, TextureId = 19 }
        ];

        public static readonly List<UnitType> UnitTypes =
        [
            new UnitType { Name = "Explorers", Type = UnitTypeEnum.Explorer, FoodCost = 12, MatCost = 2, PopCost = 25, FoodMaint = 2, MatMaint = 0, TextureId = 30, RecruitTurns = 2, RequiredTech = 0 },
            new UnitType { Name = "Farmers", Type = UnitTypeEnum.Farmer, FoodCost = 15, MatCost = 2, PopCost = 50, FoodMaint = 3, MatMaint = 0, ExtraFoodProd = 2, ExtraMatProd = 0, ExtraSciProd = 0, TextureId = 31, RecruitTurns = 2, RequiredTech = 13 },
            new UnitType { Name = "Miners", Type = UnitTypeEnum.Miner, FoodCost = 18, MatCost = 3, PopCost = 50, FoodMaint = 3, MatMaint = 1, ExtraFoodProd = 0, ExtraMatProd = 2, ExtraSciProd = 0, TextureId = 32, RecruitTurns = 2, RequiredTech = 1 },
            new UnitType { Name = "Scientists", Type = UnitTypeEnum.Scientist, FoodCost = 15, MatCost = 0, PopCost = 50, FoodMaint = 3, MatMaint = 0, ExtraFoodProd = 0, ExtraMatProd = 0, ExtraSciProd = 2, TextureId = 33, RecruitTurns = 2, RequiredTech = 2 },
            new UnitType { Name = "Builders", Type = UnitTypeEnum.Builder, FoodCost = 20, MatCost = 5, PopCost = 50, FoodMaint = 3, MatMaint = 1, TextureId = 34, RecruitTurns = 3, RequiredTech = 3 },
            new UnitType { Name = "Harvesters", Type = UnitTypeEnum.Harvester, FoodCost = 12, MatCost = 2, PopCost = 50, FoodMaint = 2, MatMaint = 0, ExtraFoodProd = 0, ExtraMatProd = 2, ExtraSciProd = 0, TextureId = 35, RecruitTurns = 2, RequiredTech = 13 },
            new UnitType { Name = "Fishermen", Type = UnitTypeEnum.Fisher, FoodCost = 12, MatCost = 0, PopCost = 50, FoodMaint = 2, MatMaint = 0, ExtraFoodProd = 2, ExtraMatProd = 0, ExtraSciProd = 0, TextureId = 36, RecruitTurns = 2, RequiredTech = 14 },
            new UnitType { Name = "Colonists", Type = UnitTypeEnum.Colonist, FoodCost = 25, MatCost = 5, PopCost = 100, FoodMaint = 3, MatMaint = 0, TextureId = 37, RecruitTurns = 4, RequiredTech = 9 }
        ];

        public static readonly List<PlanetImprovement> PlanetImprovements =
        [
            new PlanetImprovement { Name = "Urban Development", FoodProd = 1, MatProd = 1, SciProd = 1, EnergyProd = 1, AllowedHabitat = "City", AllowedUnit = "Scientists", TextureId = 11, TurnsToBuild = 2, MatCost = 30, RequiredTech = 0 },
            new PlanetImprovement { Name = "Woodcutter Camp", FoodProd = -1, MatProd = 2, SciProd = 0, EnergyProd = 0, AllowedHabitat = "Forest", AllowedUnit = "Harvesters", TextureId = 11, TurnsToBuild = 3, MatCost = 30, RequiredTech = 3 },
            new PlanetImprovement { Name = "Hunting Camp", FoodProd = 2, MatProd = -1, SciProd = 0, EnergyProd = 0, AllowedHabitat = "Forest", AllowedUnit = "Harvesters", TextureId = 11, TurnsToBuild = 3, MatCost = 30, RequiredTech = 3 },
            new PlanetImprovement { Name = "Aquaculture Farm", FoodProd = 2, MatProd = 0, SciProd = 0, EnergyProd = 0, AllowedHabitat = "Ocean", AllowedUnit = "Fishermen", TextureId = 11, TurnsToBuild = 3, MatCost = 30, RequiredTech = 14 },
            new PlanetImprovement { Name = "Mines", FoodProd = 0, MatProd = 2, SciProd = 0, EnergyProd = -1, AllowedHabitat = "Mountains", AllowedUnit = "Miners", TextureId = 11, TurnsToBuild = 3, MatCost = 30, RequiredTech = 1 },
            new PlanetImprovement { Name = "Crop Intensification", FoodProd = 2, MatProd = 0, SciProd = 0, EnergyProd = 0, AllowedHabitat = "Prairie", AllowedUnit = "Farmers", TextureId = 11, TurnsToBuild = 3, MatCost = 30, RequiredTech = 13 },
            new PlanetImprovement { Name = "Irrigation Systems", FoodProd = 2, MatProd = 0, SciProd = 0, EnergyProd = 0, AllowedHabitat = "Valley", AllowedUnit = "Farmers", TextureId = 11, TurnsToBuild = 3, MatCost = 30, RequiredTech = 3 },
            new PlanetImprovement { Name = "Science Research Stations", FoodProd = 0, MatProd = 0, SciProd = 2, EnergyProd = -1, AllowedHabitat = "Desert", AllowedUnit = "Scientists", TextureId = 11, TurnsToBuild = 3, MatCost = 30, RequiredTech = 2 },
            new PlanetImprovement { Name = "Historical Research", FoodProd = 0, MatProd = 0, SciProd = 3, EnergyProd = 0, AllowedHabitat = "Ruins", AllowedUnit = "Scientists", TextureId = 11, TurnsToBuild = 3, MatCost = 30, RequiredTech = 2 },
            new PlanetImprovement { Name = "Geothermal Mining", FoodProd = 0, MatProd = 2, SciProd = 0, EnergyProd = -3, AllowedHabitat = "Volcano", AllowedUnit = "Miners", TextureId = 11, TurnsToBuild = 3, MatCost = 30, RequiredTech = 5 },
            new PlanetImprovement { Name = "Cryo-Science Research Lab", FoodProd = 0, MatProd = 0, SciProd = 2, EnergyProd = -2, AllowedHabitat = "Arctic", AllowedUnit = "Scientists", TextureId = 11, TurnsToBuild = 3, MatCost = 30, RequiredTech = 6 },
            new PlanetImprovement { Name = "Wind Farm", FoodProd = 0, MatProd = 0, SciProd = 0, EnergyProd = 3, AllowedHabitat = "Mountains", AllowedUnit = "Scientists", TextureId = 11, TurnsToBuild = 4, MatCost = 40, RequiredTech = 4 },
            new PlanetImprovement { Name = "Hydroelectric Dam", FoodProd = 1, MatProd = 0, SciProd = 0, EnergyProd = 3, AllowedHabitat = "Valley", AllowedUnit = "Scientists", TextureId = 11, TurnsToBuild = 4, MatCost = 40, RequiredTech = 4 },
            new PlanetImprovement { Name = "Solar Power Plant", FoodProd = 0, MatProd = 0, SciProd = 0, EnergyProd = 3, AllowedHabitat = "Desert", AllowedUnit = "Scientists", TextureId = 11, TurnsToBuild = 4, MatCost = 40, RequiredTech = 4 },
            new PlanetImprovement { Name = "Geothermal Power Plant", FoodProd = 0, MatProd = 0, SciProd = 0, EnergyProd = 5, AllowedHabitat = "Volcano", AllowedUnit = "Scientists", TextureId = 11, TurnsToBuild = 5, MatCost = 60, RequiredTech = 8 }
        ];

        public static readonly List<ShipType> ShipTypes =
        [
            new ShipType { Name = "Probe", MatCost = 50, MaintCost = 2, Speed = 150, EnergyperTurn = 1, TurnsToBuild = 3, RequiredTech = 0, TextureId = 61, Type = ShipTypeEnum.Probe, Capacity = 0 },
            new ShipType { Name = "Colony Ship", MatCost = 200, MaintCost = 4, Speed = 100, EnergyperTurn = 4, TurnsToBuild = 5, RequiredTech = 9, TextureId = 62, Type = ShipTypeEnum.ColonyShip, Capacity = 0 },
            new ShipType { Name = "Freighter", MatCost = 100, MaintCost = 8, Speed = 125, EnergyperTurn = 2, TurnsToBuild = 4, RequiredTech = 15, TextureId = 63, Type = ShipTypeEnum.Freighter, Capacity = 50 },
            new ShipType { Name = "Terraformer", MatCost = 150, MaintCost = 6, Speed = 80, EnergyperTurn = 5, TurnsToBuild = 6, RequiredTech = 16, TextureId = 64, Type = ShipTypeEnum.Terraformer, Capacity = 0 },
        ];

        public static readonly List<Tech> Technologies =
        [
            new Tech { ID = 0, Name = "Colonization Basics", Description = "Fundamental settlement knowledge", Cost = 10, MinScience = 0, Prerequisites = [], BonusUnlocks = [], GridPosition = new Vector2(1, 7) },
            new Tech { ID = 1, Name = "Industrial Mining", Description = "Improved resource extraction techniques", Cost = 30, MinScience = 0, Prerequisites = [0], BonusUnlocks = [new() { Habitat = "Mountains", MatProd = 1 }], GridPosition = new Vector2(2, 1) },
            new Tech { ID = 2, Name = "Research Methods", Description = "Foundations of scientific inquiry", Cost = 25, MinScience = 0, Prerequisites = [0], BonusUnlocks = [new() { Habitat = "Desert", SciProd = 1 }, new HabitatBonus { Habitat = "Ruins", SciProd = 1 }], GridPosition = new Vector2(2, 4) },
            new Tech { ID = 3, Name = "Construction Techniques", Description = "Enhanced building practices", Cost = 35, MinScience = 20, Prerequisites = [0], BonusUnlocks = [], GridPosition = new Vector2(2, 8) },
            new Tech { ID = 4, Name = "Renewable Energy", Description = "Sustainable power sources", Cost = 40, MinScience = 10, Prerequisites = [0], BonusUnlocks = [new() { Habitat = "Mountains", EnergyProd = 1 }, new HabitatBonus { Habitat = "Desert", EnergyProd = 1 }], GridPosition = new Vector2(2, 11) },
            new Tech { ID = 5, Name = "Advanced Mining", Description = "Advanced mining operations", Cost = 60, MinScience = 30, Prerequisites = [1], BonusUnlocks = [new() { Habitat = "Volcano", MatProd = 1 }], GridPosition = new Vector2(3, 1) },
            new Tech { ID = 6, Name = "Polar Research", Description = "Studies in extreme environments", Cost = 50, MinScience = 25, Prerequisites = [2], BonusUnlocks = [new() { Habitat = "Arctic", SciProd = 1 }], GridPosition = new Vector2(3, 3) },
            new Tech { ID = 7, Name = "Advanced Genetics", Description = "Genetic manipulation for production", Cost = 80, MinScience = 50, Prerequisites = [2], BonusUnlocks = [new() { Habitat = "Forest", FoodProd = 1 }, new HabitatBonus { Habitat = "Prairie", FoodProd = 1 }], GridPosition = new Vector2(3, 5) },
            new Tech { ID = 8, Name = "Geothermal Energy", Description = "Harnessing planetary heat", Cost = 70, MinScience = 40, Prerequisites = [4], BonusUnlocks = [new() { Habitat = "Volcano", EnergyProd = 2 }], GridPosition = new Vector2(3, 11) },
            new Tech { ID = 9, Name = "Colonization Technology", Description = "Nanoscale building enhancements", Cost = 100, MinScience = 70, Prerequisites = [3], BonusUnlocks = [new() { Habitat = "Valley", MatProd = 1 }], GridPosition = new Vector2(3, 8) },
            new Tech { ID = 10, Name = "Quantum Materials", Description = "Exotic material processing", Cost = 120, MinScience = 90, Prerequisites = [5], BonusUnlocks = [new() { Habitat = "Mountains", MatProd = 2 }], GridPosition = new Vector2(4, 1) },
            new Tech { ID = 11, Name = "Alien Xenology", Description = "Deep study of alien life", Cost = 110, MinScience = 80, Prerequisites = [6], BonusUnlocks = [new() { Habitat = "Ruins", SciProd = 2 }], GridPosition = new Vector2(4, 3) },
            new Tech { ID = 12, Name = "Fusion Power", Description = "Advanced energy fusion", Cost = 150, MinScience = 120, Prerequisites = [8], BonusUnlocks = [new() { Habitat = "City", EnergyProd = 2 }], GridPosition = new Vector2(4, 11) },
            new Tech { ID = 13, Name = "Farming Techniques", Description = "New techniques of farming", Cost = 25, MinScience = 0, Prerequisites = [0], BonusUnlocks = [], GridPosition = new Vector2(2, 13) },
            new Tech { ID = 14, Name = "Aquaculture Development", Description = "Deep research about water resources", Cost = 60, MinScience = 15, Prerequisites = [13], BonusUnlocks = [], GridPosition = new Vector2(3, 13) },
            new Tech { ID = 15, Name = "Freight Logistics", Description = "Enables basic freighter ships", Cost = 50, MinScience = 25, Prerequisites = [9], BonusUnlocks = [], GridPosition = new Vector2(4, 7) },
            new Tech { ID = 16, Name = "Terraformation", Description = "Technology to modify temperature", Cost = 50, MinScience = 25, Prerequisites = [9], BonusUnlocks = [], GridPosition = new Vector2(4, 9) },
        ];

        public static readonly List<GameEvent> GameEvents =
        [
            new GameEvent
            {
                Name = "Meteor Strike",
                GetDescription = (bh, target) => $"A meteor has struck {((Planet)target).Name}, some population has been lost,\nsome materials could be fetched though.",
                GetValidTargets = bh => bh.Planets.Where(p => p.Population > 0).Cast<object>().ToList(),
                Weight = 10,
                Execute = (game, target) => { var planet = (Planet)target; planet.Population = (int)Math.Round(planet.Population * 0.95); planet.Mat += 15; }
            },
            new GameEvent
            {
                Name = "Solar Flare",
                GetDescription = (bh, target) => $"A solar flare has hit {((Planet)target).Name}\ncausing energy shortages.",
                GetValidTargets = bh => bh.Planets.Where(p => p.Population > 0 && p.Energy > 10).Cast<object>().ToList(),
                Weight = 10,
                Execute = (game, target) => { var planet = (Planet)target; planet.Energy = (int)Math.Round(planet.Energy * 0.8); }
            },
            new GameEvent
            {
                Name = "Good Harvest",
                GetDescription = (bh, target) => $"A good harvest on {((Planet)target).Name}\nhas increased food production.",
                GetValidTargets = bh => bh.Planets.Where(p => p.Food > 20).Cast<object>().ToList(),
                Weight = 5,
                Execute = (game, target) => { var planet = (Planet)target; planet.Food = (int)Math.Round(planet.Food * 1.05); }
            },
            new GameEvent
            {
                Name = "Technological Breakthrough",
                GetDescription = (bh, target) => $"A technological breakthrough on {((Planet)target).Name} has increased production efficiency.",
                GetValidTargets = bh => bh.Planets.Where(p => p.Population > 0).Cast<object>().ToList(),
                Weight = 5,
                Execute = (game, target) => { var planet = (Planet)target; planet.Mat = (int)Math.Round(planet.Mat * 1.05); }
            },
            new GameEvent
            {
                Name = "Baby Boom",
                GetDescription = (bh, target) => $"A baby boom on {((Planet)target).Name} has increased the population.",
                GetValidTargets = bh => bh.Planets.Where(p => p.Population > 0).Cast<object>().ToList(),
                Weight = 5,
                Execute = (game, target) => { var planet = (Planet)target; planet.Population = (int)Math.Round(planet.Population * 1.1); }
            }
        ];

        public static void AssignTechPositions()
        {
            int boxDistance = (Constants.SCREEN_HEIGHT - Constants.TOP_BAR_HEIGHT) / ((int)Technologies.Max(tech => tech.GridPosition.Y) + 2); // Distance between boxes
            int startX = Constants.TECH_TREE_HORIZ_MARGIN;
            int xdistance = Constants.TECH_TREE_XDISTANCE;
            int startY = Constants.TOP_BAR_HEIGHT;
            // Define a list of Vector2 positions for each technology
            for (int i = 0; i < Technologies.Count; i++)
            {
                Technologies[i].UiPosition = new Vector2(startX + (xdistance * (int)Technologies[i].GridPosition.X) - xdistance, startY + (boxDistance * (int)Technologies[i].GridPosition.Y));
            }
        }
    }
}