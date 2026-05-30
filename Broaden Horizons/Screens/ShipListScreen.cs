using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Linq;

namespace BroadenHorizons.Screens
{
    public class ShipListScreen
    {
        private readonly BH _game;
        private Vector2 _scrollOffset = Vector2.Zero;
        private const float RowHeight = Constants.LIST_ROW_HEIGHT;
        private const float HeaderHeight = Constants.LIST_HEADER_HEIGHT;
        private readonly float[] colWidths = [250, 140, 200, 130, 200, 200, 110, 110];
        private const float SPACING = 15f;
        private readonly int UnderlinePadding = 15;

        public ShipListScreen(BH game)
        {
            _game = game;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            _game.tooltipText = "";

            if (keyboard.IsKeyDown(Keys.Escape) && !_game.WasKeyDown(Keys.Escape) ||
                keyboard.IsKeyDown(Keys.L) && !_game.WasKeyDown(Keys.L))
            {
                _game.CurrentState = _game.PrevState == BH.GameState.PlanetScreen
                    ? BH.GameState.PlanetScreen
                    : BH.GameState.GalaxyMap;
                return;
            }

            // Scroll
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
            var ships = _game._shipManager.Ships;
            float totalContent = ships.Count * RowHeight + HeaderHeight + 150;
            return Math.Max(0, totalContent - (Constants.SCREEN_HEIGHT - Constants.TOP_BAR_HEIGHT - 60));
        }

        public void Draw(GameTime gameTime)
        {
            _game.GraphicsDevice.Clear(Color.DarkSlateBlue);

            float startY = Constants.TOP_BAR_HEIGHT + SPACING;
            float startX = 50f;

            // Top Bar
            _game._topBar.DrawTopBar(_game._spriteBatch, TopBarRenderer.TopBarMode.Global,
                _game.Turn, _game._techManager.GlobalScience, _game.Planets, _game._productionManager.CalculateProductionTurn);

            // Title
            string title = "FLEET OVERVIEW";
            Vector2 titleSize = _game._bitmapFontBig.MeasureString(title);
            _game._spriteBatch.DrawString(_game._bitmapFontBig, title,
                new Vector2((Constants.SCREEN_WIDTH - titleSize.X) / 2, startY - 5), Color.White);

            // Summary
            float summaryY = startY + titleSize.Y + SPACING;
            var ships = _game._shipManager.Ships;
            int docked = ships.Count(s => s.Status == ShipStatus.Docked);
            int building = ships.Count(s => s.Status == ShipStatus.Building);
            int transit = ships.Count(s => s.Status == ShipStatus.InTransit);
            string summary = $"Total Ships: {ships.Count}   •   Docked: {docked}   •   Building: {building}   •   In Transit: {transit}";
            Vector2 summarySize = _game._bitmapFont.MeasureString(summary);
            _game._spriteBatch.DrawString(_game._bitmapFont, summary, new Vector2(startX, summaryY), Color.LightGray);

            // Headers
            float headerY = summaryY + summarySize.Y + SPACING;
            DrawTableHeader(startX, headerY);

            // === Scissor Area for scrolling rows ===
            float contentStartY = headerY + HeaderHeight;
            float contentHeight = Constants.SCREEN_HEIGHT - contentStartY - 35;

            Rectangle scissorRect = new Rectangle(
                (int)startX - 20,
                (int)contentStartY,
                Constants.SCREEN_WIDTH - (int)startX + 20,
                (int)contentHeight
            );

            // End current batch and start clipped batch
            _game._spriteBatch.End();
            _game._spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                null, new RasterizerState { ScissorTestEnable = true });

            _game.GraphicsDevice.ScissorRectangle = scissorRect;

            // Draw Rows (clipped)
            float rowY = headerY + HeaderHeight - _scrollOffset.Y;

            for (int i = 0; i < ships.Count; i++)
            {
                if (rowY > Constants.SCREEN_HEIGHT) break;

                var ship = ships[i];
                var shipType = GameData.ShipTypes[ship.TypeIndex];

                DrawShipRow(ship, shipType, startX, rowY, i % 2 == 0);
                rowY += RowHeight;
            }

            // End clipped batch
            _game._spriteBatch.End();

            // Restart normal spritebatch for footer
            _game._spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        }

        private void DrawTableHeader(float x, float y)
        {
            string[] headers = { "Ship", "Type", "Home Planet", "Status", "Location", "Destination", "ETA", "Cargo" };
            float currentX = x;
            for (int i = 0; i < headers.Length; i++)
            {
                _game._spriteBatch.DrawString(_game._bitmapFont, headers[i],
                    new Vector2(currentX, y + 8), Color.Yellow);
                currentX += colWidths[i];
            }

            _game._spriteBatch.DrawLine(_game._pixel,
                new Vector2(x - UnderlinePadding, y + 40),
                new Vector2(x + colWidths.Sum() + UnderlinePadding, y + 40), Color.Yellow * 0.8f);
        }

        private void DrawShipRow(Ship ship, ShipType shipType, float startX, float y, bool evenRow)
        {
            float currentX = startX;

            // Row background
            if (evenRow)
            {
                _game._spriteBatch.Draw(_game._pixel,
                    new Rectangle((int)startX - 10, (int)y, (int)colWidths.Sum() + 16, (int)RowHeight),
                    new Color(28, 32, 48, 220));
            }

            // Vertical centering for text
            float fontHeight = _game._bitmapFont.MeasureString("A").Height;
            float textY = y + (RowHeight - fontHeight) / 2f;

            // === Column 1: Icon + Name ===
            var texture = _game.Textures[shipType.TextureId];
            
            // Dynamic icon scaling
            float targetIconHeight = RowHeight * 0.72f;
            float iconScale = targetIconHeight / texture.Height;

            Vector2 iconPos = new Vector2(
                currentX,
                y + (RowHeight - texture.Height * iconScale) / 2f
            );

            _game._spriteBatch.Draw(texture, iconPos, null, Color.White, 0f, Vector2.Zero,
                iconScale, SpriteEffects.None, 0f);

            // Ship name positioned after icon
            string name = string.IsNullOrEmpty(ship.Name) ? shipType.Name : ship.Name;
            float nameX = currentX + 6 + (texture.Width * iconScale);
            
            _game._spriteBatch.DrawString(_game._bitmapFont, name,
                new Vector2(nameX, textY), Color.White);

            currentX += colWidths[0];

            // === Type ===
            _game._spriteBatch.DrawString(_game._bitmapFont, shipType.Name,
                new Vector2(currentX, textY), Color.LightSkyBlue);
            currentX += colWidths[1];

            // === Home Planet ===
            string home = _game.Planets[ship.AssignedPlanet].Name;
            _game._spriteBatch.DrawString(_game._bitmapFont, home,
                new Vector2(currentX, textY), Color.White);
            currentX += colWidths[2];

            // === Status ===
            Color statusColor = ship.Status switch
            {
                ShipStatus.Docked => Color.LimeGreen,
                ShipStatus.Building => Color.Goldenrod,
                ShipStatus.InTransit => Color.Cyan,
                _ => Color.White
            };
            string statusText = ship.Status.ToString();
            _game._spriteBatch.DrawString(_game._bitmapFont, statusText,
                new Vector2(currentX, textY), statusColor);
            currentX += colWidths[3];

            // === Location ===
            string location = ship.Status == ShipStatus.InTransit ? "Travelling" : _game.Planets[ship.AssignedPlanet].Name;
            _game._spriteBatch.DrawString(_game._bitmapFont, location,
                new Vector2(currentX, textY), Color.White);
            currentX += colWidths[4];

            // === Destination ===
            string dest = ship.TargetPlanet >= 0 ? _game.Planets[ship.TargetPlanet].Name : "—";
            _game._spriteBatch.DrawString(_game._bitmapFont, dest,
                new Vector2(currentX, textY), Color.LightGray);
            currentX += colWidths[5];

            // === ETA ===
            string eta = ship.Status == ShipStatus.InTransit && ship.FinalTurnAction > _game.Turn
                ? $"{ship.FinalTurnAction - _game.Turn} turns"
                : "—";
            _game._spriteBatch.DrawString(_game._bitmapFont, eta,
                new Vector2(currentX, textY), Color.Orange);
            currentX += colWidths[6];

            // === Cargo ===
            string cargo = shipType.Type == ShipTypeEnum.Freighter
                ? $"{ship.CargoFood}F/{ship.CargoMat}M"
                : "—";
            _game._spriteBatch.DrawString(_game._bitmapFont, cargo,
                new Vector2(currentX, textY), Color.LightGreen);
        }
    }
}