using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace BroadenHorizons
{
    public class Functions
    {
        public static void GenHex(RegionData[] regionDatas)
        {
            float xIni = Constants.SCREEN_WIDTH / 2f;
            float yIni = Constants.SCREEN_HEIGHT / 2f + 50f;
            float[] cosAngles = new float[6];
            float[] sinAngles = new float[6];
            for (int i = 0; i < 6; i++)
            {
                double angle = 2 * Math.PI / 6 * i;
                cosAngles[i] = (float)Math.Cos(angle);
                sinAngles[i] = (float)Math.Sin(angle);
            }

            regionDatas[0].XC = xIni;
            regionDatas[0].YC = yIni;

            for (int n = 1; n <= 6; n++)
            {
                regionDatas[n].XC = xIni + Constants.HEX_SIZE * cosAngles[n - 1];
                regionDatas[n].YC = yIni + Constants.HEX_SIZE * sinAngles[n - 1];
            }

            for (int n = 7, j = 0; n <= 18; n++, j++)
            {
                int t = j % 2;
                int k = j / 2;
                regionDatas[n].XC = xIni + Constants.HEX_SIZE * ((2 - t) * cosAngles[k] + t * cosAngles[(k + 1) % 6]);
                regionDatas[n].YC = yIni + Constants.HEX_SIZE * ((2 - t) * sinAngles[k] + t * sinAngles[(k + 1) % 6]);
            }

            for (int n = 19, j = 0; n <= 36; n++, j++)
            {
                int t = j % 3;
                int k = j / 3;
                regionDatas[n].XC = xIni + Constants.HEX_SIZE * ((3 - t) * cosAngles[k] + t * cosAngles[(k + 1) % 6]);
                regionDatas[n].YC = yIni + Constants.HEX_SIZE * ((3 - t) * sinAngles[k] + t * sinAngles[(k + 1) % 6]);
            }
        }

        public static int GetClickedReg(RegionData[] regionDatas, int mx, int my)
        {
            float minDist = float.MaxValue;
            int closest = -1;
            float threshold = Constants.HEX_SIZE / 2f;
            for (int t = 0; t <= 36; t++)
            {
                float dx = mx - regionDatas[t].XC;
                float dy = my - regionDatas[t].YC;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = t;
                }
            }
            if (minDist < threshold) return closest;
            return -1;
        }

        public static string GetSignedValue(int value)
        {
            if (value > 0) return "+" + value.ToString();
            else return value.ToString();
        }

        public static int GetTurnsToExplore(int region)
        {
            if (region <= 6) return Constants.TURNS_TO_EXPLORE1;
            else if (region <= 18) return Constants.TURNS_TO_EXPLORE2;
            else return Constants.TURNS_TO_EXPLORE3;
        }

        public static Color GetRandomColor()
        {
            Random Rand = new();
            return new Color(
                Rand.Next(256),
                Rand.Next(256),
                Rand.Next(256)
            );
        }

        public static string GetTemperatureRangeData(int temperature, string dataType)
        {
            foreach (var range in GameData.TemperatureRanges)
            {
                if (temperature >= range.MinTemp && temperature <= range.MaxTemp)
                {
                    return dataType switch
                    {
                        "Name" => range.Name,
                        "Modifier" => range.PopulationModifier.ToString(),
                        _ => "Unknown",
                    };
                }
            }
            return "Unknown";
        }
        public static string GetPopModifier(Planet planet, int DeltaFood)
        {
            return GetSignedValue((int)(planet.Population * Constants.POPULATION_BASE_GROWTH * float.Parse(GetTemperatureRangeData(planet.Temperature, "Modifier")) * (1 + DeltaFood * Constants.POPULATION_FOOD_GROWTH)));
        }

        public static int GetPlanetPopulation(Planet planet, string Type)
        {
            int count = 0;
            for (int i = 0; i <= planet.Dimens; i++)
            {
                if (planet.Habitat[i] >= 0 && planet.HabitatPopulated[i])
                {
                    count += GameData.HabitatTypes[planet.Habitat[i]].PopNeeded;
                }
            }
            if (Type == "Assigned")
                return count;
            else if (Type == "Unassigned")
                return planet.Population - count;
            else
                return 0;
        }
    }
}