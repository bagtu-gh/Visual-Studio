using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;

namespace BroadenHorizons.Screens
{
    public class SaveLoadScreen
    {
        private readonly BH _game;

        public SaveLoadScreen(BH game)
        {
            _game = game;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            if (mouse.LeftButton == ButtonState.Pressed && _game._prevMouse.LeftButton == ButtonState.Released)
            {
                Point clickPoint = mouse.Position;
                Rectangle cancelButton = GetCancelButtonRect();
                if (cancelButton.Contains(clickPoint))
                {
                    _game.CurrentState = BH.GameState.MainMenu;
                    return;
                }

                for (int i = 0; i < Constants.MAX_SAVE_SLOTS; i++)
                {
                    Rectangle slotRect = GetSlotRect(i);
                    if (slotRect.Contains(clickPoint))
                    {
                        HandleSlotClick(i, gameTime);
                        return;
                    }
                }
            }

            if (keyboard.IsKeyDown(Keys.Escape) && !_game.WasKeyDown(Keys.Escape))
            {
                _game.CurrentState = BH.GameState.MainMenu;
            }
        }

        private void HandleSlotClick(int slotIndex, GameTime gameTime)
        {
            var slotInfo = _game.GetSaveSlotInfo(slotIndex);
            if (_game.CurrentSaveLoadMode == BH.SaveLoadMode.Load)
            {
                if (!slotInfo.Exists)
                {
                    _game._messageManager.Show("This slot is empty.", MessageType.Info);
                    return;
                }

                _game.LoadGameFromSlot(slotIndex, gameTime);
            }
            else
            {
                if (slotInfo.Exists)
                {
                    _game._messageManager.Show($"Overwrite save slot {slotIndex + 1}?", MessageType.Confirm, result =>
                    {
                        if (result)
                        {
                            _game.SaveGameToSlot(slotIndex, gameTime);
                        }
                    });
                }
                else
                {
                    _game.SaveGameToSlot(slotIndex, gameTime);
                }
            }
        }

        public void Draw(GameTime gameTime)
        {
            _game.GraphicsDevice.Clear(Constants.BACKGROUND_COLOR);

            string header = _game.CurrentSaveLoadMode == BH.SaveLoadMode.Load ? "Load Game" : "Save Game";
            string instructions = _game.CurrentSaveLoadMode == BH.SaveLoadMode.Load
                ? "Choose a save slot to load your game."
                : "Choose a save slot to store your game. Existing slots will be overwritten.";

            _game._spriteBatch.DrawString(_game._bitmapFontBig, header, new Vector2(80, 50), Color.Black);
            _game._spriteBatch.DrawString(_game._bitmapFont, instructions, new Vector2(80, 120), Color.Black);

            var slotInfos = _game.GetSaveSlotInfos();
            for (int i = 0; i < slotInfos.Count; i++)
            {
                DrawSaveSlot(i, slotInfos[i]);
            }

            Rectangle cancelButton = GetCancelButtonRect();
            Color cancelColor = cancelButton.Contains(_game.mousePos) ? Constants.MenuSelectedColor : Constants.MenuNonSelectedColor;
            UIHelpers.DrawRoundedButton(
                _game._spriteBatch,
                _game._pixel,
                cancelButton,
                "Back",
                cancelColor,
                _game._bitmapFont);

            Vector2 cancelTextSize = _game._bitmapFont.MeasureString("Back");
            Vector2 cancelTextPos = new Vector2(
                cancelButton.X + (cancelButton.Width - cancelTextSize.X) / 2,
                cancelButton.Y + (cancelButton.Height - cancelTextSize.Y) / 2);
            _game._spriteBatch.DrawString(_game._bitmapFont, "Back", cancelTextPos, Color.White);

            string footer = "Press Esc to return.";
            Vector2 footerSize = _game._bitmapFont.MeasureString(footer);
            _game._spriteBatch.DrawString(_game._bitmapFont, footer, new Vector2((Constants.SCREEN_WIDTH - footerSize.X) / 2, cancelButton.Y + cancelButton.Height + 15), Color.LightGray);
        }

        private void DrawSaveSlot(int slotIndex, BH.SaveSlotInfo slotInfo)
        {
            Rectangle slotRect = GetSlotRect(slotIndex);
            bool isHovered = slotRect.Contains(_game.mousePos);
            Color slotColor = isHovered ? Constants.MenuSelectedColor : Constants.MenuNonSelectedColor;

            UIHelpers.DrawRoundedButton(
                _game._spriteBatch,
                _game._pixel,
                slotRect,
                string.Empty,
                slotColor,
                _game._bitmapFont);

            string title = slotInfo.Exists
                ? $"Slot {slotIndex + 1} - Turn {slotInfo.Turn}"
                : $"Slot {slotIndex + 1} - Empty";

            string dateLine = slotInfo.Exists
                ? $"Saved: {slotInfo.SavedAtUtc.ToLocalTime():g}"
                : string.Empty;

            string promptLine;
            if (_game.CurrentSaveLoadMode == BH.SaveLoadMode.Load)
            {
                promptLine = slotInfo.Exists
                    ? "Click to load this save."
                    : "Empty slot. Cannot load.";
            }
            else
            {
                promptLine = slotInfo.Exists
                    ? "Click to overwrite this save."
                    : "Click to save here.";
            }

            Vector2 titlePos = new Vector2(slotRect.X + 20, slotRect.Y + 10);
            Vector2 datePos = new Vector2(slotRect.X + 20, slotRect.Y + 40);
            Vector2 promptPos = new Vector2(slotRect.X + 20, slotRect.Y + 70);

            _game._spriteBatch.DrawString(_game._bitmapFont, title, titlePos, Color.White);
            if (!string.IsNullOrEmpty(dateLine))
            {
                _game._spriteBatch.DrawString(_game._bitmapFont, dateLine, datePos, Color.LightGray);
            }
            _game._spriteBatch.DrawString(_game._bitmapFont, promptLine, promptPos, Color.LightGray);
        }

        private static Rectangle GetSlotRect(int slotIndex)
        {
            int slotWidth = 700;
            int slotHeight = 110;
            int slotSpacing = 18;
            int x = (Constants.SCREEN_WIDTH - slotWidth) / 2;
            int y = 180 + slotIndex * (slotHeight + slotSpacing);
            return new Rectangle(x, y, slotWidth, slotHeight);
        }

        private static Rectangle GetCancelButtonRect()
        {
            int width = 160;
            int height = 44;
            int x = (Constants.SCREEN_WIDTH - width) / 2;
            int y = 180 + Constants.MAX_SAVE_SLOTS * 128;
            return new Rectangle(x, y, width, height);
        }
    }
}
