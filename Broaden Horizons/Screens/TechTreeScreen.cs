using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Linq;

namespace BroadenHorizons.Screens
{
    public class TechTreeScreen(BH game)
    {
        private readonly BH _game = game;
        private bool _initialized = false;
        public Vector2 TechScrollOffset = Vector2.Zero;

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            if (!_initialized)
            {
                TechScrollOffset.X = 0;
                _initialized = true;
            }

            if (_game._messageManager.IsActive)
            {
                _game._messageManager.Update(gameTime, keyboard, mouse, _game._bitmapFontMessages, new Rectangle(0, 0, Constants.SCREEN_WIDTH, Constants.SCREEN_HEIGHT));
                return;
            }

            KeyboardState prevKb = _game._prevKeyboard;
            _game._prevKeyboard = keyboard;
            if (keyboard.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
            {
                _game.CurrentState = _game.PrevState;
                _game.PrevState = BH.GameState.TechTree;
                _initialized = false;
            }

            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) TechScrollOffset.X -= Constants.SCROLL_SPEED;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) TechScrollOffset.X += Constants.SCROLL_SPEED;

            float minX = _game.Techs.Min(t => t.UiPosition.X);
            float maxX = _game.Techs.Max(t => t.UiPosition.X);
            TechScrollOffset.X = MathHelper.Clamp(TechScrollOffset.X, Constants.TECH_TREE_HORIZ_MARGIN - minX, Constants.SCREEN_WIDTH - maxX - Constants.TECH_TREE_BOX_WIDTH - Constants.TECH_TREE_HORIZ_MARGIN);

            _game.hoveredTech = -1;
            bool mouseClicked = mouse.LeftButton == ButtonState.Pressed && _game._prevMouse.LeftButton == ButtonState.Released;
            _game._prevMouse = mouse;
            Vector2 mouseP = new Vector2(mouse.X, mouse.Y);
            _game.mousePos = mouseP;
            for (int i = 0; i < _game.Techs.Count; i++)
            {
                Vector2 pos = _game.Techs[i].UiPosition + TechScrollOffset;
                Rectangle box = new Rectangle((int)pos.X, (int)pos.Y, Constants.TECH_TREE_BOX_WIDTH, Constants.TECH_TREE_BOX_HEIGHT);
                if (box.Contains(mouseP))
                {
                    _game.hoveredTech = i;
                    if (mouseClicked)
                    {
                        _game._techManager.HandleTechClick(i);
                    }
                    break;
                }
            }

            // Handle top bar tooltips
            if (_game._topBar.HandleTopBarTooltips(TopBarRenderer.TopBarMode.Global, _game.mousePos, _game.Turn, _game._techManager.GlobalScience, _game.Planets, _game._productionManager.CalculateProductionTurn, null, _game._productionManager.BuildGlobalProductionTooltip, null, -1, out string tt, out Vector2 tp))
            {
                _game.tooltipText = tt;
                _game.tooltipPos = tp;
            }
            else
            {
                _game.tooltipText = "";
            }
        }

        public void Draw(GameTime gameTime)
        {
            _game.GraphicsDevice.Clear(Color.Black);

            _game._topBar.DrawTopBar(_game._spriteBatch, TopBarRenderer.TopBarMode.Global, _game.Turn, _game._techManager.GlobalScience, _game.Planets, _game._productionManager.CalculateProductionTurn);

            for (int i = 0; i < _game.Techs.Count; i++)
            {
                Tech t = _game.Techs[i];
                foreach (int pr in t.Prerequisites)
                {
                    Vector2 from = _game.Techs[pr].UiPosition + TechScrollOffset;
                    Vector2 to = t.UiPosition + TechScrollOffset;
                    _game._spriteBatch.DrawLine(_game._pixel, from + new Vector2(Constants.TECH_TREE_BOX_WIDTH, Constants.TECH_TREE_BOX_HEIGHT / 2), to + new Vector2(0, Constants.TECH_TREE_BOX_HEIGHT / 2), Color.Gray);
                }
            }

            for (int i = 0; i < _game.Techs.Count; i++)
            {
                Tech t = _game.Techs[i];
                Vector2 pos = t.UiPosition + TechScrollOffset;
                Color color;
                if (t.IsResearched) color = Constants.TECH_RESEARCHED;
                else if (t.IsInProgress) color = Constants.TECH_IN_PROGRESS;
                else if (t.CanResearchTech(_game.Techs, _game._techManager.GlobalScience)) color = Constants.TECH_CAN_RESEARCH;
                else color = Constants.TECH_NOT_RESEARCHABLE;
                Rectangle box = new Rectangle((int)pos.X, (int)pos.Y, Constants.TECH_TREE_BOX_WIDTH, Constants.TECH_TREE_BOX_HEIGHT);
                UIHelpers.DrawRoundedButton(
                    _game._spriteBatch,
                    _game._pixel,
                    box,
                    t.Name.Replace(" ", "\n"),
                    color,
                    _game._bitmapFont
                );

                if (t.IsInProgress && !t.IsResearched)
                {
                    float progressRatio = MathHelper.Clamp((float)t.ResearchProgress / Math.Max(t.Cost, 1), 0f, 1f);
                    int fillWidth = (int)((box.Width - 4) * progressRatio);
                    if (fillWidth > 0)
                    {
                        Rectangle progressRect = new Rectangle(box.X + 2, box.Y + 2, fillWidth, box.Height - 4);
                        Color progressColor = Color.Lerp(Constants.TECH_IN_PROGRESS, Constants.TECH_RESEARCHED, progressRatio) * 0.75f;
                        _game._spriteBatch.Draw(_game._pixel, progressRect, progressColor);
                    }
                }
            }

            if (_game.hoveredTech >= 0)
            {
                Tech t = _game.Techs[_game.hoveredTech];
                string tt = $"{t.Name}\n{t.Description}";
                tt += !t.IsResearched ? $"\nCost: {t.Cost}\nInitial Science Cost: {t.MinScience}" : "\nResearched";
                if ((t.ResearchProgress > 0 || t.IsInProgress) && !t.IsResearched)
                    tt += $"\nProgress: {t.ResearchProgress}/{t.Cost} ({(int)(MathHelper.Clamp((float)t.ResearchProgress / Math.Max(t.Cost, 1), 0f, 1f) * 100)}%)";
                tt += t.Prerequisites.Count != 0 ? "\nPrerequisites: " + string.Join(", ", t.Prerequisites.Select(p => _game.Techs[p].Name)) : "";
                tt += "\nUnlocks: " + Tech.GetItemsUnlockedByTech(t.ID);
                tt += Tech.GetBonusesUnlockedByTech(t.ID);
                UIHelpers.DrawTooltip(_game._spriteBatch, tt, _game.mousePos, _game._bitmapFontTooltip, _game._pixel);
            }

            // Draw top bar tooltip
            if (!string.IsNullOrEmpty(_game.tooltipText))
            {
                UIHelpers.DrawTooltip(_game._spriteBatch, _game.tooltipText, _game.mousePos, _game._bitmapFontTooltip, _game._pixel);
            }
        }
    }
}
