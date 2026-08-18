using UnityEngine;

namespace HomeProtector.Core
{
    public enum GamePhase
    {
        Preparation,
        Combat,
        Result
    }

    public enum TimeOfDay
    {
        Morning,
        Evening
    }

    public enum TowerChangeType
    {
        Placed,
        Removed,
        Upgraded,
        Moved
    }

    public struct PhaseChangedEvent
    {
        public PhaseChangedEvent(GamePhase previousPhase, GamePhase newPhase, int day)
        {
            PreviousPhase = previousPhase;
            NewPhase = newPhase;
            Day = day;
        }

        public GamePhase PreviousPhase { get; }
        public GamePhase NewPhase { get; }
        public int Day { get; }
    }

    public struct DayChangedEvent
    {
        public DayChangedEvent(int previousDay, int newDay)
        {
            PreviousDay = previousDay;
            NewDay = newDay;
        }

        public int PreviousDay { get; }
        public int NewDay { get; }
    }

    public struct WaveChangedEvent
    {
        public WaveChangedEvent(int day, int waveIndex, WaveDefinition wave)
        {
            Day = day;
            WaveIndex = waveIndex;
            Wave = wave;
        }

        public int Day { get; }
        public int WaveIndex { get; }
        public WaveDefinition Wave { get; }
    }

    public struct ResourceChangedEvent
    {
        public ResourceChangedEvent(GameObject resource, string resourceId, Vector3 position)
        {
            Resource = resource;
            ResourceId = resourceId;
            Position = position;
        }

        public GameObject Resource { get; }
        public string ResourceId { get; }
        public Vector3 Position { get; }
    }

    public struct TowerChangedEvent
    {
        public TowerChangedEvent(TowerChangeType changeType, TowerDefinition definition, GameObject instance, Vector3Int cell)
        {
            ChangeType = changeType;
            Definition = definition;
            Instance = instance;
            Cell = cell;
        }

        public TowerChangeType ChangeType { get; }
        public TowerDefinition Definition { get; }
        public GameObject Instance { get; }
        public Vector3Int Cell { get; }
    }
}
