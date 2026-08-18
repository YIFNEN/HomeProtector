using System;
using UnityEngine;

namespace HomeProtector.Core
{
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private int startingDay = 1;
        [SerializeField] private GamePhase initialPhase = GamePhase.Preparation;
        [SerializeField] private bool publishInitialStateOnStart = true;

        public event Action<PhaseChangedEvent> PhaseChanged;
        public event Action<DayChangedEvent> DayChanged;

        public int CurrentDay { get; private set; } = 1;
        public GamePhase CurrentPhase { get; private set; }
        public bool LastCombatWon { get; private set; }

        private void Awake()
        {
            CurrentDay = Mathf.Max(1, startingDay);
            CurrentPhase = initialPhase;
        }

        private void Start()
        {
            if (!publishInitialStateOnStart)
            {
                return;
            }

            DayChanged?.Invoke(new DayChangedEvent(CurrentDay, CurrentDay));
            PhaseChanged?.Invoke(new PhaseChangedEvent(CurrentPhase, CurrentPhase, CurrentDay));
        }

        public void BeginPreparation()
        {
            if (CurrentPhase != GamePhase.Result || LastCombatWon)
            {
                return;
            }

            SetPhase(GamePhase.Preparation);
        }

        public void BeginCombat()
        {
            if (CurrentPhase != GamePhase.Preparation)
            {
                return;
            }

            SetPhase(GamePhase.Combat);
        }

        public void CompleteCombat(bool victory)
        {
            if (CurrentPhase != GamePhase.Combat)
            {
                return;
            }

            LastCombatWon = victory;
            SetPhase(GamePhase.Result);
        }

        public void AdvanceDay()
        {
            if (CurrentPhase != GamePhase.Result || !LastCombatWon)
            {
                return;
            }

            int previousDay = CurrentDay;
            CurrentDay++;
            DayChanged?.Invoke(new DayChangedEvent(previousDay, CurrentDay));
            SetPhase(GamePhase.Preparation);
        }

        public void ResetSession(int day = 1)
        {
            int previousDay = CurrentDay;
            CurrentDay = Mathf.Max(1, day);
            LastCombatWon = false;
            DayChanged?.Invoke(new DayChangedEvent(previousDay, CurrentDay));
            SetPhase(GamePhase.Preparation, true);
        }

        public void SetPhase(GamePhase nextPhase)
        {
            SetPhase(nextPhase, false);
        }

        private void SetPhase(GamePhase nextPhase, bool forcePublish)
        {
            GamePhase previousPhase = CurrentPhase;
            if (!forcePublish && previousPhase == nextPhase)
            {
                return;
            }

            CurrentPhase = nextPhase;
            PhaseChanged?.Invoke(new PhaseChangedEvent(previousPhase, nextPhase, CurrentDay));
        }
    }
}
