using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BroadenHorizons
{
    public enum MessageType
    {
        Info,
        Confirm,
        Help,
        Selection,
    }

    public class MessageManager
    {
        // Layout constants
        private const float DialogWidthPercent = 0.5f;
        private const float DialogMaxHeightPercent = 0.7f;
        private const float HelpDialogWidthPercent = 0.8f;
        private const float HelpDialogMaxHeightPercent = 0.8f;
        private const int BorderPadding = 12;
        private const int ContentPadding = 12;
        private const int ButtonWidth = 110;
        private const int ButtonHeight = 42;
        private const int ButtonSpacing = 24;
        private const int CrossSize = 32;
        private const int TextTopMargin = 12;
        private const int ScrollSpeedKeyboard = 20;
        private const int ScrollSpeedWheelNotch = 30;
        private const int ScrollbarWidth = 10;
        private const int ScrollbarPadding = 2;

        // Colors (private, easy to change)
        private readonly Color DialogBackground1 = new(15, 25, 45);
        private readonly Color DialogBackground2 = new(8, 18, 35);
        private readonly Color DialogBorder1 = new(120, 200, 255);
        private readonly Color DialogBorder2 = new(70, 140, 220);
        private readonly Color DialogBorder3 = new(40, 90, 160);
        private readonly Color DialogInnerPanel = new(8, 20, 40, 240);

        public bool IsActive { get; private set; }
        public string MessageText { get; private set; }
        public MessageType Type { get; private set; }
        public Action<bool> OnResult;

        private readonly BH _game;

        private KeyboardState _prevKeyboard;
        private ButtonState _prevMouseLeftButton = ButtonState.Released;
        private bool _requireMouseRelease = false;
        private int prevScrollWheelValue = 0;
        private int scrollOffset = 0;
        private int maxScroll = 0;
        private bool needsRecalc = false;
        private string[] lines;
        private Vector2[] lineSizes;
        private int dialogHeight;
        private float dialogWidthPercent;
        private float dialogMaxHeightPercent;
        private int hoveredLineIndex = -1;
        private readonly List<Rectangle> lineHitRectangles = new();

        private List<string> selectionOptions = new List<string>();
        private List<bool> selectionSelectability = new List<bool>();
        private Action<int> onSelection;
        private Dictionary<Keys, int> selectionKeyMap = new Dictionary<Keys, int>();

        private int cargoFood;
        private int cargoMat;
        private int cargoMaxFood;
        private int cargoMaxMat;
        private int cargoCapacity;
        private int cargoSelectedRow;
        private bool isCargoSelection;
        private Action<int, int> onCargoSelected;

        private Texture2D _roundedDialogTexture;
        private bool _textureNeedsUpdate = true;

        public MessageManager(BH game)
        {
            _game = game;
        }

        public void Show(string text, MessageType type, Action<bool> onResult = null)
        {
            MessageText = string.IsNullOrEmpty(text) ? "No message provided" : text;
            Type = type;
            IsActive = true;
            OnResult = onResult;
            scrollOffset = 0;
            needsRecalc = true;
            dialogHeight = 0;
            _textureNeedsUpdate = true;
            dialogWidthPercent = type == MessageType.Help ? HelpDialogWidthPercent : DialogWidthPercent;
            dialogMaxHeightPercent = type == MessageType.Help ? HelpDialogMaxHeightPercent : DialogMaxHeightPercent;
            _requireMouseRelease = true;
        }

        public void ShowSelection(string title, List<string> options, Action<int> onSelection, List<bool> selectability = null)
        {
            if (options == null || options.Count == 0)
            {
                Show("No options available.", MessageType.Info);
                return;
            }

            selectionSelectability = selectability ?? Enumerable.Repeat(true, options.Count).ToList();
            int falseCount = selectionSelectability.Count(item => item == false);
            
            string messageText = $"{title}\n";
            messageText += "Press A-" + 
                          Convert.ToChar('A' + Math.Min(25, options.Count - 1 - falseCount)) + 
                          " or click an option, Escape to cancel\n\n";

            for (int i = 0; i < options.Count; i++)
            {
                char letter = (char)('A' + i);
                string prefix = selectionSelectability[i] ? letter.ToString() : "(-)";
                messageText += $"{prefix}: {options[i]}\n";
            }

            // Prepare key mapping - only map selectable options
            int maxOptions = Math.Min(26, options.Count);
            selectionKeyMap.Clear();
            for (int i = 0; i < maxOptions; i++)
            {
                if (selectionSelectability[i])
                    selectionKeyMap[Keys.A + i] = i;
            }

            selectionOptions = options;
            this.onSelection = onSelection;
            Show(messageText, MessageType.Selection);
        }

        public void ShowFreighterCargoSelection(string title, int maxFood, int maxMat, int capacity, Action<int, int> onCargoSelected)
        {
            cargoFood = 0;
            cargoMat = 0;
            cargoMaxFood = maxFood;
            cargoMaxMat = maxMat;
            cargoCapacity = capacity;
            cargoSelectedRow = 0;
            isCargoSelection = true;
            this.onCargoSelected = onCargoSelected;

            UpdateCargoMessage(title);
            Show(MessageText, MessageType.Selection);
        }

        private void UpdateCargoMessage(string title = null)
        {
            string currentTitle = title ?? MessageText?.Split('\n')[0] ?? string.Empty;
            int usedCapacity = cargoFood + cargoMat;
            int availableCapacity = Math.Max(0, cargoCapacity - usedCapacity);

            string foodLine = cargoSelectedRow == 0
                ? $"-> Food: {cargoFood}/{cargoMaxFood}"
                : $"   Food: {cargoFood}/{cargoMaxFood}";
            string matLine = cargoSelectedRow == 1
                ? $"-> Materials: {cargoMat}/{cargoMaxMat}"
                : $"   Materials: {cargoMat}/{cargoMaxMat}";

            MessageText = $"{currentTitle}\n\n" +
                          $"{foodLine}\n" +
                          $"{matLine}\n\n" +
                          $"Total: {usedCapacity}/{cargoCapacity}   Remaining: {availableCapacity}\n\n" +
                          $"Use Up/Down or click to select, Left/Right to change, Enter to confirm, Esc to cancel";

            needsRecalc = true;
        }

        private bool IsSelectionLineSelectable(int lineIndex)
        {
            if (lines == null || lineIndex < 0 || lineIndex >= lines.Length)
                return false;

            if (isCargoSelection)
                return lineIndex == 2 || lineIndex == 3;

            const int optionStart = 3;
            int optionIndex = lineIndex - optionStart;
            return optionIndex >= 0 && optionIndex < selectionOptions.Count && selectionSelectability[optionIndex];
        }

        private int SelectionLineToOptionIndex(int lineIndex)
        {
            const int optionStart = 3;
            return lineIndex - optionStart;
        }

        public void Dismiss(bool result = false)
        {
            IsActive = false;
            needsRecalc = false;
            lines = null;
            lineSizes = null;
            dialogHeight = 0;
            scrollOffset = 0;
            maxScroll = 0;
            _textureNeedsUpdate = true;

            if (Type == MessageType.Selection)
            {
                selectionOptions.Clear();
                selectionSelectability.Clear();
                selectionKeyMap.Clear();
                onSelection = null;
                isCargoSelection = false;
                onCargoSelected = null;
                cargoFood = cargoMat = cargoMaxFood = cargoMaxMat = cargoCapacity = cargoSelectedRow = 0;
            }

            OnResult?.Invoke(result);
            OnResult = null;

            _roundedDialogTexture?.Dispose();
            _roundedDialogTexture = null;
        }

        private Texture2D CreateDialogRoundedTexture(int width, int height)
        {
            if (width < 80 || height < 60)
            {
                // Tiny fallback
                Texture2D tex = new Texture2D(_game.GraphicsDevice, width, height);
                Color[] data = new Color[width * height];
                for (int i = 0; i < data.Length; i++) data[i] = DialogBackground1;
                tex.SetData(data);
                return tex;
            }

            int minDim = Math.Min(width, height);
            int borderRadius = Math.Max(6, Math.Min(18, minDim / 4));
            int borderThickness = Math.Max(3, Math.Min(7, borderRadius - 5));
            int borderShadow = Math.Max(2, Math.Min(5, borderRadius / 3));

            if (borderThickness + borderRadius > height / 2 || borderThickness + borderRadius > width / 2)
            {
                borderRadius = Math.Max(6, (minDim / 2) - borderThickness - 4);
            }

            var backgroundColors = new List<Color> { DialogBackground1, DialogBackground2 };
            var borderColors = new List<Color> { DialogBorder1, DialogBorder2, DialogBorder3 };

            return CreateRoundedRectangleTexture(_game.GraphicsDevice, width, height,
                borderThickness, borderRadius, borderShadow,
                backgroundColors, borderColors, 0.35f, 0.05f);
        }

        // Original rounded rectangle code (unchanged)
        public Texture2D CreateRoundedRectangleTexture(GraphicsDevice graphics, int width, int height, int borderThickness,
            int borderRadius, int borderShadow, List<Color> backgroundColors, List<Color> borderColors,
            float initialShadowIntensity, float finalShadowIntensity)
        {
            if (backgroundColors == null || backgroundColors.Count == 0) throw new ArgumentException("Must define at least one background color.");
            if (borderColors == null || borderColors.Count == 0) throw new ArgumentException("Must define at least one border color.");
            if (borderRadius < 1) throw new ArgumentException("Must define a border radius.");
            if (borderThickness < 1) throw new ArgumentException("Must define border thickness.");
            if (borderThickness + borderRadius > height / 2 || borderThickness + borderRadius > width / 2)
                throw new ArgumentException("Border will be too thick and/or rounded to fit on the texture.");
            if (borderShadow > borderRadius) throw new ArgumentException("Border shadow must be lesser than border radius.");

            Texture2D texture = new Texture2D(graphics, width, height, false, SurfaceFormat.Color);
            Color[] color = new Color[width * height];

            for (int x = 0; x < texture.Width; x++)
            {
                for (int y = 0; y < texture.Height; y++)
                {
                    switch (backgroundColors.Count)
                    {
                        case 2:
                            color[x + width * y] = Color.Lerp(backgroundColors[0], backgroundColors[1], (float)x / (width - 1));
                            break;
                        default:
                            color[x + width * y] = backgroundColors[0];
                            break;
                    }

                    color[x + width * y] = ColorBorder(x, y, width, height, borderThickness, borderRadius, borderShadow,
                        color[x + width * y], borderColors, initialShadowIntensity, finalShadowIntensity);
                }
            }
            texture.SetData(color);
            return texture;
        }

        private Color ColorBorder(int x, int y, int width, int height, int borderThickness, int borderRadius, int borderShadow,
            Color initialColor, List<Color> borderColors, float initialShadowIntensity, float finalShadowIntensity)
        {
            Rectangle internalRectangle = new Rectangle(borderThickness + borderRadius, borderThickness + borderRadius,
                width - 2 * (borderThickness + borderRadius), height - 2 * (borderThickness + borderRadius));

            if (internalRectangle.Contains(x, y)) return initialColor;

            Vector2 origin = Vector2.Zero;
            Vector2 point = new Vector2(x, y);

            if (x < borderThickness + borderRadius)
            {
                if (y < borderRadius + borderThickness)
                    origin = new Vector2(borderRadius + borderThickness, borderRadius + borderThickness);
                else if (y > height - (borderRadius + borderThickness))
                    origin = new Vector2(borderRadius + borderThickness, height - (borderRadius + borderThickness));
                else
                    origin = new Vector2(borderRadius + borderThickness, y);
            }
            else if (x > width - (borderRadius + borderThickness))
            {
                if (y < borderRadius + borderThickness)
                    origin = new Vector2(width - (borderRadius + borderThickness), borderRadius + borderThickness);
                else if (y > height - (borderRadius + borderThickness))
                    origin = new Vector2(width - (borderRadius + borderThickness), height - (borderRadius + borderThickness));
                else
                    origin = new Vector2(width - (borderRadius + borderThickness), y);
            }
            else
            {
                if (y < borderRadius + borderThickness)
                    origin = new Vector2(x, borderRadius + borderThickness);
                else if (y > height - (borderRadius + borderThickness))
                    origin = new Vector2(x, height - (borderRadius + borderThickness));
            }

            if (!origin.Equals(Vector2.Zero))
            {
                float distance = Vector2.Distance(point, origin);
                if (distance > borderRadius + borderThickness + 1)
                    return Color.Transparent;
                else if (distance > borderRadius + 1)
                {
                    if (borderColors.Count > 2)
                    {
                        float modNum = distance - borderRadius;
                        if (modNum < borderThickness / 2f)
                            return Color.Lerp(borderColors[2], borderColors[1], modNum / (borderThickness / 2f));
                        else
                            return Color.Lerp(borderColors[1], borderColors[0], (modNum - borderThickness / 2f) / (borderThickness / 2f));
                    }
                    return borderColors.Count > 0 ? borderColors[0] : initialColor;
                }
                else if (distance > borderRadius - borderShadow + 1)
                {
                    float mod = (distance - (borderRadius - borderShadow)) / borderShadow;
                    float shadowDiff = initialShadowIntensity - finalShadowIntensity;
                    return DarkenColor(initialColor, (shadowDiff * mod) + finalShadowIntensity);
                }
            }
            return initialColor;
        }

        private Color DarkenColor(Color color, float shadowIntensity)
        {
            return Color.Lerp(color, Color.Black, shadowIntensity);
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse, BitmapFont font, Rectangle canvasRect)
        {
            if (!IsActive) return;

            if (needsRecalc)
            {
                lines = MessageText?.Split('\n') ?? Array.Empty<string>();

                lineSizes = new Vector2[lines.Length];

                for (int i = 0; i < lines.Length; i++)
                {
                    lineSizes[i] = font.MeasureString(lines[i]);
                }

                float lineHeight = 0f;

                if (lines.Length > 0)
                {
                    lineHeight = font.MeasureString("Ay").Height; // stable height sample
                }
                else
                {
                    lineHeight = font.LineSpacing;
                }

                // Add spacing buffer (prevents overlap)
                lineHeight *= 1.25f;

                float totalTextHeight = lines.Length * lineHeight;

                int contentHeight = (int)totalTextHeight + TextTopMargin;

                int extraHeight = 2 * BorderPadding + 2 * ContentPadding;

                if (Type == MessageType.Confirm)
                    extraHeight += ButtonHeight + ContentPadding;

                int maxAllowedHeight = (int)(canvasRect.Height * dialogMaxHeightPercent);

                dialogHeight = Math.Min(contentHeight + extraHeight, maxAllowedHeight);

                // Optional minimum height (prevents ugly tiny dialogs)
                dialogHeight = Math.Max(dialogHeight, 100);

                int contentRectHeight = dialogHeight
                    - 2 * BorderPadding
                    - 2 * ContentPadding
                    - (Type == MessageType.Confirm ? ButtonHeight + ContentPadding : 0);

                maxScroll = Math.Max(0, (int)(totalTextHeight - contentRectHeight));

                needsRecalc = false;
                _textureNeedsUpdate = true;
            }

            // Calculate rectangles
            int dialogW = (int)(canvasRect.Width * dialogWidthPercent);
            int dialogX = (canvasRect.Width - dialogW) / 2;
            int dialogY = (canvasRect.Height - dialogHeight) / 2;
            Rectangle outerRect = new Rectangle(dialogX, dialogY, dialogW, dialogHeight);
            Rectangle innerRect = new Rectangle(outerRect.X + BorderPadding, outerRect.Y + BorderPadding,
                outerRect.Width - 2 * BorderPadding, outerRect.Height - 2 * BorderPadding);
            Rectangle contentRect = new Rectangle(innerRect.X + ContentPadding, innerRect.Y + ContentPadding,
                innerRect.Width - 2 * ContentPadding, innerRect.Height - 2 * ContentPadding);
            Rectangle crossRect = new Rectangle(outerRect.Right - CrossSize - BorderPadding, outerRect.Top + BorderPadding, CrossSize, CrossSize);

            int buttonsTotalW = ButtonWidth * 2 + ButtonSpacing;
            int buttonsX = outerRect.X + (outerRect.Width - buttonsTotalW) / 2;
            int buttonsY = outerRect.Bottom - ButtonHeight - BorderPadding - ContentPadding;
            Rectangle yesRect = new Rectangle(buttonsX, buttonsY, ButtonWidth, ButtonHeight);
            Rectangle noRect = new Rectangle(buttonsX + ButtonWidth + ButtonSpacing, buttonsY, ButtonWidth, ButtonHeight);

            // Scroll with mouse wheel or up/down keys
            if (Type == MessageType.Help || maxScroll > 0)
            {
                int scrollDelta = mouse.ScrollWheelValue - prevScrollWheelValue;
                if (scrollDelta != 0)
                {
                    scrollOffset -= scrollDelta / 120 * ScrollSpeedWheelNotch;
                }
                prevScrollWheelValue = mouse.ScrollWheelValue;

                if (keyboard.IsKeyDown(Keys.Down)) scrollOffset += ScrollSpeedKeyboard;
                if (keyboard.IsKeyDown(Keys.Up)) scrollOffset -= ScrollSpeedKeyboard;

                scrollOffset = Math.Max(0, Math.Min(scrollOffset, maxScroll));
            }

            hoveredLineIndex = -1;
            lineHitRectangles.Clear();
            if (Type == MessageType.Selection && lines != null)
            {
                float baseLineHeight = font.MeasureString("Ay").Height;
                float lineHeight = baseLineHeight * 1.25f;
                int hitWidth = contentRect.Width - (maxScroll > 0 ? ScrollbarWidth + ScrollbarPadding * 2 : 0);

                float y = contentRect.Y + TextTopMargin - scrollOffset;
                for (int i = 0; i < lines.Length; i++)
                {
                    float lineY = y + i * lineHeight;
                    Rectangle lineRect = new Rectangle(contentRect.X, (int)Math.Round(lineY), hitWidth, (int)Math.Round(lineHeight));
                    lineHitRectangles.Add(lineRect);

                    if (lineRect.Contains(mouse.Position) && IsSelectionLineSelectable(i))
                    {
                        hoveredLineIndex = i;
                    }
                }
            }

            // Mouse click detection
            if (_requireMouseRelease)
            {
                if (mouse.LeftButton == ButtonState.Released)
                {
                    _requireMouseRelease = false;
                }
            }

            bool mouseClicked = !_requireMouseRelease && mouse.LeftButton == ButtonState.Pressed && _prevMouseLeftButton == ButtonState.Released;

            if (Type == MessageType.Info)
            {
                if (keyboard.IsKeyDown(Keys.Escape))
                    Dismiss();
                if (mouseClicked && crossRect.Contains(mouse.Position))
                    Dismiss();
            }
            else if (Type == MessageType.Confirm)
            {
                if (keyboard.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter))
                    Dismiss(true);
                if (keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
                    Dismiss(false);
                if (keyboard.IsKeyDown(Keys.Y) && !_prevKeyboard.IsKeyDown(Keys.Y))
                    Dismiss(true);
                if (keyboard.IsKeyDown(Keys.N) && !_prevKeyboard.IsKeyDown(Keys.N))
                    Dismiss(false);
                if (mouseClicked && yesRect.Contains(mouse.Position))
                    Dismiss(true);
                if (mouseClicked && noRect.Contains(mouse.Position))
                    Dismiss(false);
            }
            else if (Type == MessageType.Help)
            {
                if (keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
                    Dismiss();
            }

            if (Type == MessageType.Selection)
            {
                if (isCargoSelection)
                {
                    bool updated = false;

                    if (keyboard.IsKeyDown(Keys.Up) && !_prevKeyboard.IsKeyDown(Keys.Up))
                    {
                        cargoSelectedRow = Math.Max(0, cargoSelectedRow - 1);
                        updated = true;
                    }

                    if (keyboard.IsKeyDown(Keys.Down) && !_prevKeyboard.IsKeyDown(Keys.Down))
                    {
                        cargoSelectedRow = Math.Min(1, cargoSelectedRow + 1);
                        updated = true;
                    }

                    if (keyboard.IsKeyDown(Keys.Right) && !_prevKeyboard.IsKeyDown(Keys.Right))
                    {
                        if (cargoSelectedRow == 0 && cargoFood < cargoMaxFood && cargoFood + cargoMat < cargoCapacity)
                        {
                            cargoFood++;
                            updated = true;
                        }
                        else if (cargoSelectedRow == 1 && cargoMat < cargoMaxMat && cargoFood + cargoMat < cargoCapacity)
                        {
                            cargoMat++;
                            updated = true;
                        }
                    }

                    if (keyboard.IsKeyDown(Keys.Left) && !_prevKeyboard.IsKeyDown(Keys.Left))
                    {
                        if (cargoSelectedRow == 0 && cargoFood > 0)
                        {
                            cargoFood--;
                            updated = true;
                        }
                        else if (cargoSelectedRow == 1 && cargoMat > 0)
                        {
                            cargoMat--;
                            updated = true;
                        }
                    }

                    if (mouseClicked && hoveredLineIndex >= 0)
                    {
                        if (hoveredLineIndex == 2 && cargoSelectedRow != 0)
                        {
                            cargoSelectedRow = 0;
                            updated = true;
                        }
                        else if (hoveredLineIndex == 3 && cargoSelectedRow != 1)
                        {
                            cargoSelectedRow = 1;
                            updated = true;
                        }
                    }

                    if (keyboard.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter))
                    {
                        int selectedFood = cargoFood;
                        int selectedMat = cargoMat;
                        var callback = onCargoSelected;
                        Dismiss();
                        callback?.Invoke(selectedFood, selectedMat);
                        return;
                    }

                    if (keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
                    {
                        Dismiss();
                        return;
                    }

                    if (updated)
                    {
                        UpdateCargoMessage();
                    }
                }
                else
                {
                    if (mouseClicked && hoveredLineIndex >= 0)
                    {
                        int selectedIndex = SelectionLineToOptionIndex(hoveredLineIndex);
                        if (selectedIndex >= 0 && selectedIndex < selectionOptions.Count && selectionSelectability[selectedIndex])
                        {
                            var callback = onSelection;
                            Dismiss();
                            callback?.Invoke(selectedIndex);
                            return;
                        }
                    }

                    foreach (var kvp in selectionKeyMap)
                    {
                        if (keyboard.IsKeyDown(kvp.Key) && !_prevKeyboard.IsKeyDown(kvp.Key))
                        {
                            var callback = onSelection;
                            int selectedIndex = kvp.Value;
                            Dismiss();
                            callback?.Invoke(selectedIndex);
                            return;
                        }
                    }

                    if (keyboard.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
                    {
                        var callback = onSelection;
                        Dismiss();
                        callback?.Invoke(-1);
                    }
                }
            }

            _prevKeyboard = keyboard;
            _prevMouseLeftButton = mouse.LeftButton;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, BitmapFont font, int screenWidth, int screenHeight, GraphicsDevice graphicsDevice)
        {
            if (!IsActive || spriteBatch == null || font == null || graphicsDevice == null || dialogHeight <= 0)
                return;

            int dialogW = (int)(screenWidth * dialogWidthPercent);
            int dialogX = (screenWidth - dialogW) / 2;
            int dialogY = (screenHeight - dialogHeight) / 2;
            Rectangle outerRect = new Rectangle(dialogX, dialogY, dialogW, dialogHeight);

            if (_textureNeedsUpdate || _roundedDialogTexture == null ||
                _roundedDialogTexture.Width != outerRect.Width || _roundedDialogTexture.Height != outerRect.Height)
            {
                _roundedDialogTexture?.Dispose();
                _roundedDialogTexture = CreateDialogRoundedTexture(outerRect.Width, outerRect.Height);
                _textureNeedsUpdate = false;
            }

            spriteBatch.Draw(_roundedDialogTexture, outerRect, Color.White);

            Rectangle innerRect = new Rectangle(outerRect.X + BorderPadding, outerRect.Y + BorderPadding,
                outerRect.Width - 2 * BorderPadding, outerRect.Height - 2 * BorderPadding);
            int contentHeightDraw = innerRect.Height - 2 * ContentPadding;

            if (Type == MessageType.Confirm)
            {
                contentHeightDraw -= (ButtonHeight + ContentPadding);
            }

            Rectangle contentRect = new Rectangle(
                innerRect.X + ContentPadding,
                innerRect.Y + ContentPadding,
                innerRect.Width - 2 * ContentPadding,
                contentHeightDraw
            );

            spriteBatch.Draw(pixel, innerRect, DialogInnerPanel);

            if (Type == MessageType.Selection && hoveredLineIndex >= 0 && hoveredLineIndex < lineHitRectangles.Count)
            {
                Rectangle highlightRect = lineHitRectangles[hoveredLineIndex];
                if (highlightRect.Intersects(contentRect))
                {
                    spriteBatch.Draw(pixel, new Rectangle(highlightRect.X, highlightRect.Y, highlightRect.Width, highlightRect.Height),
                        new Color(10, 100, 45, 100));
                }
            }

            // Scrollbar
            if (maxScroll > 0)
            {
                int scrollbarX = contentRect.Right - ScrollbarWidth - ScrollbarPadding;
                int scrollbarY = contentRect.Y + ScrollbarPadding;
                int scrollbarHeight = contentRect.Height - 2 * ScrollbarPadding;
                Rectangle scrollbarTrack = new Rectangle(scrollbarX, scrollbarY, ScrollbarWidth, scrollbarHeight);
                spriteBatch.Draw(pixel, scrollbarTrack, Color.DarkGray);

                float contentHeight = maxScroll + contentRect.Height;
                float thumbRatio = contentRect.Height / contentHeight;
                int thumbHeight = Math.Max((int)(scrollbarHeight * thumbRatio), 10);
                float thumbYRatio = scrollOffset / (float)maxScroll;
                int thumbY = scrollbarY + (int)((scrollbarHeight - thumbHeight) * thumbYRatio);
                Rectangle scrollbarThumb = new Rectangle(scrollbarX, thumbY, ScrollbarWidth, thumbHeight);
                spriteBatch.Draw(pixel, scrollbarThumb, Color.White);
            }

            // Buttons and Cross
            Rectangle crossRect = new Rectangle(outerRect.Right - CrossSize - BorderPadding, outerRect.Top + BorderPadding, CrossSize, CrossSize);

            if (Type == MessageType.Info)
            {
                spriteBatch.Draw(pixel, crossRect, Color.Red);
                Vector2 xSize = font.MeasureString("X");
                spriteBatch.DrawString(font, "X", new Vector2(crossRect.X + (crossRect.Width - xSize.X) / 2,
                    crossRect.Y + (crossRect.Height - xSize.Y) / 2), Color.White);
            }
            else if (Type == MessageType.Confirm)
            {
                int buttonsTotalW = ButtonWidth * 2 + ButtonSpacing;
                int buttonsX = outerRect.X + (outerRect.Width - buttonsTotalW) / 2;
                int buttonsY = outerRect.Bottom - ButtonHeight - BorderPadding - ContentPadding;

                Rectangle yesRect = new Rectangle(buttonsX, buttonsY, ButtonWidth, ButtonHeight);
                Rectangle noRect = new Rectangle(buttonsX + ButtonWidth + ButtonSpacing, buttonsY, ButtonWidth, ButtonHeight);

                UIHelpers.DrawRoundedButton(spriteBatch, pixel, yesRect, "Yes", Color.Green, font);
                UIHelpers.DrawRoundedButton(spriteBatch, pixel, noRect, "No", Color.OrangeRed, font);
            }

            // Text with scissor
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState() { ScissorTestEnable = true });
            graphicsDevice.ScissorRectangle = contentRect;

            if (lines != null && lineSizes != null)
            {
                float baseLineHeight = font.MeasureString("Ay").Height;
                float lineHeight = baseLineHeight * 1.25f;

                float y = contentRect.Y + TextTopMargin - scrollOffset;

                for (int i = 0; i < lines.Length; i++)
                {
                    float lineY = y + i * lineHeight;
                    if (lineY + lineHeight >= contentRect.Y && lineY <= contentRect.Bottom)
                    {
                        float x = Type == MessageType.Help
                            ? contentRect.X + ContentPadding
                            : contentRect.X + (contentRect.Width - lineSizes[i].X) / 2;
                        float snappedX = (float)Math.Round(x);
                        float snappedY = (float)Math.Round(lineY);

                        spriteBatch.DrawString(font, lines[i], new Vector2(snappedX, snappedY), Color.White);
                    }
                }
            }
            spriteBatch.End();
            spriteBatch.Begin();
        }
    }
}