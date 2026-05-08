using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;

namespace BroadenHorizons.Screens
{
    public class MainMenuScreen
    {
        private BH _game;

        public MainMenuScreen(BH game)
        {
            _game = game;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            _game.tooltipText = "";
            if (mouse.LeftButton == ButtonState.Pressed && _game._prevMouse.LeftButton == ButtonState.Released)
            {
                int buttonYStart = Constants.SCREEN_HEIGHT - Constants.MENU_BUTTON_BOTTOM_MARGIN - Constants.MENU_BUTTON_HEIGHT;
                int totalButtonWidth = 4 * Constants.MENU_BUTTON_WIDTH + 3 * Constants.MENU_BUTTON_SPACING;
                int buttonXStart = (Constants.SCREEN_WIDTH - totalButtonWidth) / 2;

                Rectangle newGameButton = new Rectangle(buttonXStart, buttonYStart, Constants.MENU_BUTTON_WIDTH, Constants.MENU_BUTTON_HEIGHT);
                Rectangle loadGameButton = new Rectangle(buttonXStart + Constants.MENU_BUTTON_WIDTH + Constants.MENU_BUTTON_SPACING, buttonYStart, Constants.MENU_BUTTON_WIDTH, Constants.MENU_BUTTON_HEIGHT);
                Rectangle saveGameButton = new Rectangle(buttonXStart + 2 * (Constants.MENU_BUTTON_WIDTH + Constants.MENU_BUTTON_SPACING), buttonYStart, Constants.MENU_BUTTON_WIDTH, Constants.MENU_BUTTON_HEIGHT);
                Rectangle exitGameButton = new Rectangle(buttonXStart + 3 * (Constants.MENU_BUTTON_WIDTH + Constants.MENU_BUTTON_SPACING), buttonYStart, Constants.MENU_BUTTON_WIDTH, Constants.MENU_BUTTON_HEIGHT);

                if (newGameButton.Contains(mouse.Position))
                {
                    _game.CurrentState = BH.GameState.Preferences;
                }
                else if (loadGameButton.Contains(mouse.Position))
                {
                    _game.LoadGame(gameTime);
                }
                else if (saveGameButton.Contains(mouse.Position) && _game.PrevState != BH.GameState.MainMenu)
                {
                    _game.SaveGame(gameTime);
                }
                else if (exitGameButton.Contains(mouse.Position))
                {
                    _game.messageManager.Show("Are you sure you want to quit?", MessageType.Confirm, result =>
                    {
                        if (result) _game.Exit();
                    });
                }
            }

            if (keyboard.IsKeyDown(Keys.Escape) && !_game.WasKeyDown(Keys.Escape) && _game.PrevState == BH.GameState.GalaxyMap)
            {
                _game.CurrentState = BH.GameState.GalaxyMap;
            }
        }

        public void Draw(GameTime gameTime)
        {
            _game.GraphicsDevice.Clear(Color.LightBlue);
            if (_game._logoTexture != null)
            {
                Vector2 logoSize = new Vector2(_game._logoTexture.Width, _game._logoTexture.Height);
                Vector2 logoPosition = new Vector2(
                    (Constants.SCREEN_WIDTH - logoSize.X) / 2,
                    (Constants.SCREEN_HEIGHT - logoSize.Y) / 2 - 125
                );
                _game._spriteBatch.Draw(_game._logoTexture, logoPosition, null, Color.White, 0f, Vector2.Zero, Constants.SCREEN_WIDTH / _game._logoTexture.Width, SpriteEffects.None, 0f);
            }

            int buttonYStart = Constants.SCREEN_HEIGHT - Constants.MENU_BUTTON_BOTTOM_MARGIN - Constants.MENU_BUTTON_HEIGHT;
            int totalButtonWidth = 4 * Constants.MENU_BUTTON_WIDTH + 3 * Constants.MENU_BUTTON_SPACING;
            int buttonXStart = (Constants.SCREEN_WIDTH - totalButtonWidth) / 2;

            string[] buttonLabels = ["New Game", "Load Game", "Save Game", "Exit Game"];
            for (int i = 0; i < buttonLabels.Length; i++)
            {
                int x = buttonXStart + i * (Constants.MENU_BUTTON_WIDTH + Constants.MENU_BUTTON_SPACING);
                Rectangle buttonRect = new Rectangle(x, buttonYStart, Constants.MENU_BUTTON_WIDTH, Constants.MENU_BUTTON_HEIGHT);
                bool isSaveDisabled = i == 2 && _game.PrevState == BH.GameState.MainMenu;
                Color buttonColor = isSaveDisabled ? Color.Gray : (buttonRect.Contains(_game.mousePos) ? Constants.MenuSelectedColor : Constants.MenuNonSelectedColor);
                UIHelpers.DrawRoundedButton(
                    _game._spriteBatch,
                    _game._pixel,
                    buttonRect,
                    buttonLabels[i],
                    buttonColor,
                    _game._bitmapFont
                );
                Vector2 textSize = _game._bitmapFont.MeasureString(buttonLabels[i]);
                Vector2 textPos = new Vector2(
                    x + (Constants.MENU_BUTTON_WIDTH - textSize.X) / 2,
                    buttonYStart + (Constants.MENU_BUTTON_HEIGHT - textSize.Y) / 2
                );
                _game._spriteBatch.DrawString(_game._bitmapFont, buttonLabels[i], textPos, isSaveDisabled ? Color.DarkGray : Color.White);
            }
        }
    }
}
