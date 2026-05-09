using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace BroadenHorizons
{
    public static class Constants
    {
        public const int SCREEN_WIDTH = 1600;
        public const int SCREEN_HEIGHT = 900;
        public const int MIN_PLANET_DISTANCE = 150;
        public static int NUM_PLANETS = 20;
        public const int TERRAFORMER_TEMP_CHANGE = 1;
        public const float POPULATION_BASE_GROWTH = 0.1f;
        public const float POPULATION_FOOD_GROWTH = 0.05f;
        public const int SCROLL_SPEED = 20;
        public const int MIN_PLANET_DIMENS = 12;
        public const int MAX_PLANET_DIMENS = 36;
        public const int NON_EXISTING_HABTITAT = -99;
        public const float PLANET_GALAXY_SCALE = 75f;
        public const int HEX_SIZE = 110;
        public const int UNIT_MENU_MIN_X = 25;
        public const int UNIT_MENU_MAX_X = 115;
        public const int UNIT_MENU_MIN_Y = 100;
        public const int UNIT_MENU_MAX_Y = 190;
        public const float UNEXPLORED_SCALE = 0.08f;
        public const float UNIT_SCALE = 0.05f;
        public const float MENU_UNIT_SCALE = 0.11f;
        public const int NUM_STARS = 300;
        public const int TURNS_TO_EXPLORE1 = 2;
        public const int TURNS_TO_EXPLORE2 = 4;
        public const int TURNS_TO_EXPLORE3 = 5;
        public const int MENU_BUTTON_WIDTH = 200;
        public const int MENU_BUTTON_HEIGHT = 50;
        public const int MENU_BUTTON_SPACING = 40;
        public const int MENU_BUTTON_BOTTOM_MARGIN = 60;

        public const int TOP_BAR_HEIGHT = 60;
        public const int TOP_BAR_BUTTON_WIDTH = 150;
        public const int TOP_BAR_BUTTON_HEIGHT = 40;
        public const int TOP_BAR_BUTTON_SPACING = 20;
        public static readonly Color TOP_BAR_BUTTON_COLOR = Color.Blue;
        public static readonly Color TOP_BAR_BUTTON_HIGHLIGHT_COLOR = Color.DarkOrchid;
        public static readonly Color TOP_BAR_BUTTON_COLOR_MSG = Color.Red;
        public static readonly Color TOP_BAR_BUTTON_HIGHLIGHT_COLOR_MSG = Color.MediumVioletRed;

        public const int TECH_TREE_BOX_WIDTH = 175;
        public const int TECH_TREE_BOX_HEIGHT = 80;
        public const int TECH_TREE_HORIZ_MARGIN = 50;
        public const int TECH_TREE_XDISTANCE = 300;
        public static readonly Color TECH_RESEARCHED = Color.Green;
        public static readonly Color TECH_IN_PROGRESS = Color.Yellow;
        public static readonly Color TECH_CAN_RESEARCH = Color.LightCyan;
        public static readonly Color TECH_NOT_RESEARCHABLE = Color.Gray;

        public static readonly Color MenuSelectedColor = Color.Green;
        public static readonly Color MenuNonSelectedColor = Color.OrangeRed;

        public const int RECRUIT_MENU_X = 1250;
        public const int RECRUIT_MENU_Y = 100;
        public const int RECRUIT_HEIGHT = 90;

        public const int PLANET_TOP_BAR_IMGTEXT_PAD = 10;
        public const int PLANET_TOP_BAR_LEFT_PAD = 15;
        public const int PLANET_TOP_BAR_TEXT_DIST = 20;

        // Defaults for planet production
        public static int STARTING_FOOD = 6;
        public const int STARTING_MATERIALS = 2;
        public const int STARTING_SCIENCE = 4;
        public const int STARTING_ENERGY = 200;
        public const int STARTING_POPULATION = 60;
        public const int DEFAULT_PLANET_TEXTURE = 2;

        public static readonly int REGION_BONUS_MAX_PER_PLANET = 6; // Divisor for planet dimensions to get max region bonuses
        public static readonly int MIN_REGION_BONUSES = 1;
        public static readonly int STARTING_PLANET_MIN_REGION_BONUSES = 2;

        // Planet gen tuning
        public const int MAX_PLANET_ATTEMPTS = 30;
        public const int STARTING_PLANET_MIN_SIZE = 22;

        //Ships
        public const int CITY_BUILD_TURNS = 5;
        public const int COLONY_MATERIALS = 100;
        public const int COLONY_FOOD = 50;

        // File paths (centralize)
        private static string _helpText;
        public static string HELP_TEXT
        {
            get
            {
                if (_helpText == null)
                {
                    try
                    {
                        // Try relative path first
                        _helpText = File.ReadAllText("Content/help_text.txt");
                    }
                    catch
                    {
                        try
                        {
                            // Try absolute path as fallback
                            string contentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "help_text.txt");
                            _helpText = File.ReadAllText(contentPath);
                        }
                        catch (Exception ex)
                        {
                            _helpText = $"Help text could not be loaded: {ex.Message}\n\nTried relative path: Content/help_text.txt\nTried absolute path: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "help_text.txt")}";
                        }
                    }
                }
                return _helpText;
            }
        }
        public const string SAVE_FILE = "game_save.json";
    }
}