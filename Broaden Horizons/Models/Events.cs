using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace BroadenHorizons
{
    public class GameEvent
    {
        public string Name { get; set; }
        public Func<BH, object, string> GetDescription { get; set; }
        public Action<BH, object> Execute { get; set; }
        public Func<BH, List<object>> GetValidTargets { get; set; }
        public int Weight { get; set; } = 1;
    }

    public class EventInstance
    {
        public GameEvent Event { get; set; }
        public object Target { get; set; }
    }
}