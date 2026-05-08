// PlanetGenerator.cs
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BroadenHorizons
{
    public static class PlanetGenerator
    {
        /// <summary>
        /// Generates planet positions using Poisson-like sampling to ensure minimum spacing.
        /// </summary>
        public static List<Vector2> GenerateSpacedPoints(int width, int height, int minDist, int numPoints, int maxAttempts, Random rand)
        {
            List<Vector2> points = new List<Vector2>();

            while (points.Count < numPoints)
            {
                bool added = false;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    var candidate = new Vector2(
                        rand.Next(minDist, width - minDist),
                        rand.Next(minDist, height - minDist)
                    );
                    bool valid = true;
                    foreach (var p in points)
                    {
                        if (Vector2.Distance(candidate, p) < minDist)
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (valid)
                    {
                        points.Add(candidate);
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    Debug.WriteLine($"Could not place planet {points.Count} after {maxAttempts} attempts.");
                    break;
                }
            }

            return points;
        }

        /// <summary>
        /// Creates a planet with name, position, size, habitats, and resources.
        /// </summary>
        public static void CreatePlanet(
            Planet planet,
            int planetX,
            int planetY,
            List<string> planetNames,
            List<HabitatType> habitatTypes,
            int[] neighbors,
            Random rand,
            RegionBonusManager regionBonusManager,
            bool isStartingPlanet)
        {
            // Name
            int namePos = rand.Next(0, planetNames.Count);
            planet.Name = planetNames[namePos];
            planetNames.RemoveAt(namePos);

            // Size
            int dimens = isStartingPlanet
                ? rand.Next(Constants.STARTING_PLANET_MIN_SIZE, Constants.MAX_PLANET_DIMENS + 1)
                : rand.Next(Constants.MIN_PLANET_DIMENS, Constants.MAX_PLANET_DIMENS + 1);
            planet.Dimens = dimens;

            //Temperature
            int temperature;
            if (isStartingPlanet)
            {
                // For starting planets, ensure a habitable temperature range (16 to 40)
                temperature = rand.Next(16, 41);
            }
            else
            {
                int roll = rand.Next(101); // 0 to 100
                if (roll < 15) // 15%
                {
                    // -50 to -21 (Frigid)
                    temperature = rand.Next(-50, -21);
                }
                else if (roll < 40) // 25%
                {
                    // -20 to 15 (Cold)
                    temperature = rand.Next(-20, 16);
                }
                else if (roll < 70) // 30%
                {
                    // 16 to 40 (Temperate)
                    temperature = rand.Next(16, 41);
                }
                else if (roll < 90) // 20%
                {
                    // 41 to 65 (Hot)
                    temperature = rand.Next(41, 66);
                }
                else // 10%
                {
                    // 66 to 100 (Scorching)
                    temperature = rand.Next(66, 101);
                }
            }
            planet.Temperature = temperature;

            // Position & visuals
            planet.XPos = planetX;
            planet.YPos = planetY;
            planet.TintColor = Functions.GetRandomColor();

            // Initialize habitat array
            for (int j = 1; j <= Constants.MAX_PLANET_DIMENS; j++)
            {
                planet.Habitat[j] = Constants.NON_EXISTING_HABTITAT;
                planet.HabitatPopulated[j] = false;
            }

            // === Assign first ring (regions 1–6) ===
            for (int j = 1; j <= 6; j++)
            {
                int randomValue = rand.Next(1, 101);
                int prevProb = 0;
                for (int i = 1; i < habitatTypes.Count; i++)
                {
                    int currProb = habitatTypes[i].FirstProb;
                    if (randomValue > prevProb && randomValue <= currProb)
                    {
                        planet.Habitat[j] = -i;
                        break;
                    }
                    prevProb = currProb;
                }
            }

            // === Contiguous region assignment using frontier growth ===
            HashSet<int> occupiedRegs = [.. Enumerable.Range(1, 6)];
            occupiedRegs.Add(0);
            List<int> frontier = new List<int>();

            // Initialize frontier from first ring
            for (int j = 1; j <= 6; j++)
            {
                for (int k = 0; k < 6; k++)
                {
                    int neighbor = neighbors[j * 6 + k];
                    if (neighbor >= 0 && neighbor <= Constants.MAX_PLANET_DIMENS && !occupiedRegs.Contains(neighbor))
                    {
                        frontier.Add(neighbor);
                    }
                }
            }

            int regsToAssign = dimens - 6;
            while (regsToAssign > 0 && frontier.Count > 0)
            {
                int frontierIndex = rand.Next(0, frontier.Count);
                int selectedReg = frontier[frontierIndex];
                frontier.RemoveAt(frontierIndex);
                occupiedRegs.Add(selectedReg);

                // Assign habitat based on ring
                int probType = selectedReg <= 18 ? 1 : 2;
                int randomValue = rand.Next(1, 101);
                int prevProb = 0;
                for (int i = 1; i < habitatTypes.Count; i++)
                {
                    int currProb = probType == 1 ? habitatTypes[i].SecProb : habitatTypes[i].ThirdProb;
                    if (randomValue > prevProb && randomValue <= currProb)
                    {
                        planet.Habitat[selectedReg] = -i;
                        break;
                    }
                    prevProb = currProb;
                }

                // Expand frontier
                for (int k = 0; k < 6; k++)
                {
                    int neighbor = neighbors[selectedReg * 6 + k];
                    if (neighbor >= 0 && neighbor <= Constants.MAX_PLANET_DIMENS && 
                        !occupiedRegs.Contains(neighbor) && !frontier.Contains(neighbor))
                    {
                        frontier.Add(neighbor);
                    }
                }
                regsToAssign--;

                // Starting planet data
                if (isStartingPlanet)
                {
                    planet.Habitat[0] = 0;
                    planet.Food = Constants.STARTING_FOOD;
                    planet.Mat = Constants.STARTING_MATERIALS;
                    planet.Energy = Constants.STARTING_ENERGY;
                    planet.Status = PlanetStatus.Owned;
                    planet.Population = Constants.STARTING_POPULATION;
                    planet.HabitatPopulated[0] = true;
                }
            }

            // Assign region bonuses
            regionBonusManager.AssignRegionBonusesToPlanet(planet, isStartingPlanet);
        }
    }
}