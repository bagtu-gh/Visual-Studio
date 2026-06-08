using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
            public bool[] hasRecruitedThisTurn;

            // World
            public Planet[] Planets;
            public List<Unit> Units { get; set; }
            public int NextID { get; set; } = 0;
            public List<TurnAction> TurnActions;
            public RegionData[] RegionDatas;
            public List<RegionBonus> RegionBonusTypes;
            public string TurnLogText { get; set; } = string.Empty;

            // Tech tree
            public List<Tech> Techs { get; set; }
            public int GlobalScience { get; set; }
            public int CurrentResearch { get; set; }
            public List<HabitatBonus> GlobalHabitatBonuses { get; set; }

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

        [Serializable]
        public class SaveSlotInfo
        {
            public int SlotIndex { get; set; }
            public bool Exists { get; set; }
            public int Turn { get; set; }
            public DateTime SavedAtUtc { get; set; }
            public string FilePath { get; set; } = string.Empty;
        }

        internal static string GetSaveSlotPath(int slotIndex)
        {
            return $"game_save_slot{slotIndex + 1}.json";
        }

        internal List<SaveSlotInfo> GetSaveSlotInfos()
        {
            return Enumerable.Range(0, Constants.MAX_SAVE_SLOTS)
                .Select(GetSaveSlotInfo)
                .ToList();
        }

        internal SaveSlotInfo GetSaveSlotInfo(int slotIndex)
        {
            var info = new SaveSlotInfo
            {
                SlotIndex = slotIndex,
                FilePath = GetSaveSlotPath(slotIndex),
                Exists = File.Exists(GetSaveSlotPath(slotIndex))
            };

            if (!info.Exists)
            {
                return info;
            }

            try
            {
                string json = File.ReadAllText(info.FilePath);
                var state = JsonSerializer.Deserialize<GameStateData>(json, _loadOptions);
                if (state != null)
                {
                    info.Turn = state.Turn;
                    info.SavedAtUtc = state.SavedAtUtc;
                }
                else
                {
                    info.Exists = false;
                }
            }
            catch
            {
                info.Exists = false;
            }

            return info;
        }

        internal void SaveGameToSlot(int slotIndex, GameTime gameTime)
        {
            try
            {
                var state = BuildSaveState();
                string json = JsonSerializer.Serialize(state, _saveOptions);
                AtomicWrite(GetSaveSlotPath(slotIndex), json);

                _messageManager.Show($"Game saved to slot {slotIndex + 1}.", MessageType.Info);
            }
            catch (Exception ex)
            {
                _messageManager.Show($"Failed to save game: {ex.Message}", MessageType.Info);
            }
        }

        internal void LoadGameFromSlot(int slotIndex, GameTime gameTime)
        {
            try
            {
                string path = GetSaveSlotPath(slotIndex);
                if (!File.Exists(path))
                {
                    _messageManager.Show("This save slot is empty.", MessageType.Info);
                    return;
                }

                string json = File.ReadAllText(path);
                GameStateData state = JsonSerializer.Deserialize<GameStateData>(json, _loadOptions);

                if (state == null)
                {
                    _messageManager.Show("Save file is empty or invalid.", MessageType.Info);
                    return;
                }

                RestoreFromSaveState(state);

                CurrentState = GameState.GalaxyMap;
                _messageManager.Show($"Loaded game from slot {slotIndex + 1}.", MessageType.Info);
            }
            catch (Exception ex)
            {
                _messageManager.Show($"Failed to load game: {ex.Message}", MessageType.Info);
            }
        }

        // -----------------------------
        // Save/Load for numbered save slots only
        // -----------------------------

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
                Turn = Constants.TURN,
                ScrollOffset = ScrollOffset,
                PosX = PosX,
                PosY = PosY,
                CurrentPlanet = CurrentPlanet,
                hasRecruitedThisTurn = (bool[])hasRecruitedThisTurn.Clone(),

                // World
                Planets = Planets,
                Units = _unitManager._units,
                NextID = Constants.NEXT_ID,
                TurnActions = new List<TurnAction>(TurnActions ?? new List<TurnAction>()),
                RegionDatas = RegionDatas,
                RegionBonusTypes = new List<RegionBonus>(_regionBonusManager.RegionBonusTypes ?? new List<RegionBonus>()),
                TurnLogText = File.Exists(Constants.TURN_LOG_FILE) ? File.ReadAllText(Constants.TURN_LOG_FILE) : string.Empty,

                // Tech
                Techs = Techs,
                GlobalScience = _techManager.GlobalScience,
                CurrentResearch = _techManager.CurrentResearch,
                GlobalHabitatBonuses = _techManager.GlobalHabitatBonuses ?? new List<HabitatBonus>(),

                // Visuals
                StarPositions = new List<Vector2>(StarPositions),

                // === SHIPS ===
                Ships = new List<Ship>(_shipManager.Ships ?? new List<Ship>()),
            };
        }

        // -----------------------------
        // Restore from Save State
        // -----------------------------
        private void RestoreFromSaveState(GameStateData state)
        {
            // 1) Restore World Data (Planets, TurnActions, etc. — preserves Name/Dimens/Status)
            Planets = state.Planets ?? new Planet[Constants.NUM_PLANETS];
            TurnActions = state.TurnActions ?? new List<TurnAction>();

            // 2) Initialize static data (sets HabitatTypes, recreates managers, preserves Planets)
            InitializeBasicData(clearTurnLog: false, clearTurnActions: false, resetPlanetData: false);  // ← preserve loaded Planets data and TurnActions during restore

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
            Constants.TURN = state.Turn;
            ScrollOffset = state.ScrollOffset;
            PosX = state.PosX;
            PosY = state.PosY;
            CurrentPlanet = state.CurrentPlanet;
            hasRecruitedThisTurn = state.hasRecruitedThisTurn ?? new bool[Constants.NUM_PLANETS];

            Techs = state.Techs ?? GameData.Technologies.ToList();
            _techManager.GlobalScience = state.GlobalScience;
            _techManager.CurrentResearch = state.CurrentResearch;
            _techManager.GlobalHabitatBonuses = state.GlobalHabitatBonuses ?? new List<HabitatBonus>();

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

            // 8) Restore turn log file
            if (!string.IsNullOrEmpty(state.TurnLogText))
            {
                File.WriteAllText(Constants.TURN_LOG_FILE, state.TurnLogText);
            }
            else if (File.Exists(Constants.TURN_LOG_FILE))
            {
                File.Delete(Constants.TURN_LOG_FILE);
            }

            // 9) Restore ships and units (managers already recreated in InitializeBasicData)
            _shipManager.SetShipsAndId(state.Ships);
            _unitManager.SetUnitsAndId(state.Units);
            Constants.NEXT_ID = state.NextID;

            // 9) Clamp scroll offsets
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