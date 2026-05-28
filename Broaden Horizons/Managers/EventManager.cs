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

            var beforeSnapshot = CreateResourceSnapshot(selected.Target);
            selected.Event.Execute(_game, selected.Target);
            string description = selected.Event.GetDescription != null
                ? selected.Event.GetDescription(_game, selected.Target)
                : selected.Event.Name;

            string changeText = GetResourceChangeText(beforeSnapshot, selected.Target);
            if (!string.IsNullOrEmpty(changeText))
            {
                summary.Add($"Event: {description}\n{changeText}");
            }
            else
            {
                summary.Add($"Event: {description}");
            }
        }

        private static ResourceSnapshot? CreateResourceSnapshot(object target)
        {
            if (target is Planet planet)
            {
                return new ResourceSnapshot
                {
                    Name = planet.Name,
                    Population = planet.Population,
                    Food = planet.Food,
                    Mat = planet.Mat,
                    Energy = planet.Energy
                };
            }

            return null;
        }

        private static string GetResourceChangeText(ResourceSnapshot? beforeSnapshot, object target)
        {
            if (beforeSnapshot is not ResourceSnapshot before)
                return null;
            if (target is not Planet after)
                return null;

            var changes = new List<string>();
            AppendChange(changes, before.Population, after.Population, "population");
            AppendChange(changes, before.Food, after.Food, "food");
            AppendChange(changes, before.Mat, after.Mat, "materials");
            AppendChange(changes, before.Energy, after.Energy, "energy");

            if (changes.Count == 0)
                return null;

            return $"{after.Name} {string.Join(", ", changes)}.";
        }

        private static void AppendChange(List<string> changes, int before, int after, string resourceName)
        {
            int delta = after - before;
            if (delta == 0)
                return;

            string verb = delta > 0 ? "increased" : "decreased";
            changes.Add($"{verb} its {resourceName} by {Math.Abs(delta)}");
        }

        private readonly struct ResourceSnapshot
        {
            public string Name { get; init; }
            public int Population { get; init; }
            public int Food { get; init; }
            public int Mat { get; init; }
            public int Energy { get; init; }
        }
    }
}