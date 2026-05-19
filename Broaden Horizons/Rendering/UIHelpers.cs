using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;

namespace BroadenHorizons
{
    public static class UIHelpers
    {
        private struct Button
        {
            public Rectangle Rect { get; set; }
            public string Label { get; set; }
            public Action OnClick { get; set; }
        }

        private static readonly Dictionary<(int width, int height), Texture2D> roundedButtonTextureCache = new();

        private static List<Button> InitializeGalaxyMapButtons(MessageManager messageManager, Action<GameTime> endTurnAction, Action switchToTechTree, Action switchToPlanetList, Action switchToShipsList, List<Tech> techs, int globalScience, GameTime gameTime)
        {
            var buttons = new List<Button>();
            int buttonY = (Constants.TOP_BAR_HEIGHT - Constants.TOP_BAR_BUTTON_HEIGHT) / 2;

            // Ships Screen Button
            buttons.Add(new Button
            {
                Rect = new Rectangle(
                    Constants.SCREEN_WIDTH - 15 - Constants.TOP_BAR_BUTTON_WIDTH * 5 - Constants.TOP_BAR_BUTTON_SPACING * 4,
                    buttonY,
                    Constants.TOP_BAR_BUTTON_WIDTH,
                    Constants.TOP_BAR_BUTTON_HEIGHT
                ),
                Label = "Sh(i)ps",
                OnClick = switchToShipsList
            });

            // Planet Screen Button
            buttons.Add(new Button
            {
                Rect = new Rectangle(
                    Constants.SCREEN_WIDTH - 15 - Constants.TOP_BAR_BUTTON_WIDTH * 4 - Constants.TOP_BAR_BUTTON_SPACING * 3,
                    buttonY,
                    Constants.TOP_BAR_BUTTON_WIDTH,
                    Constants.TOP_BAR_BUTTON_HEIGHT
                ),
                Label = "(P)lanets",
                OnClick = switchToPlanetList
            });

            // Tech Tree Button
            buttons.Add(new Button
            {
                Rect = new Rectangle(
                    Constants.SCREEN_WIDTH - 15 - Constants.TOP_BAR_BUTTON_WIDTH * 3 - Constants.TOP_BAR_BUTTON_SPACING * 2,
                    buttonY,
                    Constants.TOP_BAR_BUTTON_WIDTH,
                    Constants.TOP_BAR_BUTTON_HEIGHT
                ),
                Label = "(T)ech Tree",
                OnClick = switchToTechTree
            });

            // Help Button
            buttons.Add(new Button
            {
                Rect = new Rectangle(
                    Constants.SCREEN_WIDTH - 15 - Constants.TOP_BAR_BUTTON_WIDTH * 2 - Constants.TOP_BAR_BUTTON_SPACING,
                    buttonY,
                    Constants.TOP_BAR_BUTTON_WIDTH,
                    Constants.TOP_BAR_BUTTON_HEIGHT
                ),
                Label = "(H)elp",
                OnClick = () => messageManager.Show(Constants.HELP_TEXT, MessageType.Help)
            });

            // End Turn Button
            buttons.Add(new Button
            {
                Rect = new Rectangle(
                    Constants.SCREEN_WIDTH - 15 - Constants.TOP_BAR_BUTTON_WIDTH,
                    buttonY,
                    Constants.TOP_BAR_BUTTON_WIDTH,
                    Constants.TOP_BAR_BUTTON_HEIGHT
                ),
                Label = "(E)nd Turn",
                OnClick = () =>
                {
                    bool hasTechTreeActions = Tech.HasTechTreeActions(techs, globalScience);
                    if (hasTechTreeActions)
                    {
                        messageManager.Show("Research actions are available. End turn anyway?", MessageType.Confirm, result =>
                        {
                            if (result) endTurnAction?.Invoke(gameTime);
                        });
                    }
                    else
                    {
                        messageManager.Show("End turn?", MessageType.Confirm, result =>
                        {
                            if (result) endTurnAction?.Invoke(gameTime);
                        });
                    }
                }
            });

            return buttons;
        }

        public static void DrawGalaxyMapButtons(SpriteBatch spriteBatch, Texture2D pixel, BitmapFont font, Vector2 mousePos, bool highlightTechTreeButton, List<Tech> techs, int globalScience, GameTime gameTime)
        {
            foreach (var button in InitializeGalaxyMapButtons(null, null, null, null, null, techs, globalScience, gameTime)) // Pass null for actions as they aren't needed for drawing
            {
                var Color = Constants.TOP_BAR_BUTTON_COLOR;
                var ColorHighlight = Constants.TOP_BAR_BUTTON_HIGHLIGHT_COLOR;
                if (button.Label == "(E)nd Turn" && highlightTechTreeButton)
                {
                    Color = Constants.TOP_BAR_BUTTON_COLOR_MSG;
                    ColorHighlight = Constants.TOP_BAR_BUTTON_HIGHLIGHT_COLOR_MSG;
                }
                Color buttonColor = button.Rect.Contains(mousePos) ? ColorHighlight : Color;

                Vector2 textSize = font.MeasureString(button.Label);
                Vector2 textPos = new Vector2(
                    button.Rect.X + (Constants.TOP_BAR_BUTTON_WIDTH - textSize.X) / 2,
                    button.Rect.Y + (Constants.TOP_BAR_BUTTON_HEIGHT - textSize.Y) / 2
                );
                DrawRoundedButton(
                    spriteBatch,
                    pixel,
                    button.Rect,
                    button.Label,
                    buttonColor,
                    font
                );
                spriteBatch.DrawString(font, button.Label, textPos, Color.White);
            }
        }

        public static void UpdateGalaxyMapButtons(GameTime gameTime, MouseState mouse, MouseState prevMouse, MessageManager messageManager, Action<GameTime> endTurnAction, Action switchToTechTree, Action switchToPlanetList, Action switchToShipsList, List<Tech> techs, int globalScience)
        {
            if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
            {
                Vector2 mousePos = mouse.Position.ToVector2();
                foreach (var button in InitializeGalaxyMapButtons(messageManager, endTurnAction, switchToTechTree, switchToPlanetList, switchToShipsList, techs, globalScience, gameTime))
                {
                    if (button.Rect.Contains(mousePos))
                    {
                        button.OnClick?.Invoke();
                        break;
                    }
                }
            }
        }

        public static void DrawRoundedButton(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, string text, 
            Color baseColor, BitmapFont font, bool isHighlighted = false, Color? textColor = null)
        {
            Color fillColor = isHighlighted ? Color.Lerp(baseColor, Color.White, 0.25f) : baseColor;
            Color borderColor = isHighlighted ? Color.White : Color.Lerp(baseColor, Color.Black, 0.3f);

            // Draw rounded background using cached texture
            Texture2D roundedTex = GetOrCreateRoundedButtonTexture(spriteBatch.GraphicsDevice, rect.Width, rect.Height);
            spriteBatch.Draw(roundedTex, rect, fillColor);

            // Optional subtle border
            if (!isHighlighted)
            {
                Rectangle borderRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
            }

            // Draw text centered
            Vector2 textSize = font.MeasureString(text);
            Vector2 textPos = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2,
                rect.Y + (rect.Height - textSize.Y) / 2
            );

            spriteBatch.DrawString(font, text, textPos, textColor ?? Color.White);
        }

        private static Texture2D GetOrCreateRoundedButtonTexture(GraphicsDevice graphics, int width, int height)
        {
            var key = (width, height);
            if (roundedButtonTextureCache.TryGetValue(key, out Texture2D cachedTexture) && cachedTexture != null)
            {
                return cachedTexture;
            }

            Texture2D tex = CreateRoundedButtonTexture(graphics, width, height);
            roundedButtonTextureCache[key] = tex;
            return tex;
        }

        private static Texture2D CreateRoundedButtonTexture(GraphicsDevice graphics, int width, int height)
        {
            Texture2D tex = new Texture2D(graphics, width, height);
            Color[] data = new Color[width * height];

            Color baseFill = new Color(70, 70, 90);
            int radius = 8;
            int rSquared = radius * radius;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool transparent = false;

                    // Top-left corner
                    if (x < radius && y < radius)
                    {
                        int dx = radius - x;
                        int dy = radius - y;
                        if (dx * dx + dy * dy > rSquared)
                            transparent = true;
                    }
                    // Top-right
                    else if (x >= width - radius && y < radius)
                    {
                        int dx = x - (width - radius - 1);
                        int dy = radius - y;
                        if (dx * dx + dy * dy > rSquared)
                            transparent = true;
                    }
                    // Bottom-left
                    else if (x < radius && y >= height - radius)
                    {
                        int dx = radius - x;
                        int dy = y - (height - radius - 1);
                        if (dx * dx + dy * dy > rSquared)
                            transparent = true;
                    }
                    // Bottom-right
                    else if (x >= width - radius && y >= height - radius)
                    {
                        int dx = x - (width - radius - 1);
                        int dy = y - (height - radius - 1);
                        if (dx * dx + dy * dy > rSquared)
                            transparent = true;
                    }

                    data[x + y * width] = transparent ? Color.Transparent : baseFill;
                }
            }

            tex.SetData(data);
            return tex;
        }

        private static readonly Dictionary<float, Vector2[]> hexPointCache = new Dictionary<float, Vector2[]>();

        public static void DrawHex(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float size, Color lineColor)
        {
            if (!hexPointCache.TryGetValue(size, out Vector2[] points))
            {
                points = new Vector2[6];
                float radius = size * 2f / (float)Math.Sqrt(3);
                for (int i = 0; i < 6; i++)
                {
                    float angle = (float)(2 * Math.PI / 6 * i + Math.PI / 6);
                    points[i] = new Vector2(radius * (float)Math.Cos(angle), radius * (float)Math.Sin(angle));
                }
                hexPointCache[size] = points;
            }

            Vector2[] absPoints = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                absPoints[i] = center + points[i];
            }

            int thickness = (lineColor != Color.White) ? 5 : 1;
            for (int i = 0; i < 5; i++)
            {
                spriteBatch.DrawLine(pixel, absPoints[i], absPoints[i + 1], lineColor, thickness);
            }
            spriteBatch.DrawLine(pixel, absPoints[5], absPoints[0], lineColor, thickness);
        }

        public static void DrawTooltip(SpriteBatch sb, string text, Vector2 pos, BitmapFont font, Texture2D pixel)
        {
            if (string.IsNullOrEmpty(text)) return;

            Vector2 size = font.MeasureString(text);
            Vector2 offset = new Vector2(10, 10);
            Vector2 tooltipSize = size + new Vector2(20, 20);

            Vector2 drawPos = pos + offset;
            bool nearRight = pos.X > Constants.SCREEN_WIDTH - Constants.TOOLTIP_EDGE_THRESHOLD_RIGHT;
            bool nearBottom = pos.Y > Constants.SCREEN_HEIGHT - Constants.TOOLTIP_EDGE_THRESHOLD_BOTTOM;

            if (nearRight)
            {
                drawPos.X = pos.X - tooltipSize.X - offset.X;
            }

            if (nearBottom)
            {
                drawPos.Y = pos.Y - tooltipSize.Y - offset.Y;
            }

            drawPos.X = MathHelper.Clamp(drawPos.X, 0, Constants.SCREEN_WIDTH - tooltipSize.X);
            drawPos.Y = MathHelper.Clamp(drawPos.Y, 0, Constants.SCREEN_HEIGHT - tooltipSize.Y);

            Rectangle bg = new Rectangle((int)drawPos.X, (int)drawPos.Y, (int)tooltipSize.X, (int)tooltipSize.Y);
            sb.Draw(pixel, bg, new Color(0, 0, 0, 200));
            sb.DrawString(font, text, drawPos + offset, Color.White);
        }
    }
}