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

    public enum UnitActionType
    {
        None = 0,
        Building = 1,
        Recruiting = 2,
        MovingOrExploring = 3
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
        internal Dictionary<float, Vector2[]> hexPointCache = new Dictionary<float, Vector2[]>();
        internal KeyboardState _prevKeyboard;
        internal MouseState _prevMouse;
        public Vector2 mousePos;
        public float Texturescale;
        public bool requireMouseRelease = false;
        public string tooltipText = "";
        public Vector2 tooltipPos = Vector2.Zero;
        internal MessageManager messageManager;
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
            messageManager = new MessageManager(this);
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

            // Initialize Planets and RegionDatas if not loaded from save
            if (Planets == null || Planets.Length == 0 || Planets[0] == null)
            {
                Planets = new Planet[Constants.NUM_PLANETS];
                for (int i = 0; i < Planets.Length; i++)
                {
                    Planets[i] = new Planet();
                    for (int j = 0; j < 37; j++)
                    {
                        Planets[i].Habitat[j] = Constants.NON_EXISTING_HABTITAT;
                        Planets[i].RegionBonusRegions[j] = -1;
                    }
                }
            }

            for (int i = 0; i < RegionDatas.Length; i++) RegionDatas[i] = new RegionData();

            _regionBonusManager.UpdateHabitats(HabitatTypes);
            _unitManager = new UnitManager(this, UnitTypes, messageManager);
            _shipManager = new ShipManager(Planets, Techs, messageManager, TurnActions);
            _productionManager = new ProductionManager(Planets, HabitatTypes, UnitTypes, PlanetImprovements, _regionBonusManager, _unitManager, _shipManager);
            _techManager = new TechManager(Techs, Constants.STARTING_SCIENCE, messageManager, HabitatTypes);
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
                Planets[i].Improvements = [.. Enumerable.Repeat(-1, Constants.MAX_PLANET_DIMENS + 1)];
                Planets[i].OccupiedByUnit = [.. Enumerable.Repeat(-1, Constants.MAX_PLANET_DIMENS + 1)];
                //if (i != 0) Planets[i].Status = PlanetStatus.Unexplored; TEMPORARY: all planets start as owned for testing
                if (i >= 4) 
                    Planets[i].Status = PlanetStatus.Unexplored;
                else
                    Planets[i].Status = PlanetStatus.Explored;
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

                if (messageManager.IsActive)
                {
                    messageManager.Update(gameTime, keyboard, mouse, _bitmapFont, new Rectangle(0, 0, Constants.SCREEN_WIDTH, Constants.SCREEN_HEIGHT));
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

            tooltipLines.Add($"Modifiers:\nBase: {Constants.POPULATION_BASE_GROWTH:P0}\nTemp factor: {double.Parse(Functions.GetTemperatureRangeData(planet.Temperature, "Modifier")):P0}");
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
            Turn++;
            for (int i = 0; i < Constants.NUM_PLANETS; i++)
            {
                hasRecruitedThisTurn[i] = false;
            }

            for (int p = 0; p < Constants.NUM_PLANETS; p++)
            {
                var production = _productionManager.CalculateProduction(p);
                Planets[p].Food += production.Food;
                Planets[p].Mat += production.Materials;
                Planets[p].Energy += production.Energy;
                Planets[p].Population += int.Parse(Functions.GetPopModifier(Planets[p], int.Parse(CalculateResourceTurn(p, "Food"))));
            }

            List<string> summary = new List<string>();
            int scienceProduced = int.Parse(_productionManager.CalculateProductionTurn(-1, "Science"));
            _techManager.ProcessTurnResearch(scienceProduced, summary, out var TechResearchedThisTurn);

            for (int i = TurnActions.Count - 1; i >= 0; i--)
            {
                var ta = TurnActions[i];
                var SelectedUnit = _unitManager.GetUnitById(ta.UnitID);
                if (Turn >= ta.TurnFinal)
                {
                    if (ta.UnitActionType == UnitActionType.Building)
                    {
                        Planets[ta.PlanetCode].Improvements[ta.TargetReg] = ta.ImprovementIndex;
                        var improvement = PlanetImprovements[ta.ImprovementIndex];
                        string planetName = Planets[ta.PlanetCode].Name;
                        string msg = $"Your builder has finished {improvement.Name} at {planetName},\nit will produce {improvement.FoodProd} food, {improvement.MatProd} materials, {improvement.SciProd} science,\na {improvement.AllowedUnit} can occupy the building to increase even more the production.";
                        summary.Add(msg);
                    }
                    else if (ta.UnitActionType == UnitActionType.Recruiting)
                    {
                        string unitName = SelectedUnit.Name;
                        string planetName = Planets[ta.PlanetCode].Name;
                        string msg = $"A new unit of {unitName} has been recruited\nat {planetName} and is now ready for action!";
                        summary.Add(msg);
                    }
                    else if (ta.UnitActionType == UnitActionType.MovingOrExploring)
                    {
                        int unitCode = SelectedUnit.TypeIndex;
                        string unitName = UnitTypes[unitCode].Name;
                        string planetName = Planets[ta.PlanetCode].Name;
                        int targetRegion = ta.TargetReg;

                        if (unitCode == (int)UnitTypeEnum.Explorer && Planets[ta.PlanetCode].Habitat[targetRegion] < 0)
                        {
                            // Explorer discovering new habitat
                            int hab = Planets[ta.PlanetCode].Habitat[targetRegion];
                            Planets[ta.PlanetCode].Habitat[targetRegion] = Math.Abs(hab);
                            var habitat = HabitatTypes[Math.Abs(hab)];
                            if (Functions.GetPlanetPopulation(Planets[ta.PlanetCode], "Unassigned") >= habitat.PopNeeded)
                            {
                                summary.Add($"A new habitat {habitat.Name} has been discovered at {planetName},\nit will yield {habitat.FoodProd} food, {habitat.MatProd} materials, and {habitat.SciProd} science.\nIt has been automatically populated with {habitat.PopNeeded} colonists\nYour explorer is now free to explore more!");
                                Planets[ta.PlanetCode].HabitatPopulated[targetRegion] = true;
                            } else {
                                summary.Add($"A new habitat {habitat.Name} has been discovered at {planetName},\nit would yield {habitat.FoodProd} food, {habitat.MatProd} materials, and {habitat.SciProd} science\nif populated with {habitat.PopNeeded} colonists that are not currently available.\nYour explorer is now free to explore more!");
                            }

                            int regionBonusIndex = Planets[ta.PlanetCode].RegionBonusRegions[targetRegion];
                            if (regionBonusIndex >= 0)
                            {
                                var regionBonus = _regionBonusManager.RegionBonusTypes[regionBonusIndex];
                                string regionBonusMsg = $"A {regionBonus.Name} was discovered in the {habitat.Name} at {planetName}!\n" +
                                                    $"It will provide +{regionBonus.BaseBonus} {regionBonus.BonusType} per turn.";
                                summary.Add(regionBonusMsg);
                            }
                        }
                        else
                        {
                            int habitatIndex = Planets[ta.PlanetCode].Habitat[targetRegion];
                            var habitat = HabitatTypes[habitatIndex];
                            string habitatName = habitat.Name;
                            string msg = $"{unitName} has arrived at {habitatName} (Region {targetRegion}) on {planetName}!\nThey are now ready to work or move again.";
                            summary.Add(msg);
                        }
                    }
                    SelectedUnit.Action = 0;
                    TurnActions.RemoveAt(i);
                }
            }

            var shipMessages = _shipManager.ProcessEndTurn(Turn);

            foreach (var msg in shipMessages)
            {
                summary.Add(msg);
            }

            // Check for tech tree actions
            if (_techManager?.HasAvailableTechs() ?? false && _techManager.CurrentResearch == -1)
            {
                summary.Add("Warning! No research in progress.\nVisit the Tech Tree to start a new research project.");
            }
            
            // Always show a message to confirm the turn has ended
            if (summary.Count > 0)
            {
                messageManager.Show($"Turn {Turn} Summary:\n\n{string.Join("\n\n", summary)}", MessageType.Info);
            }
            else
            {
                messageManager.Show($"Turn {Turn} completed. No actions finished this turn.", MessageType.Info);
            }
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
            messageManager.Draw(_spriteBatch, _pixel, _bitmapFontMessages, Constants.SCREEN_WIDTH, Constants.SCREEN_HEIGHT, graphicsDevice: GraphicsDevice);
            _spriteBatch.End();
            base.Draw(gameTime);
        }

    }
}