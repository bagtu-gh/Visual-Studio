using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using BroadenHorizons.Screens;

namespace BroadenHorizons
{

    public struct RegionData
    {
        public float XC { get; set; }
        public float YC { get; set; }
    }

    public class TurnAction
    {
        public int ActionTurn { get; set; }
        public int TurnFinal { get; set; }
        public int PlanetCode { get; set; }
        public int UnitID { get; set; }
        public UnitActionType UnitActionType { get; set; }
        public int TargetReg { get; set; } = -1;
        public int ImprovementIndex { get; set; } = -1;
    }

    public partial class BH : Game
    {
        internal GraphicsDeviceManager _graphics;
        internal SpriteBatch _spriteBatch;
        internal int PosX, PosY;
        internal int MouseMapX, MouseMapY;
        internal TopBarRenderer _topBar;
        internal int Turn = 1;
        internal RegionData[] RegionDatas = new RegionData[37];
        public Planet[] Planets = new Planet[Constants.NUM_PLANETS];
        public List<TurnAction> TurnActions = new List<TurnAction>();
        internal List<HabitatType> HabitatTypes = new List<HabitatType>();
        internal List<UnitType> UnitTypes = new List<UnitType>();
        internal List<PlanetImprovement> PlanetImprovements = new List<PlanetImprovement>();
        internal List<ShipType> Ships = new List<ShipType>();
        internal List<string> PlanetNames = new List<string>();
        internal int[] Neighbors = new int[222];
        internal List<string> PlanetRegionBonuses = new List<string>();
        internal RegionBonusManager _regionBonusManager;
        internal ShipManager _shipManager;
        internal UnitManager _unitManager;
        internal ProductionManager _productionManager;
        internal TechManager _techManager;
        internal MessageManager _messageManager;
        internal EndTurnManager _endTurnManager;
        internal EventManager _eventManager;
        internal Vector2 ScrollOffset = Vector2.Zero;
        internal enum GameState { MainMenu, Preferences, GalaxyMap, PlanetScreen, PlanetList, TechTree, ShipList }
        internal GameState CurrentState = GameState.MainMenu;
        internal GameState PrevState = GameState.MainMenu;
        internal int CurrentPlanet = -1;
        internal Random Rand = new Random();
        internal Dictionary<int, Texture2D> Textures = new Dictionary<int, Texture2D>();
        internal BitmapFont _bitmapFont;
        internal BitmapFont _bitmapFontBig;
        internal BitmapFont _bitmapFontTooltip;
        internal BitmapFont _bitmapFontMessages;
        internal Texture2D _pixel;
        internal Texture2D _logoTexture;
        public int SelectedUnit = -1;
        public List<int> PossibleDestinations = new List<int>();
        internal bool confirmEndTurn = false;
        internal List<Vector2> StarPositions = new List<Vector2>();
        internal bool confirmRecruit = false;
        internal int recruitIndex = -1;
        public bool[] hasRecruitedThisTurn = new bool[Constants.NUM_PLANETS];
        internal bool confirmBuild = false;
        internal int buildReg = -1;
        internal int buildImprovementIndex = -1;
        internal bool confirmOccupy = false;
        internal int occupyReg = -1;
        internal bool chooseBuild = false;
        internal int chooseReg = -1;
        public List<int> availableImprovementIndices = new List<int>();
        public List<int> availableUnitIndices = new List<int>();
        internal KeyboardState _prevKeyboard;
        internal MouseState _prevMouse;
        public Vector2 mousePos;
        public float Texturescale;
        public bool requireMouseRelease = false;
        public string tooltipText = "";
        public Vector2 tooltipPos = Vector2.Zero;
        internal List<Tech> Techs;
        private int _globalScience = Constants.STARTING_SCIENCE;
        private int _currentResearch = -1;
        private List<HabitatBonus> _globalHabitatBonuses = new List<HabitatBonus>();

        // Properties for tech state
        internal int GlobalScience
        {
            get => _techManager?.GlobalScience ?? _globalScience;
            set { if (_techManager != null) _techManager.GlobalScience = value; else _globalScience = value; }
        }

        internal int CurrentResearch
        {
            get => _techManager?.CurrentResearch ?? _currentResearch;
            set { if (_techManager != null) _techManager.CurrentResearch = value; else _currentResearch = value; }
        }

        internal List<HabitatBonus> GlobalHabitatBonuses
        {
            get => _techManager?.GlobalHabitatBonuses ?? _globalHabitatBonuses;
            set { if (_techManager != null) _techManager.GlobalHabitatBonuses = value; else _globalHabitatBonuses = value ?? new List<HabitatBonus>(); }
        }
        internal int hoveredTech = -1;
        private readonly Dictionary<GameState, Action<GameTime, KeyboardState, MouseState>> updateHandlers;
        private readonly Dictionary<GameState, Action<GameTime>> drawHandlers;
        internal PlanetScreen _planetScreen;
        internal GalaxyMapScreen _galaxyMapScreen;
        internal MainMenuScreen _mainMenuScreen;
        internal TechTreeScreen _techTreeScreen;
        internal PlanetListScreen _planetListScreen;
        internal ShipListScreen _shipListScreen;
        internal PreferencesScreen _preferencesScreen;
        internal Dictionary<int, Vector2> _planetNameSizeCache = new Dictionary<int, Vector2>();

        public BH()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = Constants.SCREEN_WIDTH;
            _graphics.PreferredBackBufferHeight = Constants.SCREEN_HEIGHT;
            //_graphics.IsFullScreen = true;
            _regionBonusManager = new RegionBonusManager(this, Rand, new List<HabitatType>());

            updateHandlers = new Dictionary<GameState, Action<GameTime, KeyboardState, MouseState>>
            {
            };
            drawHandlers = new Dictionary<GameState, Action<GameTime>>
            {
            };

            _planetScreen = new PlanetScreen(this);
            _galaxyMapScreen = new GalaxyMapScreen(this);
            _mainMenuScreen = new MainMenuScreen(this);
            _techTreeScreen = new TechTreeScreen(this);
            _planetListScreen = new PlanetListScreen(this);
            _shipListScreen = new ShipListScreen(this);
            _preferencesScreen = new PreferencesScreen(this);
            _messageManager = new MessageManager(this);
            _endTurnManager = new EndTurnManager(this);
            _eventManager = new EventManager(this);
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _bitmapFont = BitmapFont.FromFile(GraphicsDevice, "Content/fonts/font.fnt");
            _bitmapFontMessages = BitmapFont.FromFile(GraphicsDevice, "Content/fonts/fontmsg.fnt");
            _bitmapFontTooltip = BitmapFont.FromFile(GraphicsDevice, "Content/fonts/fonttooltip.fnt");
            _bitmapFontBig = BitmapFont.FromFile(GraphicsDevice, "Content/fonts/fontbig.fnt");

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _logoTexture = Content.Load<Texture2D>("images/logoBH");
            Textures[1] = Content.Load<Texture2D>("images/Galaxy");
            Textures[2] = Content.Load<Texture2D>("images/planet_transp");
            Textures[5] = Content.Load<Texture2D>("images/Unexplored");
            Textures[6] = Content.Load<Texture2D>("images/Food");
            Textures[7] = Content.Load<Texture2D>("images/Materials");
            Textures[8] = Content.Load<Texture2D>("images/Science");
            Textures[9] = Content.Load<Texture2D>("images/Energy");

            Textures[10] = Content.Load<Texture2D>("images/City");
            Textures[11] = Content.Load<Texture2D>("images/Forest");
            Textures[12] = Content.Load<Texture2D>("images/Ocean");
            Textures[13] = Content.Load<Texture2D>("images/Mountains");
            Textures[14] = Content.Load<Texture2D>("images/Prairie");
            Textures[15] = Content.Load<Texture2D>("images/Valley");
            Textures[16] = Content.Load<Texture2D>("images/Desert");
            Textures[17] = Content.Load<Texture2D>("images/Ruins");
            Textures[18] = Content.Load<Texture2D>("images/Volcano");
            Textures[19] = Content.Load<Texture2D>("images/Arctic");

            Textures[30] = Content.Load<Texture2D>("images/Explorer");
            Textures[31] = Content.Load<Texture2D>("images/Farmer");
            Textures[32] = Content.Load<Texture2D>("images/Miner");
            Textures[33] = Content.Load<Texture2D>("images/Scientist");
            Textures[34] = Content.Load<Texture2D>("images/Builder");
            Textures[35] = Content.Load<Texture2D>("images/Harvester");
            Textures[36] = Content.Load<Texture2D>("images/Fisher");
            Textures[37] = Content.Load<Texture2D>("images/Colonist");

            for (int i = 40; i <= 50; i++)
            {
                try
                {
                    string textureName = $"images/Resource{i - 39}";
                    Textures[i] = Content.Load<Texture2D>(textureName);
                }
                catch
                {
                    Textures[i] = _regionBonusManager.CreateRegionBonusCircleTexture(i, GraphicsDevice);
                }
            }

            // Load ship textures
            Textures.Add(61, Content.Load<Texture2D>("images/Probe")); // Explorer ship
            Textures.Add(62, Content.Load<Texture2D>("images/ColonyShip")); // Colony ship
            Textures.Add(63, Content.Load<Texture2D>("images/Freighter")); // Freighter
            Textures.Add(64, Content.Load<Texture2D>("images/Farmer")); // Terraformer

            _topBar = new TopBarRenderer(_bitmapFont, Textures, _pixel);
        }

        protected override void UnloadContent()
        {
            _pixel.Dispose();
            base.UnloadContent();
        }

        private void InitializeBasicData()
        {
            ResetGameData();

            HabitatTypes = [.. GameData.HabitatTypes];
            UnitTypes = [.. GameData.UnitTypes];
            PlanetImprovements = [.. GameData.PlanetImprovements];
            Ships = [.. GameData.ShipTypes];
            Techs = [.. GameData.Technologies];
            GameData.AssignTechPositions();

            if (Planets == null || Planets.Length != Constants.NUM_PLANETS)
            {
                Planets = new Planet[Constants.NUM_PLANETS];
            }

            for (int i = 0; i < Constants.NUM_PLANETS; i++)
            {
                if (Planets[i] == null)
                    Planets[i] = new Planet();

                // Reset lists/arrays that depend on planet size
                Planets[i].Habitat = [.. Enumerable.Repeat(Constants.NON_EXISTING_HABTITAT, 37)];
                Planets[i].HabitatPopulated = [.. Enumerable.Repeat(false, 37)];
                Planets[i].Improvements = [.. Enumerable.Repeat(-1, 37)];
                Planets[i].OccupiedByUnit = [.. Enumerable.Repeat(-1, 37)];
                Planets[i].RegionBonusRegions = [.. Enumerable.Repeat(-1, 37)];
                Planets[i].RegionBonuses = [];
            }

            for (int i = 0; i < RegionDatas.Length; i++)
                RegionDatas[i] = new RegionData();

            _regionBonusManager.UpdateHabitats(HabitatTypes);
            _unitManager = new UnitManager(this, UnitTypes, _messageManager);
            _shipManager = new ShipManager(Planets, Techs, _messageManager, TurnActions);
            _productionManager = new ProductionManager(Planets, HabitatTypes, UnitTypes, PlanetImprovements, _regionBonusManager, _unitManager, _shipManager);
            _techManager = new TechManager(Techs, Constants.STARTING_SCIENCE, _messageManager, HabitatTypes);
        }

        private void InitializeData()
        {
            // Initialize basic structures (habitats, units, improvements, etc.)
            InitializeBasicData();

            // Initialize region bonuses through RegionBonusManager
            _regionBonusManager.InitializeRegionBonuses();

            // Populate PlanetRegionBonuses with names for galaxy view compatibility
            PlanetRegionBonuses.Clear();
            foreach (var bonus in _regionBonusManager.RegionBonusTypes)
            {
                PlanetRegionBonuses.Add(bonus.Name);
            }

            PlanetNames.AddRange(GameData.PlanetNames);

            Array.Copy(GameData.NeighborsData, Neighbors, GameData.NeighborsData.Length);

            Functions.GenHex(RegionDatas);
        }

        private void ResetGameData()
        {
            HabitatTypes.Clear();
            UnitTypes.Clear();
            PlanetImprovements.Clear();
            PlanetRegionBonuses.Clear();
            TurnActions.Clear();
            PlanetNames.Clear();
        }

        internal void NewGame()
        {
            // One authoritative init for new session
            InitializeData();

            // Reset UI/runtime flags
            Turn = 1;
            ScrollOffset = Vector2.Zero;
            PosX = 0; PosY = 0;
            CurrentPlanet = -1;
            SelectedUnit = -1;
            PossibleDestinations.Clear();
            confirmEndTurn = confirmRecruit = confirmBuild = confirmOccupy = chooseBuild = false;
            recruitIndex = buildReg = buildImprovementIndex = occupyReg = chooseReg = -1;
            availableImprovementIndices.Clear();

            // World defaults not done by InitializeData:
            hasRecruitedThisTurn = new bool[Constants.NUM_PLANETS];
            for (int i = 0; i < Constants.NUM_PLANETS; i++)
            {
                hasRecruitedThisTurn[i] = false;
                if (i != 0) Planets[i].Status = PlanetStatus.Unexplored; /*TEMPORARY
                if (i >= 4)
                    Planets[i].Status = PlanetStatus.Unexplored;
                else
                    Planets[i].Status = PlanetStatus.Explored;
                if (i == 1)
                {
                    Planets[i].Status = PlanetStatus.Owned;
                    Planets[i].Habitat[0] = 0;
                    Planets[i].Food = Constants.STARTING_FOOD;
                    Planets[i].Mat = Constants.STARTING_MATERIALS;
                    Planets[i].Energy = Constants.STARTING_ENERGY;
                    Planets[i].Population = Constants.STARTING_POPULATION;
                    Planets[i].HabitatPopulated[0] = true;
                }*/
            }

            // Stars
            StarPositions = new List<Vector2>();
            for (int i = 0; i < Constants.NUM_STARS; i++)
            {
                StarPositions.Add(new Vector2(
                    Rand.Next(0, Textures[1].Width),
                    Rand.Next(Constants.TOP_BAR_HEIGHT, Textures[1].Height)
                ));
            }

            // Planets
            var galaxyTexture = Textures[1];
            var points = PlanetGenerator.GenerateSpacedPoints(
                galaxyTexture.Width, galaxyTexture.Height,
                Constants.MIN_PLANET_DISTANCE, Constants.NUM_PLANETS,
                Constants.MAX_PLANET_ATTEMPTS, Rand);

            for (int i = 0; i < points.Count; i++)
            {
                PlanetGenerator.CreatePlanet(
                    Planets[i], (int)points[i].X, (int)points[i].Y,
                    PlanetNames, HabitatTypes, Neighbors, Rand,
                    _regionBonusManager, i == 0);
            }

            if (points.Count < Constants.NUM_PLANETS)
            {
                Debug.WriteLine($"Only {points.Count} planets were generated (out of {Constants.NUM_PLANETS}).");
            }

            // Cache planet name sizes once per new game
            _planetNameSizeCache.Clear();
            for (int i = 0; i < Constants.NUM_PLANETS && i < Planets.Length; i++)
            {
                if (!string.IsNullOrEmpty(Planets[i].Name))
                {
                    _planetNameSizeCache[i] = _bitmapFont.MeasureString(Planets[i].Name);
                }
                else
                {
                    _planetNameSizeCache[i] = Vector2.Zero;
                }
            }

            // Starting units/ships on home planet
            _unitManager.StartingUnits(0);
            _shipManager.StartingShips(0);

            // Camera center & clamp
            ScrollOffset = new Vector2(
                Planets[0].XPos - Constants.SCREEN_WIDTH / 2f,
                Planets[0].YPos - Constants.SCREEN_HEIGHT / 2f
            );
            ScrollOffset.X = MathHelper.Clamp(ScrollOffset.X, 0, Textures[1].Width - Constants.SCREEN_WIDTH);
            ScrollOffset.Y = MathHelper.Clamp(ScrollOffset.Y, 0, Textures[1].Height - Constants.SCREEN_HEIGHT);
            PosX = (int)ScrollOffset.X;
            PosY = (int)ScrollOffset.Y;

            CurrentState = GameState.GalaxyMap;
        }

        protected override void Update(GameTime gameTime)
        {
            if (IsActive)
            {
                var keyboard = Keyboard.GetState();
                var mouse = Mouse.GetState();
                mousePos = mouse.Position.ToVector2();
                MouseMapX = (int)(mouse.X + ScrollOffset.X);
                MouseMapY = (int)(mouse.Y + ScrollOffset.Y);

                if (_messageManager.IsActive)
                {
                    _messageManager.Update(gameTime, keyboard, mouse, _bitmapFont, new Rectangle(0, 0, Constants.SCREEN_WIDTH, Constants.SCREEN_HEIGHT));
                }
                else
                {
                    if (CurrentState == GameState.PlanetScreen)
                    {
                        _planetScreen.Update(gameTime, keyboard, mouse);
                    }
                    else if (CurrentState == GameState.GalaxyMap)
                    {
                        _galaxyMapScreen.Update(gameTime, keyboard, mouse);
                    }
                    else if (CurrentState == GameState.MainMenu)
                    {
                        _mainMenuScreen.Update(gameTime, keyboard, mouse);
                    }
                    else if (CurrentState == GameState.Preferences)
                    {
                        _preferencesScreen.Update(gameTime, keyboard, mouse);
                    }
                    else if (CurrentState == GameState.TechTree)
                    {
                        _techTreeScreen.Update(gameTime, keyboard, mouse);
                    }
                    else if (CurrentState == GameState.PlanetList)
                    {
                        _planetListScreen.Update(gameTime, keyboard, mouse);
                    }
                    else if (CurrentState == GameState.ShipList)
                    {
                        _shipListScreen.Update(gameTime, keyboard, mouse);
                    }
                    else
                    {
                        updateHandlers[CurrentState](gameTime, keyboard, mouse);
                    }
                }

                _prevKeyboard = keyboard;
                _prevMouse = mouse;
            }
            base.Update(gameTime);
        }

        public bool WasKeyDown(Keys key)
        {
            return _prevKeyboard.IsKeyDown(key);
        }

        public string GetProductionTooltip(int planetIndex, string productionType)
        {
            return _productionManager?.GetProductionTooltip(planetIndex, productionType) ?? "";
        }

        public string GetRegTooltip(int planetIndex, int regIndex)
        {
            return _productionManager?.GetRegTooltip(planetIndex, regIndex) ?? "";
        }

        public string GetPopulationTooltip(int planetIndex)
        {
            var planet = Planets[planetIndex];
            var tooltipLines = new List<string>{
                "Summary:"
            };

            bool hasExplored = false;
            for (int i = 0; i <= planet.Dimens; i++)
            {
                if (planet.Habitat[i] >= 0)
                {
                    hasExplored = true;
                    string habitatName = GameData.HabitatTypes[planet.Habitat[i]].Name;
                    if (planet.HabitatPopulated[i])
                    {
                        tooltipLines.Add($"Region {i}: {habitatName} ({GameData.HabitatTypes[planet.Habitat[i]].PopNeeded})");
                    }
                    else
                    {
                        tooltipLines.Add($"Region {i}: {habitatName} (0, requires {GameData.HabitatTypes[planet.Habitat[i]].PopNeeded})");
                    }
                }
            }

            if (!hasExplored)
            {
                tooltipLines.Add("No explored regions yet.");
            }
            var dataList = Functions.GetTemperatureRangeData(planet.Temperature);
            tooltipLines.Add($"Modifiers:\nBase: {Constants.POPULATION_BASE_GROWTH:P0}\nTemp factor: {(float)dataList["Modifier"]:P0}");
            tooltipLines.Add($"Food factor: {1 + double.Parse(CalculateResourceTurn(planetIndex, "Food")) * Constants.POPULATION_FOOD_GROWTH:P0}");

            return string.Join("\n", tooltipLines);
        }

        public string BuildGlobalProductionTooltip(string productionType)
        {
            return _productionManager?.BuildGlobalProductionTooltip(productionType) ?? "";
        }

        public string CalculateResourceTurn(int planetIndex, string productionType)
        {
            return _productionManager?.CalculateResourceTurn(planetIndex, productionType) ?? "0";
        }

        internal void HandleTechClick(int techId)
        {
            hoveredTech = -1;
            _techManager?.HandleTechClick(techId);
        }

        internal void EndTurn(GameTime gameTime)
        {
            _endTurnManager.EndTurn(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            if (CurrentState == GameState.PlanetScreen)
            {
                _planetScreen.Draw(gameTime);
            }
            else if (CurrentState == GameState.GalaxyMap)
            {
                _galaxyMapScreen.Draw(gameTime);
            }
            else if (CurrentState == GameState.MainMenu)
            {
                _mainMenuScreen.Draw(gameTime);
            }
            else if (CurrentState == GameState.Preferences)
            {
                _preferencesScreen.Draw(gameTime);
            }
            else if (CurrentState == GameState.TechTree)
            {
                _techTreeScreen.Draw(gameTime);
            }
            else if (CurrentState == GameState.PlanetList)
            {
                _planetListScreen.Draw(gameTime);
            }
            else if (CurrentState == GameState.ShipList)
            {
                _shipListScreen.Draw(gameTime);
            }
            else
            {
                drawHandlers[CurrentState](gameTime);
            }
            _messageManager.Draw(_spriteBatch, _pixel, _bitmapFontMessages, Constants.SCREEN_WIDTH, Constants.SCREEN_HEIGHT, graphicsDevice: GraphicsDevice);
            _spriteBatch.End();
            base.Draw(gameTime);
        }

    }
}