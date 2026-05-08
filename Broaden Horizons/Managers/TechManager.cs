using System;
using System.Collections.Generic;
using System.Linq;

namespace BroadenHorizons
{
    public class TechManager
    {
        private List<Tech> _techs;
        private int _globalScience;
        private int _currentResearch = -1;
        private List<HabitatBonus> _globalHabitatBonuses;
        private MessageManager _messageManager;
        private List<HabitatType> _habitatTypes;

        public TechManager(
            List<Tech> techs,
            int startingScience,
            MessageManager messageManager,
            List<HabitatType> habitatTypes)
        {
            _techs = techs;
            _globalScience = startingScience;
            _messageManager = messageManager;
            _habitatTypes = habitatTypes;
            _globalHabitatBonuses = new List<HabitatBonus>();
        }

        /// <summary>
        /// Handles a tech click from the tech tree screen.
        /// Validates tech and shows confirmation message.
        /// </summary>
        public void HandleTechClick(int techId)
        {
            Tech t = _techs[techId];
            if (!t.CanResearchTech(_techs, _globalScience)) return;

            if (_currentResearch == -1)
            {
                _messageManager.Show($"Start researching {t.Name}?", MessageType.Confirm, yes =>
                {
                    if (yes)
                    {
                        t.IsInProgress = true;
                        _currentResearch = techId;
                        _globalScience -= t.MinScience;
                    }
                });
            }
            else
            {
                Tech current = _techs[_currentResearch];
                _messageManager.Show($"Switch research from {current.Name} to {t.Name}? Progress on {current.Name} will be kept.", MessageType.Confirm, yes =>
                {
                    if (yes)
                    {
                        current.IsInProgress = false;
                        t.IsInProgress = true;
                        _currentResearch = techId;
                    }
                });
            }
        }

        /// <summary>
        /// Processes tech research during end turn.
        /// Returns messages about tech completion and unlocks.
        /// </summary>
        public void ProcessTurnResearch(int scienceProduced, List<string> summaryMessages, out bool techResearchedThisTurn)
        {
            techResearchedThisTurn = false;
            _globalScience += scienceProduced;

            // Apply science to active research
            if (_currentResearch >= 0)
            {
                Tech t = _techs[_currentResearch];
                t.ResearchProgress += scienceProduced;

                if (t.ResearchProgress >= t.Cost)
                {
                    t.IsResearched = true;
                    t.IsInProgress = false;
                    techResearchedThisTurn = true;
                    _globalHabitatBonuses.AddRange(t.BonusUnlocks);

                    string msg = $"Technology researched: {t.Name}\nYou have unlocked: {Tech.GetItemsUnlockedByTech(t.ID)}{Tech.GetBonusesUnlockedByTech(t.ID)}";
                    summaryMessages.Add(msg);
                    _currentResearch = -1;

                    // Apply habitat bonuses
                    foreach (var bonus in t.BonusUnlocks)
                    {
                        if (bonus.FoodProd > 0) 
                            _habitatTypes[_habitatTypes.FindIndex(h => h.Name == bonus.Habitat)].FoodProd += bonus.FoodProd;
                        if (bonus.MatProd > 0) 
                            _habitatTypes[_habitatTypes.FindIndex(h => h.Name == bonus.Habitat)].MatProd += bonus.MatProd;
                        if (bonus.SciProd > 0) 
                            _habitatTypes[_habitatTypes.FindIndex(h => h.Name == bonus.Habitat)].SciProd += bonus.SciProd;
                        if (bonus.EnergyProd > 0) 
                            _habitatTypes[_habitatTypes.FindIndex(h => h.Name == bonus.Habitat)].EnergyProd += bonus.EnergyProd;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if there are available techs to research.
        /// </summary>
        public bool HasAvailableTechs()
        {
            return Tech.HasTechTreeActions(_techs, _globalScience);
        }

        // Accessors and mutators
        public int GlobalScience
        {
            get => _globalScience;
            set => _globalScience = value;
        }

        public int CurrentResearch
        {
            get => _currentResearch;
            set => _currentResearch = value;
        }

        public List<HabitatBonus> GlobalHabitatBonuses
        {
            get => _globalHabitatBonuses;
            set => _globalHabitatBonuses = value ?? new List<HabitatBonus>();
        }

        public List<Tech> Techs
        {
            get => _techs;
        }
    }
}
