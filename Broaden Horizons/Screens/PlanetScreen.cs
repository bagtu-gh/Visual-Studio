using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using MonoGame.Extended.BitmapFonts;

namespace BroadenHorizons.Screens
{
    public class PlanetScreen
    {
        private BH _game;
        private bool showRegionNumber = true;

        public PlanetScreen(BH game)
        {
            _game = game;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            if (_game.requireMouseRelease)
            {
                if (mouse.LeftButton == ButtonState.Released)
                {
                    _game.requireMouseRelease = false;
                }
            }
            else
            {
                // Tooltip handling for resources and regions
                _game.tooltipText = "";
                _game.tooltipPos = _game.mousePos + new Vector2(10, 10); // Slightly offset from mouse

                // Handle top bar tooltips
                if (_game._topBar.HandleTopBarTooltips(TopBarRenderer.TopBarMode.Planet, _game.mousePos, _game.Turn, _game.GlobalScience, _game.Planets, _game.CalculateResourceTurn, _game.GetProductionTooltip, null, _game.GetPopulationTooltip, _game.CurrentPlanet, out string tt, out Vector2 tp))
                {
                    _game.tooltipText = tt;
                    _game.tooltipPos = tp;
                }
                else
                {
                    // Check for mouseover on explored regions
                    int clickedReg = Functions.GetClickedReg(_game.RegionDatas, (int)_game.mousePos.X, (int)_game.mousePos.Y);
                    if (clickedReg != -1 && _game.Planets[_game.CurrentPlanet].Habitat[clickedReg] >= 0)
                    {
                        _game.tooltipText = _game.GetRegTooltip(_game.CurrentPlanet, clickedReg);
                    }
                }

                if (keyboard.IsKeyDown(Keys.Escape) && !_game.WasKeyDown(Keys.Escape))
                {
                    _game.CurrentState = BH.GameState.GalaxyMap;
                    _game.CurrentPlanet = -1;
                    _game.SelectedUnit = -1;
                    _game.PossibleDestinations.Clear();
                    _game.confirmRecruit = false;
                    _game.recruitIndex = -1;
                    _game.confirmBuild = false;
                    _game.buildReg = -1;
                    _game.buildImprovementIndex = -1;
                    _game.confirmOccupy = false;
                    _game.occupyReg = -1;
                    _game.chooseBuild = false;
                    _game.chooseReg = -1;
                    _game.availableImprovementIndices.Clear();
                }

                if (keyboard.IsKeyDown(Keys.Tab) && !_game.WasKeyDown(Keys.Tab))
                {
                    showRegionNumber = !showRegionNumber;
                }

                if (mouse.LeftButton == ButtonState.Pressed && _game._prevMouse.LeftButton == ButtonState.Released)
                {
                    var unitsOnPlanet = _game._unitManager.GetUnitsOnPlanet(_game.CurrentPlanet);
                    var shipsOnPlanet = _game._shipManager.GetShipsOnPlanet(_game.CurrentPlanet);
                    //Available menu clicked
                    if (mouse.X >= Constants.UNIT_MENU_MIN_X && mouse.X <= Constants.UNIT_MENU_MAX_X)
                    {
                        //Unit or ship clicked
                        int totalUnits = unitsOnPlanet.Count;
                        int totalShips = shipsOnPlanet.Count;
                        for (int i = 0; i < totalUnits + totalShips; i++)
                        {
                            if (mouse.Y >= Constants.UNIT_MENU_MIN_Y + 90 * i && mouse.Y <= Constants.UNIT_MENU_MAX_Y + 90 * i)
                            {
                                //Unit clicked
                                if (i < totalUnits)
                                {
                                    _game.SelectedUnit = -1;
                                    _game.PossibleDestinations.Clear();
                                    _game._unitManager.HandleUnitClicked(
                                        unitsOnPlanet[i],
                                        _game.CurrentPlanet,
                                        _game.TurnActions,
                                        _game.UnitTypes,
                                        _game.PlanetImprovements,
                                        _game.Planets[_game.CurrentPlanet],
                                        _game.Neighbors,
                                        _game.HabitatTypes,
                                        ref _game.SelectedUnit,
                                        ref _game.PossibleDestinations,
                                        _game.messageManager
                                    );
                                    break;
                                }
                                else //Ship clicked
                                {
                                    _game.SelectedUnit = -1;
                                    _game.PossibleDestinations.Clear();
                                    Ship ship = shipsOnPlanet[i - totalUnits];
                                    if (ship.TypeIndex == (int)ShipTypeEnum.Probe && ship.Status == ShipStatus.Docked)
                                    {
                                        _game._shipManager.ShowProbeLaunchMenu(ship, _game.Turn);
                                        break;
                                    }
                                    else if (ship.TypeIndex == (int)ShipTypeEnum.ColonyShip && ship.Status == ShipStatus.Docked)
                                    {
                                        if(unitsOnPlanet.Exists(u => u.TypeIndex == (int)UnitTypeEnum.Colonist))
                                        {
                                            //_game._shipManager.ShowColonyLaunchMenu(ship, _game.Turn);
                                            break;
                                        }
                                        else
                                        {
                                            _game.messageManager.Show("You don't have a colonist on this planet", MessageType.Info);
                                            break;
                                        }
                                    }
                                    else if (GameData.ShipTypes[ship.TypeIndex].Type == ShipTypeEnum.Freighter && ship.Status == ShipStatus.Docked)
                                    {
                                        _game._shipManager.ShowFreighterLaunchMenu(ship, _game.Turn);
                                    }
                                    else if (GameData.ShipTypes[ship.TypeIndex].Type == ShipTypeEnum.Terraformer && ship.Status == ShipStatus.Docked)
                                    {
                                        _game._shipManager.ShowTerraformerLaunchMenu(ship, _game.Turn);
                                    }
                                    else if (ship.Status == ShipStatus.InTransit)
                                    {
                                        _game.messageManager.Show($"Your {ship.Name} is travelling to {_game.Planets[ship.TargetPlanet].Name}", MessageType.Info);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    //Recruit menu clicked
                    else if (mouse.X >= Constants.RECRUIT_MENU_X && mouse.X <= Constants.RECRUIT_MENU_X + 200)
                    {
                        int index = (mouse.Y - Constants.RECRUIT_MENU_Y) / Constants.RECRUIT_HEIGHT;
                        //Available units to recruit
                        _game.availableUnitIndices.Clear();
                        _game.availableUnitIndices = UnitManager.GetAvailableUnitTypes(_game.Techs);
                        
                        if (index >= 0 && index < _game.availableUnitIndices.Count)
                        {
                            int unitIndex = _game.availableUnitIndices[index];
                            var unit = _game.UnitTypes[unitIndex];
                            if (_game.hasRecruitedThisTurn[_game.CurrentPlanet])
                            {
                                _game.messageManager.Show("Only one unit can be recruited per turn on this planet", MessageType.Info);
                            }
                            else if (_game.Planets[_game.CurrentPlanet].Food >= unit.FoodCost && _game.Planets[_game.CurrentPlanet].Mat >= unit.MatCost)
                            {
                                _game.messageManager.Show($"Recruit {unit.Name}?\nIt will cost {unit.FoodCost} food and {unit.MatCost} materials\nand take {unit.RecruitTurns} turns to be available", MessageType.Confirm, result =>
                                {
                                    if (result)
                                    {
                                        var unit = _game.UnitTypes[unitIndex];
                                        _game._unitManager.RecruitUnit(_game.CurrentPlanet, unitIndex, _game.Turn);
                                        _game.hasRecruitedThisTurn[_game.CurrentPlanet] = true;
                                    }
                                });
                            }
                            else
                            {
                                _game.messageManager.Show("The planet does not have enough resources", MessageType.Info);
                            }
                            return;
                        }
                        //Available ships to recruit
                        int lastIndex = _game.availableUnitIndices.Count;
                        _game.availableUnitIndices.Clear();
                        var availableShips = _game._shipManager.GetAvailableShipTypes();

                        if (index >= lastIndex && index < lastIndex + availableShips.Count)
                        {
                            int shipIndex = availableShips[index - lastIndex];
                            var shipType = GameData.ShipTypes[shipIndex];

                            if (_game.hasRecruitedThisTurn[_game.CurrentPlanet])
                            {
                                _game.messageManager.Show("Only one unit can be recruited per turn on this planet", MessageType.Info);
                            }
                            else if (_game.Planets[_game.CurrentPlanet].Mat >= shipType.MatCost)
                            {
                                _game.messageManager.Show($"Recruit {shipType.Name}?\nIt will cost {shipType.MatCost} materials\nand take {shipType.TurnsToBuild} turns to be available", MessageType.Confirm, result =>
                                {
                                    if (result)
                                    {
                                        _game._shipManager.StartBuildingShip(_game.CurrentPlanet, shipIndex, _game.Turn);
                                        _game.hasRecruitedThisTurn[_game.CurrentPlanet] = true;
                                    }
                                });
                            }
                            else
                            {
                                _game.messageManager.Show("There are not enough resources", MessageType.Info);
                            }
                        }
                    }
                    //Region clicked
                    else
                    {
                        if (_game.SelectedUnit != -1)
                        {
                            int clickedReg = Functions.GetClickedReg(_game.RegionDatas, (int)_game.mousePos.X, (int)_game.mousePos.Y);
                            if (clickedReg != -1 && _game.PossibleDestinations.Contains(clickedReg))
                            {
                                int n = _game.CurrentPlanet;
                                int u = _game.SelectedUnit;
                                int currentReg = unitsOnPlanet[u].Region;
                                int unitCode = unitsOnPlanet[u].TypeIndex;
                                int hab = _game.Planets[n].Habitat[clickedReg];
                                int imp = _game.Planets[n].Improvements[clickedReg];
                                //Build Improvement
                                if (clickedReg == currentReg && unitCode == (int)UnitTypeEnum.Builder && hab >= 0 && imp == -1)
                                {
                                    _game.availableImprovementIndices.Clear();
                                    string habitatName = _game.HabitatTypes[Math.Abs(_game.Planets[_game.CurrentPlanet].Habitat[clickedReg])].Name;
                                    for (int j = 0; j < _game.PlanetImprovements.Count; j++)
                                    {
                                        var improvement = _game.PlanetImprovements[j];
                                        if (improvement.RequiredTech != -1 && !_game.Techs[improvement.RequiredTech].IsResearched) continue;
                                        _game.availableImprovementIndices.Add(j);
                                    }

                                    if (_game.availableImprovementIndices.Count == 0)
                                    {
                                        _game.messageManager.Show($"No improvement available for {habitatName}", MessageType.Info);
                                    }
                                    else if (_game.availableImprovementIndices.Count == 1)
                                    {
                                        var improvement = _game.PlanetImprovements[_game.availableImprovementIndices[0]];
                                        if (_game.Planets[n].Mat >= improvement.MatCost)
                                        {
                                            _game.messageManager.Show($"Build {improvement.Name}? It will take {improvement.TurnsToBuild} turns to complete.", MessageType.Confirm, result =>
                                            {
                                                if (result)
                                                {
                                                    _game.Planets[n].Mat -= improvement.MatCost;
                                                    unitsOnPlanet[u].Action = 1;
                                                    _game.TurnActions.Add(new TurnAction { ActionTurn = _game.Turn, TurnFinal = _game.Turn + improvement.TurnsToBuild, PlanetCode = n, UnitID = _game.SelectedUnit, UnitActionType = UnitActionType.Building, TargetReg = clickedReg, ImprovementIndex = _game.availableImprovementIndices[0] });
                                                    _game.messageManager.Show($"Started building {improvement.Name} on {_game.HabitatTypes[_game.Planets[n].Habitat[clickedReg]].Name},\nit will cost {improvement.MatCost} materials and take {improvement.TurnsToBuild} turns.\nUpon completion, it will yield {improvement.FoodProd} food, {improvement.MatProd} materials,\nand {improvement.SciProd} science", MessageType.Info);
                                                    _game.SelectedUnit = -1;
                                                    _game.PossibleDestinations.Clear();
                                                }
                                            });
                                        }
                                        else
                                        {
                                            _game.messageManager.Show($"Not enough materials for building a {improvement.Name}, it needs {improvement.MatCost}", MessageType.Info);
                                        }
                                    }
                                    else if (_game.availableImprovementIndices.Count > 1)
                                    {
                                        habitatName = _game.HabitatTypes[hab].Name;
                                        var optionStrings = _game.availableImprovementIndices.Select(idx =>
                                            $"{_game.PlanetImprovements[idx].Name} (Cost: {_game.PlanetImprovements[idx].MatCost} mat, Turns: {_game.PlanetImprovements[idx].TurnsToBuild})"
                                        ).ToList();

                                        _game.messageManager.ShowSelection($"Choose improvement to build on {habitatName}:", optionStrings, selectedIndex =>
                                        {
                                            if (selectedIndex >= 0) // Valid selection (not cancel)
                                            {
                                                int improvementIdx = _game.availableImprovementIndices[selectedIndex];
                                                var improvement = _game.PlanetImprovements[improvementIdx];
                                                if (_game.Planets[n].Mat >= improvement.MatCost)
                                                {
                                                    _game.messageManager.Show($"Build {improvement.Name}? It will take {improvement.TurnsToBuild} turns to complete.", MessageType.Confirm, confirmResult =>
                                                    {
                                                        if (confirmResult)
                                                        {
                                                            _game.Planets[n].Mat -= improvement.MatCost;
                                                            unitsOnPlanet[u].Action = 1;
                                                            _game.TurnActions.Add(new TurnAction
                                                            {
                                                                ActionTurn = _game.Turn,
                                                                TurnFinal = _game.Turn + improvement.TurnsToBuild,
                                                                PlanetCode = n,
                                                                UnitID = _game.SelectedUnit,
                                                                UnitActionType = UnitActionType.Building,
                                                                TargetReg = clickedReg,
                                                                ImprovementIndex = improvementIdx
                                                            });
                                                            _game.messageManager.Show($"Started building {improvement.Name} on {_game.HabitatTypes[_game.Planets[n].Habitat[clickedReg]].Name},\nit will cost {improvement.MatCost} materials and take {improvement.TurnsToBuild} turns.\nUpon completion, it will yield {improvement.FoodProd} food, {improvement.MatProd} materials,\nand {improvement.SciProd} science", MessageType.Info);
                                                            _game.SelectedUnit = -1;
                                                            _game.PossibleDestinations.Clear();
                                                        }
                                                    });
                                                }
                                                else
                                                {
                                                    _game.messageManager.Show($"Not enough materials for building a {improvement.Name}, it needs {improvement.MatCost}", MessageType.Info);
                                                }
                                            }
                                            else
                                            {
                                                // Optional: Cancel does nothing, or show "Selection cancelled"
                                            }
                                        });
                                    }
                                }
                                else if (clickedReg == currentReg && imp >= 0 && _game.Planets[n].OccupiedByUnit[clickedReg] == -1 && _game.UnitTypes[unitCode].Name == _game.PlanetImprovements[imp].AllowedUnit)
                                {
                                    var improvement = _game.PlanetImprovements[imp];
                                    _game.messageManager.Show($"Occupy {improvement.Name}?\nIt will provide extra {_game.UnitTypes[unitCode].ExtraFoodProd} food, {_game.UnitTypes[unitCode].ExtraMatProd} materials, {_game.UnitTypes[unitCode].ExtraSciProd} science.\nThe unit will remain on the improvement and continue to consume resources.", MessageType.Confirm, result =>
                                    {
                                        if (result)
                                        {
                                            _game.Planets[n].OccupiedByUnit[clickedReg] = unitCode;
                                            unitsOnPlanet[u].Action = -1;
                                            for (int j = _game.TurnActions.Count - 1; j >= 0; j--)
                                            {
                                                if (_game.TurnActions[j].PlanetCode == n && _game.TurnActions[j].UnitID == u)
                                                {
                                                    _game.TurnActions.RemoveAt(j);
                                                }
                                            }
                                            _game.PossibleDestinations.Clear();
                                            _game.messageManager.Show($"Occupied {improvement.Name} with {_game.UnitTypes[unitCode].Name},\ngaining extra {_game.UnitTypes[unitCode].ExtraFoodProd} food, {_game.UnitTypes[unitCode].ExtraMatProd} materials, {_game.UnitTypes[unitCode].ExtraSciProd} science.\nThe unit will remain on the improvement and continue to consume resources.", MessageType.Info);
                                        }
                                    });
                                }
                                else if (unitCode == (int)UnitTypeEnum.Explorer && hab < 0) // Explorer moving to unexplored region
                                {
                                    _game.messageManager.Show($"Do you want to explore region {clickedReg}? It will take {Functions.GetTurnsToExplore(clickedReg)} turns.", MessageType.Confirm, result =>
                                    {
                                        if (result)
                                        {
                                            unitsOnPlanet[u].Region = clickedReg;
                                            unitsOnPlanet[u].Action = 1;
                                            _game.TurnActions.Add(new TurnAction { ActionTurn = _game.Turn, TurnFinal = _game.Turn + Functions.GetTurnsToExplore(clickedReg), PlanetCode = n, UnitID = u, UnitActionType = UnitActionType.MovingOrExploring, TargetReg = clickedReg });
                                            _game.messageManager.Show($"Exploring a new region, it will be finished on turn {Functions.GetTurnsToExplore(clickedReg) + _game.Turn}", MessageType.Info);
                                            _game.SelectedUnit = -1;
                                            _game.PossibleDestinations.Clear();
                                        }
                                    });
                                }
                                else // Regular movement to a explored region
                                {
                                    _game.messageManager.Show($"Move to region {clickedReg}?", MessageType.Confirm, result =>
                                    {
                                        if (result)
                                        {
                                            unitsOnPlanet[u].Region = clickedReg;
                                            unitsOnPlanet[u].Action = 1;
                                            _game.TurnActions.Add(new TurnAction { ActionTurn = _game.Turn, TurnFinal = _game.Turn + 1, PlanetCode = n, UnitID = u, UnitActionType = UnitActionType.MovingOrExploring, TargetReg = clickedReg });
                                            //messageManager.Show($"Moving to habitat {clickedReg}", MessageType.Info);
                                            _game.SelectedUnit = -1;
                                            _game.PossibleDestinations.Clear();
                                        }
                                    });
                                }
                            }
                            else
                            {
                                _game.SelectedUnit = -1;
                                _game.PossibleDestinations.Clear();
                            }
                        } 
                        else // Allow to populate or unpopulate regions
                        {
                            int clickedReg = Functions.GetClickedReg(_game.RegionDatas, (int)_game.mousePos.X, (int)_game.mousePos.Y);
                            if (clickedReg != -1 && _game.Planets[_game.CurrentPlanet].Habitat[clickedReg] >= 0)
                            {
                                _game.tooltipText = "";
                                if(_game.Planets[_game.CurrentPlanet].HabitatPopulated[clickedReg])
                                {
                                    _game.messageManager.Show($"Do you want to stop production in region {clickedReg}?\nThis will free up {GameData.HabitatTypes[_game.Planets[_game.CurrentPlanet].Habitat[clickedReg]].PopNeeded} colonists.", MessageType.Confirm, result =>
                                    {
                                        if (result)
                                        {
                                            _game.Planets[_game.CurrentPlanet].HabitatPopulated[clickedReg] = false;
                                            //_game.messageManager.Show($"Region {clickedReg} has stopped production,\n{GameData.HabitatTypes[_game.Planets[_game.CurrentPlanet].Habitat[clickedReg]].PopNeeded} colonists are now available for other regions.", MessageType.Info);
                                        }
                                    });
                                }
                                else
                                {
                                    if (Functions.GetPlanetPopulation(_game.Planets[_game.CurrentPlanet], "Unassigned") >= GameData.HabitatTypes[_game.Planets[_game.CurrentPlanet].Habitat[clickedReg]].PopNeeded)
                                    {
                                        _game.messageManager.Show($"Do you want to start production in region {clickedReg}?\nThis will assign {GameData.HabitatTypes[_game.Planets[_game.CurrentPlanet].Habitat[clickedReg]].PopNeeded} colonists to this region.", MessageType.Confirm, result =>
                                        {
                                            if (result)
                                            {
                                                _game.Planets[_game.CurrentPlanet].HabitatPopulated[clickedReg] = true;
                                                //_game.messageManager.Show($"Region {clickedReg} has started production with {GameData.HabitatTypes[_game.Planets[_game.CurrentPlanet].Habitat[clickedReg]].PopNeeded} colonists.", MessageType.Info);
                                            }
                                        });
                                    }
                                    else
                                    {
                                        _game.messageManager.Show($"Not enough free colonists to populate this region,\nit requires {GameData.HabitatTypes[_game.Planets[_game.CurrentPlanet].Habitat[clickedReg]].PopNeeded} colonists but only {Functions.GetPlanetPopulation(_game.Planets[_game.CurrentPlanet], "Unassigned")} are available.", MessageType.Info);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Draw(GameTime gameTime)
        {
            _game.GraphicsDevice.Clear(Color.Black);

            // Draw top info bar
            _game._spriteBatch.DrawRectangle(_game._pixel, new Rectangle(0, Constants.TOP_BAR_HEIGHT, Constants.SCREEN_WIDTH, Constants.SCREEN_HEIGHT), Color.LightSeaGreen);
            _game._topBar.DrawTopBar(_game._spriteBatch, TopBarRenderer.TopBarMode.Planet, _game.Turn, _game.GlobalScience, _game.Planets, _game.CalculateResourceTurn, _game.CurrentPlanet);

            float xOffset = _game._bitmapFontBig.MeasureString(_game.Planets[_game.CurrentPlanet].Name.ToUpper()).Width / 2;
            _game._spriteBatch.DrawString(_game._bitmapFontBig, _game.Planets[_game.CurrentPlanet].Name.ToUpper(), new Vector2(Constants.SCREEN_WIDTH / 2 - xOffset, Constants.TOP_BAR_HEIGHT + 20), Color.Black);

            // Draw planet surface
            for (int i = 0; i <= Constants.MAX_PLANET_DIMENS; i++)
            {
                if (i == 0 || _game.Planets[_game.CurrentPlanet].Habitat[i] != Constants.NON_EXISTING_HABTITAT)
                {
                    int hab = _game.Planets[_game.CurrentPlanet].Habitat[i];
                    Vector2 center = new(_game.RegionDatas[i].XC, _game.RegionDatas[i].YC);

                    Color lineColor = Color.White;

                    if (_game.PossibleDestinations.Contains(i))
                    {
                        lineColor = Color.Red;
                    } else if (_game.Planets[_game.CurrentPlanet].HabitatPopulated[i]) 
                    {
                        lineColor = Color.Green;
                    }

                    UIHelpers.DrawHex(_game._spriteBatch, _game._pixel, center, Constants.HEX_SIZE / 2f, lineColor);
                    //_spriteBatch.DrawString(_font, HabitatTypes[hab].Name, center, Color.White);

                    if (hab >= 0)
                    {
                        var texture = _game.Textures[_game.HabitatTypes[Math.Abs(hab)].TextureId];
                        _game.Texturescale = (float)Math.Round(Constants.HEX_SIZE / (decimal)texture.Width, 3);
                        _game._spriteBatch.Draw(texture, new Vector2(center.X - Constants.HEX_SIZE / 2, center.Y - Constants.HEX_SIZE / 2 * 1.1547f), null, Color.White, 0f, Vector2.Zero, _game.Texturescale, SpriteEffects.None, 0f);

                        int imp = _game.Planets[_game.CurrentPlanet].Improvements[i];
                        if (imp >= 0)
                        {
                            var improvement = _game.PlanetImprovements[imp];
                            var textureImp = _game.Textures[improvement.TextureId];
                            _game._spriteBatch.Draw(textureImp, new Vector2(center.X - textureImp.Width * 0.165f + 10, center.Y - textureImp.Height * 0.165f + 10), null, Color.White * 0.8f, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
                        }

                        _game._regionBonusManager.DrawRegionBonus(_game.Planets[_game.CurrentPlanet], i, center, _game._spriteBatch, _game.Textures);
                    }
                    else
                    {
                        _game._spriteBatch.Draw(_game.Textures[5], new Vector2(center.X - _game.Textures[5].Width * 0.04f, center.Y - _game.Textures[5].Height * 0.04f), null, Color.White, 0f, Vector2.Zero, Constants.UNEXPLORED_SCALE, SpriteEffects.None, 0f);
                    }
                    if (showRegionNumber)
                        _game._spriteBatch.DrawString(_game._bitmapFont, i.ToString(), new Vector2(center.X - Constants.HEX_SIZE / 3, center.Y + Constants.HEX_SIZE / 10), Color.Yellow);
                }
            }
            // Draw available units
            var unitsOnPlanet = _game._unitManager.GetUnitsOnPlanet(_game.CurrentPlanet);
            int totalUnits = unitsOnPlanet.Count;
            _game._spriteBatch.DrawString(_game._bitmapFont, $"Available Units/Ships", new Vector2(25, Constants.UNIT_MENU_MIN_Y - 35), Color.White);
            for (int i = 0; i < totalUnits; i++)
            {
                Unit unit = unitsOnPlanet[i];
                int unitCode = unit.TypeIndex;
                int region = unit.Region;
                if (region >= _game.RegionDatas.Length) continue;

                Vector2 center = new Vector2(_game.RegionDatas[region].XC, _game.RegionDatas[region].YC);
                var unitTexture = _game.Textures[_game.UnitTypes[unitCode].TextureId];

                // Draw units icon on grid (grayed out if inactive)
                float gridScale = (unit.Action != 0) ? Constants.UNIT_SCALE * 0.9f : Constants.UNIT_SCALE;
                Color gridColor = (unit.Action != 0) ? Color.Gray * 0.7f : Color.White;
                _game._spriteBatch.Draw(unitTexture, center, null, gridColor, 0f, Vector2.Zero, gridScale, SpriteEffects.None, 0f);

                // Draw units in menu (grayed out if inactive)
                _game._spriteBatch.Draw(unitTexture, new Vector2(25, 100 + i * 90), null, gridColor, 0f, Vector2.Zero, Constants.MENU_UNIT_SCALE, SpriteEffects.None, 0f);
                float yOffset = _game._bitmapFont.MeasureString(_game.UnitTypes[unitCode].Name).Height / 2;
                Color textColor = (unit.Action != 0) ? Color.Gray : Color.White;
                _game._spriteBatch.DrawString(_game._bitmapFont, $"{_game.UnitTypes[unitCode].Name} ({region})", new Vector2(125, 145 + i * 90 - yOffset), textColor);
            }
            // Draw available ships
            var shipsOnPlanet = _game._shipManager.GetShipsOnPlanet(_game.CurrentPlanet);
            int totalShips = shipsOnPlanet.Count;
            for (int i = 0; i < totalShips; i++)
            {
                int pos = totalUnits + i;
                Ship ship = shipsOnPlanet[i];
                int shipCode = ship.TypeIndex;
                var shipTexture = _game.Textures[GameData.ShipTypes[shipCode].TextureId];
                Color gridColor = (ship.Status == ShipStatus.Building || ship.Status == ShipStatus.InTransit) ? Color.Gray : Color.White;

                // Draw ships in menu (grayed out if inactive)
                _game._spriteBatch.Draw(shipTexture, new Vector2(25, 100 + pos * 90), null, gridColor, 0f, Vector2.Zero, Constants.MENU_UNIT_SCALE, SpriteEffects.None, 0f);
                float yOffset = _game._bitmapFont.MeasureString(GameData.ShipTypes[shipCode].Name).Height / 2;
                Color textColor = (ship.Status == ShipStatus.Building || ship.Status == ShipStatus.InTransit) ? Color.Gray : Color.White;
                _game._spriteBatch.DrawString(_game._bitmapFont, $"{GameData.ShipTypes[shipCode].Name}", new Vector2(125, 145 + pos * 90 - yOffset), textColor);
            }
            //Draw recruit units menu
            _game._spriteBatch.DrawString(_game._bitmapFont, $"Recruitable Units/Ships", new Vector2(Constants.RECRUIT_MENU_X, Constants.RECRUIT_MENU_Y - 35), Color.White);
            int unitPos = 0;
            for (int i = 0; i < _game.UnitTypes.Count; i++)
            {
                var unit = _game.UnitTypes[i];
                if (unit.RequiredTech != -1 && !_game.Techs[unit.RequiredTech].IsResearched) continue;
                Vector2 pos = new Vector2(Constants.RECRUIT_MENU_X, Constants.RECRUIT_MENU_Y + unitPos * Constants.RECRUIT_HEIGHT);
                _game._spriteBatch.Draw(_game.Textures[unit.TextureId], pos, null, Color.White, 0f, Vector2.Zero, Constants.MENU_UNIT_SCALE, SpriteEffects.None, 0f);
                _game._spriteBatch.DrawString(_game._bitmapFont, unit.Name, pos + new Vector2(100, 10), Color.White);
                _game._spriteBatch.DrawString(_game._bitmapFont, $"Food: {unit.FoodCost} Material: {unit.MatCost}", pos + new Vector2(100, 30), Color.White);
                unitPos++;
            }
            //Draw recruit ships menu
            foreach (var ship in _game._shipManager.GetAvailableShipTypes())
            {
                Vector2 pos = new Vector2(Constants.RECRUIT_MENU_X, Constants.RECRUIT_MENU_Y + unitPos * Constants.RECRUIT_HEIGHT);
                _game._spriteBatch.Draw(_game.Textures[_game.Ships[ship].TextureId], pos, null, Color.White, 0f, Vector2.Zero, Constants.MENU_UNIT_SCALE, SpriteEffects.None, 0f);
                _game._spriteBatch.DrawString(_game._bitmapFont, _game.Ships[ship].Name, pos + new Vector2(100, 10), Color.White);
                _game._spriteBatch.DrawString(_game._bitmapFont, $"Material: {_game.Ships[ship].MatCost}", pos + new Vector2(100, 30), Color.White);
            }
            // Draw tooltip
            UIHelpers.DrawTooltip(_game._spriteBatch, _game.tooltipText, _game.mousePos, _game._bitmapFontTooltip, _game._pixel);
        }
    }
}