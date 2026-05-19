using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BroadenHorizons.Screens;
using Microsoft.Xna.Framework;

namespace BroadenHorizons
{
    public partial class BH
    {
        // -----------------------------
        // Save DTO (public for tooling)
        // -----------------------------
        [Serializable]
        public class GameStateData
        {
            // Versioning & metadata
            public int SaveVersion { get; set; } = 3;               // Bumped to 3 for ships
            public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;

            // Camera / selection / UI state
            public int Turn;
            public Vector2 ScrollOffset;
            public int PosX, PosY;
            public int CurrentPlanet;
            public int SelectedUnit;
            public List<int> PossibleDestinations;
            public bool confirmEndTurn;
            public bool confirmRecruit;
            public int recruitIndex;
            public bool[] hasRecruitedThisTurn;
            public bool confirmBuild;
            public int buildReg;
            public int buildImprovementIndex;
            public bool confirmOccupy;
            public int occupyReg;
            public bool chooseBuild;
            public int chooseReg;

            // World
            public Planet[] Planets;
            public List<Unit> Units { get; set; }
            public int NextUnitId { get; set; } = 0;
            public List<TurnAction> TurnActions;
            public RegionData[] RegionDatas;
            public List<string> PlanetRegionBonuses;
            public List<RegionBonus> RegionBonusTypes;

            // Tech tree
            public List<Tech> Techs { get; set; }
            public int GlobalScience { get; set; }
            public int CurrentResearch { get; set; }
            public List<HabitatBonus> GlobalHabitatBonuses { get; set; }
            public Vector2 TechScrollOffset { get; set; }

            // Visuals
            public List<Vector2> StarPositions { get; set; } = new();

            // Ships
            public List<Ship> Ships { get; set; } = new List<Ship>();
            public int NextShipId { get; set; } = 0;
        }

        // -----------------------------
        // JSON Options
        // -----------------------------
        private static readonly JsonSerializerOptions _saveOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true
        };

        private static readonly JsonSerializerOptions _loadOptions = new()
        {
            IncludeFields = true
        };

        private const string DefaultSavePath = "game_save.json";

        // -----------------------------
        // Save Entry Point
        // -----------------------------
        internal void SaveGame(GameTime gameTime)
        {
            try
            {
                var state = BuildSaveState();

                string json = JsonSerializer.Serialize(state, _saveOptions);
                AtomicWrite(DefaultSavePath, json);

                _messageManager.Show("Game saved successfully!", MessageType.Info);
            }
            catch (Exception ex)
            {
                _messageManager.Show($"Failed to save game: {ex.Message}", MessageType.Info);
            }
        }

        // -----------------------------
        // Load Entry Point
        // -----------------------------
        internal void LoadGame(GameTime gameTime)
        {
            try
            {
                if (!File.Exists(DefaultSavePath))
                {
                    _messageManager.Show("No saved game found", MessageType.Info);
                    return;
                }

                string json = File.ReadAllText(DefaultSavePath);
                GameStateData state = JsonSerializer.Deserialize<GameStateData>(json, _loadOptions);

                if (state == null)
                {
                    _messageManager.Show("Save file is empty or invalid.", MessageType.Info);
                    return;
                }

                RestoreFromSaveState(state);

                CurrentState = GameState.GalaxyMap;
                _messageManager.Show("Game loaded successfully!", MessageType.Info);
            }
            catch (Exception ex)
            {
                _messageManager.Show($"Failed to load game: {ex.Message}", MessageType.Info);
            }
        }

        // -----------------------------
        // Build Save State
        // -----------------------------
        private GameStateData BuildSaveState()
        {
            if (StarPositions == null) StarPositions = new List<Vector2>();

            return new GameStateData
            {
                // Metadata
                SaveVersion = 3,
                SavedAtUtc = DateTime.UtcNow,

                // UI / selection
                Turn = Turn,
                ScrollOffset = ScrollOffset,
                PosX = PosX,
                PosY = PosY,
                CurrentPlanet = CurrentPlanet,
                SelectedUnit = SelectedUnit,
                PossibleDestinations = new List<int>(PossibleDestinations ?? new List<int>()),
                confirmEndTurn = confirmEndTurn,
                confirmRecruit = confirmRecruit,
                recruitIndex = recruitIndex,
                hasRecruitedThisTurn = (bool[])hasRecruitedThisTurn.Clone(),
                confirmBuild = confirmBuild,
                buildReg = buildReg,
                buildImprovementIndex = buildImprovementIndex,
                confirmOccupy = confirmOccupy,
                occupyReg = occupyReg,
                chooseBuild = chooseBuild,
                chooseReg = chooseReg,

                // World
                Planets = Planets,
                Units = _unitManager._units,
                NextUnitId = _unitManager.NextUnitId,
                TurnActions = new List<TurnAction>(TurnActions ?? new List<TurnAction>()),
                RegionDatas = RegionDatas,
                PlanetRegionBonuses = new List<string>(PlanetRegionBonuses ?? new List<string>()),
                RegionBonusTypes = new List<RegionBonus>(_regionBonusManager.RegionBonusTypes ?? new List<RegionBonus>()),

                // Tech
                Techs = Techs,
                GlobalScience = GlobalScience,
                CurrentResearch = CurrentResearch,
                GlobalHabitatBonuses = GlobalHabitatBonuses ?? new List<HabitatBonus>(),

                // Visuals
                StarPositions = new List<Vector2>(StarPositions),

                // === SHIPS ===
                Ships = new List<Ship>(_shipManager.Ships ?? new List<Ship>()),
                NextShipId = _shipManager.NextShipId
            };
        }

        // -----------------------------
        // Restore from Save State
        // -----------------------------
        private void RestoreFromSaveState(GameStateData state)
        {
            // 1) Restore World Data (Planets, Units, etc. — preserves Name/Dimens/Status)
            Planets = state.Planets ?? new Planet[Constants.NUM_PLANETS];
            _unitManager._units = state.Units ?? new List<Unit>();
            TurnActions = state.TurnActions ?? new List<TurnAction>();
            PlanetRegionBonuses = state.PlanetRegionBonuses ?? new List<string>();

            // 2) Initialize static data (sets HabitatTypes, recreates managers, preserves Planets)
            InitializeBasicData();  // ← Now safe: doesn't overwrite Planets

            // 3) Regenerate RegionDatas & hex positions (critical for PlanetScreen!)
            RegionDatas = new RegionData[Constants.MAX_PLANET_DIMENS + 1];  // Fresh array
            Functions.GenHex(RegionDatas); // Rebuild hex positions

            // 4) Copy Neighbors
            if (GameData.NeighborsData != null)
            {
                if (Neighbors == null || Neighbors.Length != GameData.NeighborsData.Length)
                    Neighbors = new int[GameData.NeighborsData.Length];
                Array.Copy(GameData.NeighborsData, Neighbors, GameData.NeighborsData.Length);
            }

            // 5) Region Bonuses (after Planets restored)
            if (state.RegionBonusTypes != null && state.RegionBonusTypes.Count > 0)
            {
                _regionBonusManager.RegionBonusTypes.Clear();
                _regionBonusManager.RegionBonusTypes.AddRange(state.RegionBonusTypes);
            }
            else
            {
                _regionBonusManager.InitializeRegionBonuses();
            }

            // 6) UI/Runtime state
            Turn = state.Turn;
            ScrollOffset = state.ScrollOffset;
            PosX = state.PosX;
            PosY = state.PosY;
            CurrentPlanet = state.CurrentPlanet;
            SelectedUnit = state.SelectedUnit;
            PossibleDestinations = state.PossibleDestinations ?? new List<int>();
            confirmEndTurn = state.confirmEndTurn;
            confirmRecruit = state.confirmRecruit;
            recruitIndex = state.recruitIndex;
            hasRecruitedThisTurn = state.hasRecruitedThisTurn ?? new bool[Constants.NUM_PLANETS];
            confirmBuild = state.confirmBuild;
            buildReg = state.buildReg;
            buildImprovementIndex = state.buildImprovementIndex;
            confirmOccupy = state.confirmOccupy;
            occupyReg = state.occupyReg;
            chooseBuild = state.chooseBuild;
            chooseReg = state.chooseReg;

            Techs = state.Techs ?? GameData.Technologies.ToList();
            GlobalScience = state.GlobalScience;
            CurrentResearch = state.CurrentResearch;
            GlobalHabitatBonuses = state.GlobalHabitatBonuses ?? new List<HabitatBonus>();

            // 7) Starfield
            if (state.StarPositions != null && state.StarPositions.Count > 0)
            {
                StarPositions = new List<Vector2>(state.StarPositions);
            }
            else
            {
                StarPositions = new List<Vector2>();
                for (int i = 0; i < Constants.NUM_STARS; i++)
                {
                    StarPositions.Add(new Vector2(
                        Rand.Next(0, Textures[1].Width),
                        Rand.Next(Constants.TOP_BAR_HEIGHT, Textures[1].Height)
                    ));
                }
            }

            // 8) Apply tech bonuses to HabitatTypes
            foreach (var t in Techs)
            {
                if (t.IsResearched)
                {
                    foreach (var bonus in t.BonusUnlocks)
                    {
                        int idx = HabitatTypes.FindIndex(h => h.Name == bonus.Habitat);
                        if (idx >= 0)
                        {
                            if (bonus.FoodProd != 0) HabitatTypes[idx].FoodProd += bonus.FoodProd;
                            if (bonus.MatProd != 0) HabitatTypes[idx].MatProd += bonus.MatProd;
                            if (bonus.SciProd != 0) HabitatTypes[idx].SciProd += bonus.SciProd;
                            if (bonus.EnergyProd != 0) HabitatTypes[idx].EnergyProd += bonus.EnergyProd;
                        }
                    }
                }
            }

            // 9) Restore ships and units (managers already recreated in InitializeBasicData)
            _shipManager.SetShipsAndId(state.Ships, state.NextShipId);
            _unitManager.SetUnitsAndId(state.Units, state.NextUnitId);

            // 10) Clamp scroll offsets
            ScrollOffset.X = MathHelper.Clamp(ScrollOffset.X, 0,
                Textures.ContainsKey(1) ? Textures[1].Width - Constants.SCREEN_WIDTH : 0);
            float maxScrollY = Math.Max(0, (Textures.ContainsKey(1) ? Textures[1].Height : 0) - (Constants.SCREEN_HEIGHT - Constants.TOP_BAR_HEIGHT));
            ScrollOffset.Y = MathHelper.Clamp(ScrollOffset.Y, 0, maxScrollY);
            PosX = (int)ScrollOffset.X;
            PosY = (int)ScrollOffset.Y;
        }

        // -----------------------------
        // Atomic File Write
        // -----------------------------
        private static void AtomicWrite(string path, string content)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, content);
            if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
        }
    }
}