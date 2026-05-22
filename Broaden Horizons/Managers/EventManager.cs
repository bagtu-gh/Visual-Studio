using System;
using System.Collections.Generic;
using System.Linq;

namespace BroadenHorizons
{
    public class EventManager
    {
        private readonly BH _game;

        public EventManager(BH game)
        {
            _game = game;
        }

        public void TryTriggerEvent(List<string> summary)
        {
            // Check if event triggers at all
            if (_game.Rand.NextDouble() > Constants.PROB_EVENT)
              return;

            var validInstances = new List<EventInstance>();

            foreach (var ev in GameData.GameEvents)
            {
                if (ev.GetValidTargets == null)
                    continue;
                var targets = ev.GetValidTargets(_game);
                if (targets == null || targets.Count == 0)
                    continue;
                foreach (var target in targets)
                {
                    validInstances.Add(new EventInstance
                    {
                        Event = ev,
                        Target = target
                    });
                }
            }

            if (validInstances.Count == 0)
                return;

            // Weighted selection
            int totalWeight = validInstances.Sum(i => i.Event.Weight);
            int roll = _game.Rand.Next(1, totalWeight + 1);
            int cumulative = 0;
            EventInstance selected = null;
            foreach (var instance in validInstances)
            {
                cumulative += instance.Event.Weight;
                if (roll <= cumulative)
                {
                    selected = instance;
                    break;
                }
            }

            if (selected == null)
                return;

            selected.Event.Execute(_game, selected.Target);
            string description = selected.Event.GetDescription != null
                ? selected.Event.GetDescription(_game, selected.Target)
                : selected.Event.Name;

            summary.Add($"Event: {description}"
            );
        }
    }
}