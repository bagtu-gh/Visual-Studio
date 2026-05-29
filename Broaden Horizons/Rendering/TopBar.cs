using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BroadenHorizons
{
    /// <summary>
    /// Centralized renderer for the top information bar.
    /// Keeps layout and drawing consistent across Galaxy, Planet, and Tech screens.
    /// </summary>
    public sealed class TopBarRenderer
    {
        private readonly BitmapFont _font;
        private readonly Texture2D _pixel;
        private readonly Dictionary<int, Texture2D> _textures;

        private const int TEX_FOOD = 6;
        private const int TEX_MAT = 7;
        private const int TEX_SCI = 8;
        private const int TEX_ENE = 9;

        public enum TopBarMode
        {
            Global,
            Planet
        }

        public TopBarRenderer(BitmapFont font, Dictionary<int, Texture2D> textures, Texture2D pixel)
        {
            _font = font;
            _textures = textures;
            _pixel = pixel;
        }

        /// <summary>
        /// Unified method to draw the top bar in either Global or Planet mode.
        /// </summary>
        public void DrawTopBar(
            SpriteBatch sb,
            TopBarMode mode,
            int turn,
            int globalScience,
            IReadOnlyList<Planet> planets,
            Func<int, string, string> calcResourceTurn,
            int planetIndex = -1,
            Color? background = null)
        {
            var bg = background ?? Color.DarkGray;

            // Background bar
            sb.Draw(_pixel, new Rectangle(0, 0, Constants.SCREEN_WIDTH, Constants.TOP_BAR_HEIGHT), bg);

            // Left: "TURN: X"
            float textTopPad = (Constants.TOP_BAR_HEIGHT - _font.MeasureString("A").Height) / 2f;
            sb.DrawString(_font, $"TURN: {turn}",
                new Vector2(Constants.PLANET_TOP_BAR_LEFT_PAD, textTopPad), Color.White);

            float startX = _font.MeasureString($"TURN: {turn}").Width
                            + Constants.PLANET_TOP_BAR_LEFT_PAD
                            + Constants.PLANET_TOP_BAR_TEXT_DIST;
            float currentX = startX;

            // Resources
            var resources = new (int texId, string name)[]
            {
                (TEX_FOOD, "Food"),
                (TEX_MAT, "Materials"),
                (TEX_SCI, "Science"),
                (TEX_ENE, "Energy")
            };

            foreach (var (texId, name) in resources)
            {
                var tex = _textures[texId];
                float scale = (Constants.TOP_BAR_HEIGHT - 10f) / tex.Height;
                float iconWidth = tex.Width * scale;

                // Draw icon
                sb.Draw(tex, new Vector2(currentX, 5f), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                // Compute value and delta based on mode
                int value = 0;
                string delta;
                if (mode == TopBarMode.Global)
                {
                    if (name == "Science")
                    {
                        value = globalScience;
                    }
                    else
                    {
                        value = planets.Where(p => p.Status == PlanetStatus.Owned)
                                       .Sum(p => name switch
                                       {
                                           "Food" => p.Food,
                                           "Materials" => p.Mat,
                                           "Energy" => p.Energy,
                                           _ => 0
                                       });
                    }
                    delta = calcResourceTurn(-1, name);
                }
                else // Planet mode
                {
                    if (planetIndex < 0 || planetIndex >= planets.Count) throw new ArgumentException("Invalid planet index for Planet mode");
                    var currentPlanet = planets[planetIndex];
                    value = name switch
                    {
                        "Food" => currentPlanet.Food,
                        "Materials" => currentPlanet.Mat,
                        "Science" => globalScience,
                        "Energy" => currentPlanet.Energy,
                        _ => 0
                    };
                    delta = calcResourceTurn(planetIndex, name);
                }

                string labelText = $"{value} ({delta})";
                float labelSize = _font.MeasureString(labelText).Width;

                // Draw text to the right of the icon
                float textX = currentX + iconWidth + Constants.PLANET_TOP_BAR_IMGTEXT_PAD;
                sb.DrawString(_font, labelText, new Vector2(textX, textTopPad), Color.White);

                currentX += iconWidth + Constants.PLANET_TOP_BAR_IMGTEXT_PAD + labelSize + Constants.PLANET_TOP_BAR_IMGTEXT_PAD;
            }

            // For Planet mode, add population and temperature
            if (mode == TopBarMode.Planet)
            {
                var currentPlanet = planets[planetIndex];
                // Prepare and draw the temperature text
                var dataList = Functions.GetTemperatureRangeData(currentPlanet.Temperature);
                string tempText = $"TEMP: {currentPlanet.Temperature} ({dataList["Name"]})";
                float tempTextWidth = _font.MeasureString(tempText).Width;
                float tempX = Constants.SCREEN_WIDTH - Constants.PLANET_TOP_BAR_TEXT_DIST - tempTextWidth;
                float tempY = (Constants.TOP_BAR_HEIGHT - _font.MeasureString(tempText).Height) / 2f;
                sb.DrawString(_font, tempText, new Vector2(tempX, tempY), Color.White);

                // Prepare and draw the population text
                string popText = $"POP: {Functions.GetPlanetPopulation(currentPlanet, "Assigned")}/{currentPlanet.Population} ({Functions.GetPopModifier(currentPlanet, int.Parse(calcResourceTurn(planetIndex, "Food")))})";
                float popTextWidth = _font.MeasureString(popText).Width;
                float popX = tempX - Constants.PLANET_TOP_BAR_TEXT_DIST - popTextWidth;
                float popY = tempY;
                sb.DrawString(_font, popText, new Vector2(popX, popY), Color.White);

                //Prepare and draw planet size
                string sizeText = $"SIZE: {currentPlanet.Dimens}";
                float sizeTextWidth = _font.MeasureString(sizeText).Width;
                float sizeX = popX - Constants.PLANET_TOP_BAR_TEXT_DIST - sizeTextWidth;
                float sizeY = tempY;
                sb.DrawString(_font, sizeText, new Vector2(sizeX, sizeY), Color.White);
            }
        }

        /// <summary>
        /// Unified method to handle tooltips for the top bar resources.
        /// Returns true if a tooltip was set, and sets tooltipText and tooltipPos.
        /// </summary>
        public bool HandleTopBarTooltips(
            TopBarMode mode,
            Vector2 mousePos,
            int turn,
            int globalScience,
            IReadOnlyList<Planet> planets,
            Func<int, string, string> calcResourceTurn,
            Func<int, string, string> getProductionTooltip,
            Func<string, string> buildGlobalProductionTooltip,
            Func<int, string> getPopulationTooltip,
            int planetIndex,
            out string tooltipText,
            out Vector2 tooltipPos)
        {
            tooltipText = "";
            tooltipPos = Vector2.Zero;

            var rects = GetTopBarRects(mode, turn, globalScience, planets, calcResourceTurn, planetIndex);

            string[] resourceNames = { "Food", "Materials", "Science", "Energy" };
            foreach (var name in resourceNames)
            {
                if (rects.TryGetValue(name, out Rectangle rect) && rect.Contains(mousePos))
                {
                    if (mode == TopBarMode.Global)
                    {
                        tooltipText = buildGlobalProductionTooltip(name);
                    }
                    else // Planet
                    {
                        if (planetIndex >= 0 && planetIndex < planets.Count)
                        {
                            tooltipText = getProductionTooltip(planetIndex, name);
                        }
                    }
                    tooltipPos = mousePos + new Vector2(20, 20);
                    return true;
                }
            }

            if (mode == TopBarMode.Planet && rects.TryGetValue("Population", out Rectangle popRect) && popRect.Contains(mousePos))
            {
                if (planetIndex >= 0 && planetIndex < planets.Count && getPopulationTooltip != null)
                {
                    tooltipText = getPopulationTooltip(planetIndex);
                    tooltipPos = mousePos + new Vector2(20, 20);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Unified method to get rectangles for top bar resources.
        /// </summary>
        private Dictionary<string, Rectangle> GetTopBarRects(
            TopBarMode mode,
            int turn,
            int globalScience,
            IReadOnlyList<Planet> planets,
            Func<int, string, string> calcResourceTurn,
            int planetIndex)
        {
            var rects = new Dictionary<string, Rectangle>();

            // top text vertical alignment
            float textTopPad = (Constants.TOP_BAR_HEIGHT - _font.MeasureString("A").Height) / 2f;

            // starting X after "TURN: X"
            float startX = _font.MeasureString($"TURN: {turn}").Width
                            + Constants.PLANET_TOP_BAR_LEFT_PAD
                            + Constants.PLANET_TOP_BAR_TEXT_DIST;

            //float resourceSpacing = 150f;
            float currentX = startX;

            // resources in the exact order required
            var resources = new (int texId, string name)[]
            {
                (TEX_FOOD, "Food"),
                (TEX_MAT, "Materials"),
                (TEX_SCI, "Science"),
                (TEX_ENE, "Energy")
            };

            foreach (var (texId, name) in resources)
            {
                var tex = _textures[texId];
                float scale = (Constants.TOP_BAR_HEIGHT - 10f) / tex.Height;
                float iconWidth = tex.Width * scale;

                // Compute value and delta based on mode
                int value = 0;
                string delta;
                if (mode == TopBarMode.Global)
                {
                    if (name == "Science")
                    {
                        value = globalScience;
                    }
                    else
                    {
                        value = planets.Where(p => p.Status == PlanetStatus.Owned)
                                       .Sum(p => name switch
                                       {
                                           "Food" => p.Food,
                                           "Materials" => p.Mat,
                                           "Energy" => p.Energy,
                                           _ => 0
                                       });
                    }
                    delta = calcResourceTurn(-1, name);
                }
                else // Planet mode
                {
                    if (planetIndex < 0 || planetIndex >= planets.Count) throw new ArgumentException("Invalid planet index for Planet mode");
                    var currentPlanet = planets[planetIndex];
                    value = name switch
                    {
                        "Food" => currentPlanet.Food,
                        "Materials" => currentPlanet.Mat,
                        "Science" => globalScience,
                        "Energy" => currentPlanet.Energy,
                        _ => 0
                    };
                    delta = calcResourceTurn(planetIndex, name);
                }

                string labelText = $"{value} ({delta})";
                float labelSize = _font.MeasureString(labelText).Width;

                int rectW = (int)(iconWidth + Constants.PLANET_TOP_BAR_IMGTEXT_PAD + labelSize);
                rects[name] = new Rectangle((int)currentX, 0, rectW, Constants.TOP_BAR_HEIGHT);

                currentX += iconWidth + Constants.PLANET_TOP_BAR_IMGTEXT_PAD + labelSize + Constants.PLANET_TOP_BAR_IMGTEXT_PAD;
            }

            if (mode == TopBarMode.Planet)
            {
                var currentPlanet = planets[planetIndex];
                var dataList = Functions.GetTemperatureRangeData(currentPlanet.Temperature);

                string sizeText = $"SIZE: {currentPlanet.Dimens}";
                string popText = $"POP: {Functions.GetPlanetPopulation(currentPlanet, "Assigned")}/{currentPlanet.Population} ({Functions.GetPopModifier(currentPlanet, int.Parse(calcResourceTurn(planetIndex, "Food")))})";
                string tempText = $"TEMP: {currentPlanet.Temperature} ({dataList["Name"]})";
                float sizeTextWidth = _font.MeasureString(sizeText).Width;
                float sizeX = Constants.SCREEN_WIDTH - Constants.PLANET_TOP_BAR_TEXT_DIST - sizeTextWidth;
                float tempTextWidth = _font.MeasureString(tempText).Width;
                float tempX = Constants.SCREEN_WIDTH - Constants.PLANET_TOP_BAR_TEXT_DIST - tempTextWidth;
                float popTextWidth = _font.MeasureString(popText).Width;
                float popX = tempX - Constants.PLANET_TOP_BAR_TEXT_DIST - popTextWidth;

                rects["Population"] = new Rectangle((int)popX, 0, (int)popTextWidth, Constants.TOP_BAR_HEIGHT);
            }

            return rects;
        }
    }
}