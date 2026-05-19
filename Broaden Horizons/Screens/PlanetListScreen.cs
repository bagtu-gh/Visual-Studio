using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Linq;
using System.Reflection.Metadata;

namespace BroadenHorizons.Screens
{
    public class PlanetListScreen
    {
        private readonly BH _game;
        private Vector2 _scrollOffset = Vector2.Zero;

        private const float RowHeight = Constants.LIST_ROW_HEIGHT;
        private const float HeaderHeight = Constants.LIST_HEADER_HEIGHT;
        private readonly float[] colWidths = [200, 80, 150, 70, 80, 325, 70, 70];
        private readonly bool[] isCentered = [false, true, false, true, true, false, true, true];
        private readonly string[] headers = {
            "Planet",
            "Size",
            "Status",
            "Res",
            "Temp",
            "Production",
            "Pop",
            "Units"
        };
        private const float SPACING = 15f;
        private readonly int UnderlinePadding = 15;

        public PlanetListScreen(BH game)
        {
            _game = game;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            _game.tooltipText = "";

            if (keyboard.IsKeyDown(Keys.Escape) && !_game.WasKeyDown(Keys.Escape) ||
                keyboard.IsKeyDown(Keys.P) && !_game.WasKeyDown(Keys.P) ||
                keyboard.IsKeyDown(Keys.Enter) && !_game.WasKeyDown(Keys.Enter))
            {
                _game.CurrentState = BH.GameState.GalaxyMap;
                return;
            }

            // Clicking on a planet row goes to that planet's screen (only if explored/owned)
            if (mouse.LeftButton == ButtonState.Pressed && _game._prevMouse.LeftButton == ButtonState.Released)
            {
                float titleSizeY = _game._bitmapFontBig.MeasureString("A").Height;
                float summarySizeY = _game._bitmapFont.MeasureString("A").Height;
                float rowY = Constants.TOP_BAR_HEIGHT + SPACING + titleSizeY + SPACING + summarySizeY + SPACING + HeaderHeight - _scrollOffset.Y;
                float startX = 50f;
                float totalRowWidth = colWidths.Sum();

                for (int i = 0; i < Constants.NUM_PLANETS; i++)
                {
                    if (mouse.Y >= rowY && mouse.Y <= rowY + RowHeight &&
                        mouse.X >= startX && mouse.X <= startX + totalRowWidth)
                    {
                        var planet = _game.Planets[i];
                        if (planet.Status == PlanetStatus.Owned || planet.Status == PlanetStatus.Explored)
                        {
                            _game.CurrentPlanet = i;
                            _game.CurrentState = BH.GameState.PlanetScreen;
                            _game.requireMouseRelease = true;
                            return;
                        }
                        else if (planet.Status == PlanetStatus.Unexplored)
                        {
                            _game._messageManager.Show($"{planet.Name} is not explored yet", MessageType.Info);
                            return;
                        }
                    }
                    rowY += RowHeight;
                }
            }

            // Scroll handling
            if (mouse.ScrollWheelValue != _game._prevMouse.ScrollWheelValue)
            {
                _scrollOffset.Y -= (mouse.ScrollWheelValue - _game._prevMouse.ScrollWheelValue) / 4f;
            }

            if (keyboard.IsKeyDown(Keys.Down)) _scrollOffset.Y += 20f;
            if (keyboard.IsKeyDown(Keys.Up)) _scrollOffset.Y -= 20f;

            _scrollOffset.Y = MathHelper.Clamp(_scrollOffset.Y, 0, GetMaxScroll());
        }

        private float GetMaxScroll()
        {
            float totalContentHeight = HeaderHeight + (Constants.NUM_PLANETS * RowHeight);
            float visibleHeight = Constants.SCREEN_HEIGHT - (Constants.TOP_BAR_HEIGHT + SPACING + 75 + HeaderHeight) - 35; // matches top layout + bottom margin

            return Math.Max(0, totalContentHeight - visibleHeight);
        }

        public void Draw(GameTime gameTime)
        {
            _game.GraphicsDevice.Clear(Color.DarkSlateBlue);

            float startY = Constants.TOP_BAR_HEIGHT + SPACING;
            float startX = 50f;

            // Top Bar
            _game._topBar.DrawTopBar(_game._spriteBatch, TopBarRenderer.TopBarMode.Global,
                _game.Turn, _game.GlobalScience, _game.Planets, _game.CalculateResourceTurn);

            // Title
            string title = "PLANET LIST";
            Vector2 titleSize = _game._bitmapFontBig.MeasureString(title);
            _game._spriteBatch.DrawString(_game._bitmapFontBig, title,
                new Vector2((Constants.SCREEN_WIDTH - titleSize.X) / 2, startY), Color.White);

            // Summary
            float summaryY = startY + titleSize.Y + SPACING;
            int owned = _game.Planets.Count(p => p.Status == PlanetStatus.Owned);
            int explored = _game.Planets.Count(p => p.Status == PlanetStatus.Explored);
            string summary = $"Total Planets: {Constants.NUM_PLANETS}   •   Owned: {owned}   •   Explored: {explored}";
            Vector2 summarySize = _game._bitmapFont.MeasureString(summary);
            _game._spriteBatch.DrawString(_game._bitmapFont, summary, new Vector2(startX, summaryY), Color.LightGray);

            // Headers
            float headerY = summaryY + summarySize.Y + SPACING;
            DrawTableHeader(startX, headerY);

            // === Scissor Area for scrolling content ===
            float contentStartY = headerY + HeaderHeight;
            float contentHeight = Constants.SCREEN_HEIGHT - contentStartY - 35;

            Rectangle scissorRect = new Rectangle(
                (int)startX - 20,
                (int)contentStartY,
                Constants.SCREEN_WIDTH - (int)startX + 40,
                (int)contentHeight
            );

            // End current batch and start clipped one
            _game._spriteBatch.End();
            _game._spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                null, new RasterizerState { ScissorTestEnable = true });

            _game.GraphicsDevice.ScissorRectangle = scissorRect;

            // Draw Rows (clipped)
            float rowY = headerY + HeaderHeight - _scrollOffset.Y;

            for (int i = 0; i < Constants.NUM_PLANETS; i++)
            {
                if (rowY > Constants.SCREEN_HEIGHT) break;

                var planet = _game.Planets[i];
                DrawPlanetRow(planet, i, startX, rowY, i % 2 == 0);
                rowY += RowHeight;
            }

            // End clipped batch
            _game._spriteBatch.End();

            // Restart normal drawing for footer
            _game._spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        }

        private void DrawTableHeader(float x, float y)
        {
            float currentX = x;
            for (int i = 0; i < headers.Length; i++)
            {
                if (isCentered[i])
                {
                    Vector2 textSize = _game._bitmapFont.MeasureString(headers[i]);
                    _game._spriteBatch.DrawString(_game._bitmapFont, headers[i],
                        new Vector2(currentX + (colWidths[i] - textSize.X) / 2f, y + 8), Color.Yellow);
                }
                else
                {
                    _game._spriteBatch.DrawString(_game._bitmapFont, headers[i],
                        new Vector2(currentX, y + 8), Color.Yellow);
                }
                currentX += colWidths[i];
            }

            _game._spriteBatch.DrawLine(_game._pixel,
                new Vector2(x - UnderlinePadding, y + 40),
                new Vector2(x + colWidths.Sum() + UnderlinePadding, y + 40), Color.Yellow * 0.8f);
        }

        private void DrawPlanetRow(Planet planet, int planetIndex, float startX, float y, bool evenRow)
        {
            float currentX = startX;

            // Alternating row background
            if (evenRow)
            {
                _game._spriteBatch.Draw(_game._pixel,
                    new Rectangle((int)startX - 10, (int)y, (int)colWidths.Sum() + 16, (int)RowHeight),
                    new Color(28, 32, 48, 220));
            }

            // Vertical text centering
            float fontHeight = _game._bitmapFont.MeasureString("A").Height;
            float textY = y + (RowHeight - fontHeight) / 2f;

            // 1. Planet Name
            _game._spriteBatch.DrawString(_game._bitmapFont, planet.Name,
                new Vector2(currentX, textY), Color.White);
            currentX += colWidths[0];

            // 2. Size
            string sizeText = (planet.Status == PlanetStatus.Owned || planet.Status == PlanetStatus.Explored)
                ? planet.Dimens.ToString()
                : "??";
            Vector2 sizeMeasure = _game._bitmapFont.MeasureString(sizeText);
            float sizeCenterX = currentX + (colWidths[1] - sizeMeasure.X) / 2f;
            _game._spriteBatch.DrawString(_game._bitmapFont, sizeText,
                new Vector2(sizeCenterX, textY), Color.White);
            currentX += colWidths[1];

            // 3. Status
            Color statusColor = planet.Status switch
            {
                PlanetStatus.Owned => Color.LimeGreen,
                PlanetStatus.Explored => Color.Cyan,
                PlanetStatus.Unexplored => Color.Gray,
                _ => Color.Orange
            };

            _game._spriteBatch.DrawString(_game._bitmapFont, planet.Status.ToString(),
            new Vector2(currentX, textY), statusColor);
            currentX += colWidths[2];

            // 4. Resources
            sizeText = planet.RegionBonuses.Count.ToString();
            sizeMeasure = _game._bitmapFont.MeasureString(sizeText);
            sizeCenterX = currentX + (colWidths[3] - sizeMeasure.X) / 2f;

            string resources = (planet.Status == PlanetStatus.Owned || planet.Status == PlanetStatus.Explored)
                ? planet.RegionBonuses.Count.ToString()
                : "??";
            _game._spriteBatch.DrawString(_game._bitmapFont, resources,
                new Vector2(sizeCenterX, textY), Color.LightGreen);
            currentX += colWidths[3];

            // 5. Temperature
            var dataList = Functions.GetTemperatureRangeData(planet.Temperature);
            Color tempColor = (Color)dataList["Color"];
            if (planet.Status == PlanetStatus.Owned || planet.Status == PlanetStatus.Explored)
            {
                sizeText = planet.Temperature.ToString();
                sizeMeasure = _game._bitmapFont.MeasureString(sizeText);
                sizeCenterX = currentX + (colWidths[4] - sizeMeasure.X) / 2f;
                _game._spriteBatch.DrawString(_game._bitmapFont, planet.Temperature.ToString(),
                    new Vector2(sizeCenterX, textY), tempColor);
            }
            else
            {
                sizeText = "---";
                sizeMeasure = _game._bitmapFont.MeasureString(sizeText);
                sizeCenterX = currentX + (colWidths[4] - sizeMeasure.X) / 2f;
                _game._spriteBatch.DrawString(_game._bitmapFont, "---", new Vector2(sizeCenterX, textY), Color.Gray);
            }
            currentX += colWidths[4];

            // 6. Production
            var production = _game._productionManager.CalculateProduction(planetIndex);
            string prod = $"F:{planet.Food}({Functions.GetSignedValue(production.Food)}) M:{planet.Mat}({Functions.GetSignedValue(production.Materials)}) S+{production.Science} E:{planet.Energy}({Functions.GetSignedValue(production.Energy)})";
            if (planet.Status == PlanetStatus.Owned)
            {
                _game._spriteBatch.DrawString(_game._bitmapFont, prod, new Vector2(currentX, textY), Color.LightGreen);
            }
            else
            {
                _game._spriteBatch.DrawString(_game._bitmapFont, "---", new Vector2(currentX, textY), Color.Gray);
            }
            currentX += colWidths[5];

            // 7. Population
            sizeText = planet.Population.ToString();
            sizeMeasure = _game._bitmapFont.MeasureString(sizeText);
            sizeCenterX = currentX + (colWidths[6] - sizeMeasure.X) / 2f;
            _game._spriteBatch.DrawString(_game._bitmapFont, planet.Population.ToString(),
                new Vector2(sizeCenterX, textY), Color.White);
            currentX += colWidths[6];

            // 8. Units on Planet
            int unitCount = _game._unitManager.GetUnitsOnPlanet(planetIndex).Count;
            sizeText = unitCount.ToString();
            sizeMeasure = _game._bitmapFont.MeasureString(sizeText);
            sizeCenterX = currentX + (colWidths[7] - sizeMeasure.X) / 2f;
            _game._spriteBatch.DrawString(_game._bitmapFont, unitCount.ToString(),
                new Vector2(sizeCenterX, textY), Color.White);
        }
    }
}