using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace BroadenHorizons
{
    public class Ship
    {
        public int Id { get; set; } // Unique ID
        public string Name { get; set; }
        public int TypeIndex { get; set; } // Index in GameData.ShipTypes
        public int AssignedPlanet { get; set; }
        public ShipStatus Status { get; set; } = ShipStatus.Docked;
        public Vector2 CurrentPosition { get; set; }
        public int TargetPlanet { get; set; } = -1;
        public int FinalTurnAction { get; set; }
        public List<int> LoadedUnits { get; set; } = new List<int>();
        public int CargoFood { get; set; } = 0;
        public int CargoMat { get; set; } = 0;
    }

    public enum ShipTypeEnum
    {
        Probe = 0,
        ColonyShip = 1,
        Freighter = 2
    }

    public class ShipType
    {
        public string Name { get; set; }
        public ShipTypeEnum Type { get; set; }
        public int MatCost { get; set; }
        public int MaintCost { get; set; }
        public int Speed { get; set; }
        public int EnergyperTurn { get; set; }
        public int TurnsToBuild { get; set; }
        public int RequiredTech { get; set; } = -1;
        public int TextureId { get; set; }
        public int Capacity { get; set; } = 0; // For freighters: max units
    }

    public enum ShipStatus
    {
        Building,
        Docked,
        InTransit,
        PerformingAction
    }

    public class LaunchData
    {
        public int Turn { get; set; }
        public int TurnsNeeded { get; set; }
        public int EnergyNeeded { get; set; }
        public int TargetPlanet { get; set; }
    }
}
