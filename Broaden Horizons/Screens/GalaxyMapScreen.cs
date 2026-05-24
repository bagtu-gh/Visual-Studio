using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using MonoGame.Extended.BitmapFonts;

namespace BroadenHorizons.Screens
{
    public class GalaxyMapScreen(BH game)
    {
        private BH _game = game;

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            _game.tooltipText = "";
            if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D)) _game.ScrollOffset.X += Constants.SCROLL_SPEED;
            if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A)) _game.ScrollOffset.X -= Constants.SCROLL_SPEED;
            if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S)) _game.ScrollOffset.Y += Constants.SCROLL_SPEED;
            if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W)) _game.ScrollOffset.Y -= Constants.SCROLL_SPEED;

            _game.ScrollOffset.X = MathHelper.Clamp(_game.ScrollOffset.X, 0, _game.Textures.ContainsKey(1) ? _game.Textures[1].Width - Constants.SCREEN_WIDTH : 0);
            float maxScrollY = Math.Max(0, _game.Textures[1].Height - (Constants.SCREEN_HEIGHT - Constants.TOP_BAR_HEIGHT));
            _game.ScrollOffset.Y = MathHelper.Clamp(_game.ScrollOffset.Y, 0, maxScrollY);

            if (_game.ScrollOffset.X >= (_game.Textures.ContainsKey(1) ? _game.Textures[1].Width : 0) || _game.ScrollOffset.X <= -1) _game.ScrollOffset.X = _game.PosX;
            if (_game.ScrollOffset.Y >= (_game.Textures.ContainsKey(1) ? _game.Textures[1].Height : 0) || _game.ScrollOffset.Y <= -1) _game.ScrollOffset.Y = _game.PosY;

            _game.PosX = (int)_game.ScrollOffset.X;
            _game.PosY = (int)_game.ScrollOffset.Y;

            if (keyboard.IsKeyDown(Keys.P) && !_game.WasKeyDown(Keys.P)) _game.CurrentState = BH.GameState.PlanetList;

            if (keyboard.IsKeyDown(Keys.I) && !_game.WasKeyDown(Keys.I)) _game.CurrentState = BH.GameState.ShipList;

            if (keyboard.IsKeyDown(Keys.T) && !_game.WasKeyDown(Keys.T))
            {
                _game.PrevState = _game.CurrentState;
                _game.CurrentState = BH.GameState.TechTree;
            }

            if (keyboard.IsKeyDown(Keys.H) && !_game.WasKeyDown(Keys.H))
            {
                _game._messageManager.Show(Constants.HELP_TEXT, MessageType.Help);
            }

            if (keyboard.IsKeyDown(Keys.L) && !_game.WasKeyDown(Keys.L))
            {
                _game._endTurnManager.ShowTurnLog();
            }

            if (keyboard.IsKeyDown(Keys.E) && !_game.WasKeyDown(Keys.E))
            {
                bool hasTechTreeActions = Tech.HasTechTreeActions(_game.Techs, _game.GlobalScience);
                if (hasTechTreeActions)
                {
                    _game._messageManager.Show("Research actions are available. End turn anyway?", MessageType.Confirm, result =>
                    {
                        if (result) _game._endTurnManager.EndTurn(gameTime);
                    });
                }
                else
                {
                    _game._messageManager.Show("End turn?", MessageType.Confirm, result =>
                    {
                        if (result) _game._endTurnManager.EndTurn(gameTime);
                    });
                }
            }

            _game.mousePos = new Vector2(mouse.X, mouse.Y);

            if (_game._topBar.HandleTopBarTooltips(TopBarRenderer.TopBarMode.Global, _game.mousePos, _game.Turn, _game.GlobalScience, _game.Planets, _game._productionManager.CalculateProductionTurn, null, _game._productionManager.BuildGlobalProductionTooltip, null, -1, out string tt, out Vector2 tp))
            {
                _game.tooltipText = tt;
                _game.tooltipPos = tp;
            }

            if (mouse.LeftButton == ButtonState.Pressed && _game._prevMouse.LeftButton == ButtonState.Released)
            {
                UIHelpers.UpdateGalaxyMapButtons(
                    gameTime,
                    mouse,
                    _game._prevMouse,
                    _game._messageManager,
                    _game._endTurnManager.EndTurn,
                    () =>
                    {  // Tech Tree
                        _game.PrevState = _game.CurrentState;
                        _game.CurrentState = BH.GameState.TechTree;
                    },
                    () =>
                    {  // Planets List
                        _game.PrevState = _game.CurrentState;
                        _game.CurrentState = BH.GameState.PlanetList;
                    },
                    () =>
                    {  // Ships List
                        _game.PrevState = _game.CurrentState;
                        _game.CurrentState = BH.GameState.ShipList;
                    },
                    _game.Techs,
                    _game.GlobalScience
                );

                if (mouse.Y >= Constants.TOP_BAR_HEIGHT)
                {
                    for (int i = 0; i < Constants.NUM_PLANETS; i++)
                    {
                        float dist = Vector2.Distance(new Vector2(_game.MouseMapX, _game.MouseMapY), new Vector2(_game.Planets[i].XPos, _game.Planets[i].YPos));
                        float size = _game.Planets[i].Dimens * Constants.PLANET_GALAXY_SCALE / Constants.MAX_PLANET_DIMENS;
                        if (dist <= size / 2)
                        {
                            if (_game.Planets[i].Status == PlanetStatus.Owned || _game.Planets[i].Status == PlanetStatus.Explored)
                            {
                                _game.CurrentPlanet = i;
                                _game.CurrentState = BH.GameState.PlanetScreen;
                                _game.requireMouseRelease = true;
                                break;
                            }
                            if (_game.Planets[i].Status == PlanetStatus.Unexplored)
                            {
                                _game._messageManager.Show($"{_game.Planets[i].Name} is not explored yet", MessageType.Info);
                                break;
                            }
                        }
                    }
                }
            }

            if (keyboard.IsKeyDown(Keys.Escape) && !_game.WasKeyDown(Keys.Escape))
            {
                _game.PrevState = _game.CurrentState;
                _game.CurrentState = BH.GameState.MainMenu;
            }
        }

        public void Draw(GameTime gameTime)
        {
            _game.GraphicsDevice.Clear(Color.Black);
            Vector2 mapPosition = new Vector2(-_game.ScrollOffset.X, Constants.TOP_BAR_HEIGHT - _game.ScrollOffset.Y);
            _game._spriteBatch.Draw(_game.Textures[1], mapPosition, null, Color.White);

            foreach (var star in _game.StarPositions)
            {
                _game._spriteBatch.Draw(_game._pixel, star - _game.ScrollOffset, Color.White);
            }

            for (int i = 0; i < Constants.NUM_PLANETS; i++)
            {
                Vector2 pos = new Vector2(_game.Planets[i].XPos - _game.ScrollOffset.X, _game.Planets[i].YPos - _game.ScrollOffset.Y);
                float size = _game.Planets[i].Dimens * Constants.PLANET_GALAXY_SCALE / Constants.MAX_PLANET_DIMENS;
                if (_game.Textures.ContainsKey(_game.Planets[i].TextureId))
                {
                    float scale = size / _game.Textures[_game.Planets[i].TextureId].Width;
                    _game._spriteBatch.Draw(_game.Textures[_game.Planets[i].TextureId], pos - new Vector2(size / 2, size / 2), null, _game.Planets[i].TintColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                if (_game.Planets[i].Status == PlanetStatus.Owned)
                {
                    _game._spriteBatch.DrawCircle(_game._pixel, pos, size / 2 + 2, 20, Color.White);
                }

                if (_game.Planets[i].Status == PlanetStatus.Explored)
                {
                    _game._spriteBatch.DrawCircle(_game._pixel, pos, size / 2 + 2, 20, Color.OrangeRed);
                }

                string planetName = _game.Planets[i].Name;
                Vector2 nameSize;
                if (!_game._planetNameSizeCache.TryGetValue(i, out nameSize))
                {
                    nameSize = _game._bitmapFont.MeasureString(planetName ?? "");
                    _game._planetNameSizeCache[i] = nameSize;
                }

                _game._spriteBatch.DrawRectangle(_game._pixel, new Rectangle((int)(pos.X + _game.Planets[i].Dimens + 5f), (int)(pos.Y + _game.Planets[i].Dimens / 4 - nameSize.Y / 2), (int)nameSize.X, (int)nameSize.Y), Color.Black * 0.8f);
                _game._spriteBatch.DrawString(_game._bitmapFont, planetName, new Vector2(pos.X + _game.Planets[i].Dimens + 5f, pos.Y + _game.Planets[i].Dimens / 4 - nameSize.Y / 2), Color.White);
            }

            _game._topBar.DrawTopBar(_game._spriteBatch, TopBarRenderer.TopBarMode.Global, _game.Turn, _game.GlobalScience, _game.Planets, _game._productionManager.CalculateProductionTurn);

            bool hasTechTreeActions = Tech.HasTechTreeActions(_game.Techs, _game.GlobalScience);
            UIHelpers.DrawGalaxyMapButtons(_game._spriteBatch, _game._pixel, _game._bitmapFont, _game.mousePos, hasTechTreeActions, _game.Techs, _game.GlobalScience, gameTime);

            foreach (var ship in _game._shipManager.GetShipsInTransit())
            {
                var tex = _game.Textures[GameData.ShipTypes[ship.TypeIndex].TextureId];
                Vector2 adjustedPosition = ship.CurrentPosition - _game.ScrollOffset;
                _game._spriteBatch.Draw(tex, adjustedPosition, null, Color.White, 0f, Vector2.Zero, 0.05f, SpriteEffects.None, 0f);
            }

            UIHelpers.DrawTooltip(_game._spriteBatch, _game.tooltipText, _game.mousePos, _game._bitmapFontTooltip, _game._pixel);
        }
    }
}
