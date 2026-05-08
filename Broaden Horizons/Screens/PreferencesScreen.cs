using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;

namespace BroadenHorizons.Screens
{
    public class PreferencesScreen(BH game)
    {
        private readonly BH _game = game;
        private readonly int[] _planetOptions = [15, 20, 25, 30];
        private readonly int[] _foodOptions = [6, 8, 10];
        private const int TOP_MARGIN = 180;
        private const int LEFT_MARGIN = 120;
        private const int BUTTON_WIDTH = 150;
        private const int BUTTON_HEIGHT = 50;
        private const int BUTTON_SPACING = 20;
        private const int BUTTON_VERT_DIST = 150;
        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            _game.tooltipText = "";

            if (mouse.LeftButton == ButtonState.Pressed && _game._prevMouse.LeftButton == ButtonState.Released)
            {
                for (int i = 0; i < _planetOptions.Length; i++)
                {
                    var rect = new Rectangle(LEFT_MARGIN + i * (BUTTON_WIDTH + BUTTON_SPACING), TOP_MARGIN, BUTTON_WIDTH, BUTTON_HEIGHT);
                    if (rect.Contains(mouse.Position))
                    {
                        Constants.NUM_PLANETS = _planetOptions[i];
                        return;
                    }
                }

                for (int i = 0; i < _foodOptions.Length; i++)
                {
                    var rect = new Rectangle(LEFT_MARGIN + i * (BUTTON_WIDTH + BUTTON_SPACING), TOP_MARGIN + BUTTON_VERT_DIST, BUTTON_WIDTH, BUTTON_HEIGHT);
                    if (rect.Contains(mouse.Position))
                    {
                        Constants.STARTING_FOOD = _foodOptions[i];
                        return;
                    }
                }

                var startGameButton = new Rectangle((Constants.SCREEN_WIDTH - BUTTON_WIDTH) / 2, 520, BUTTON_WIDTH, BUTTON_HEIGHT);
                if (startGameButton.Contains(mouse.Position))
                {
                    _game.NewGame();
                }
            }

            if (keyboard.IsKeyDown(Keys.Escape) && !_game.WasKeyDown(Keys.Escape))
            {
                _game.CurrentState = BH.GameState.MainMenu;
            }
        }

        public void Draw(GameTime gameTime)
        {
            string title = "PREFERENCES";
            _game.GraphicsDevice.Clear(Color.LightBlue);
            Vector2 titleSize = _game._bitmapFontBig.MeasureString(title);
            _game._spriteBatch.DrawString(_game._bitmapFontBig, title, new Vector2((Constants.SCREEN_WIDTH - titleSize.X) / 2, 50), Color.Black);

            string planetLabel = "Number of Planets";
            _game._spriteBatch.DrawString(_game._bitmapFont, planetLabel, new Vector2(LEFT_MARGIN, 130), Color.Black);

            for (int i = 0; i < _planetOptions.Length; i++)
            {
                string label = _planetOptions[i].ToString();
                var rect = new Rectangle(LEFT_MARGIN + i * (BUTTON_WIDTH + BUTTON_SPACING), TOP_MARGIN, BUTTON_WIDTH, BUTTON_HEIGHT);
                Color color = Constants.NUM_PLANETS == _planetOptions[i] ? Constants.MenuSelectedColor : Constants.MenuNonSelectedColor;
                UIHelpers.DrawRoundedButton(
                    _game._spriteBatch,
                    _game._pixel,
                    rect,
                    label,
                    color,
                    _game._bitmapFont,
                    Constants.NUM_PLANETS == _planetOptions[i]
                );
            }

            string foodLabel = "Starting Food";
            _game._spriteBatch.DrawString(_game._bitmapFont, foodLabel, new Vector2(LEFT_MARGIN, 280), Color.Black);

            for (int i = 0; i < _foodOptions.Length; i++)
            {
                string label = _foodOptions[i].ToString();
                var rect = new Rectangle(LEFT_MARGIN + i * (BUTTON_WIDTH + BUTTON_SPACING), TOP_MARGIN + BUTTON_VERT_DIST, BUTTON_WIDTH, BUTTON_HEIGHT);
                Color color = Constants.STARTING_FOOD == _foodOptions[i] ? Constants.MenuSelectedColor : Constants.MenuNonSelectedColor;
                UIHelpers.DrawRoundedButton(
                    _game._spriteBatch,
                    _game._pixel,
                    rect,
                    label,
                    color,
                    _game._bitmapFont,
                    Constants.STARTING_FOOD == _foodOptions[i]
                );
            }

            var startGameButton = new Rectangle((Constants.SCREEN_WIDTH - BUTTON_WIDTH) / 2, 520, BUTTON_WIDTH, BUTTON_HEIGHT);
            UIHelpers.DrawRoundedButton(
                _game._spriteBatch,
                _game._pixel,
                startGameButton,
                "Start Game",
                Color.DarkSlateGray,
                _game._bitmapFont
            );

            string currentSettings = $"Current: {Constants.NUM_PLANETS} planets, {Constants.STARTING_FOOD} food";
            Vector2 currentTextSize = _game._bitmapFont.MeasureString(currentSettings);
            _game._spriteBatch.DrawString(_game._bitmapFont, currentSettings, new Vector2((Constants.SCREEN_WIDTH - currentTextSize.X) / 2, 460), Color.Black);
        }
    }
}
