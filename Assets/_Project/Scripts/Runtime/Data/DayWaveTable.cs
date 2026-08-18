using System.Collections.Generic;
using UnityEngine;

namespace HomeProtector.Core
{
    [System.Serializable]
    public sealed class DayWaveEntry
    {
        [SerializeField] private string label = "Day";
        [SerializeField] private int day = 1;
        [SerializeField] private int dayRangeStart = 0;
        [SerializeField] private int dayRangeEnd = 0;
        [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

        public string Label => label;
        public int Day => day;
        public int DayRangeStart => dayRangeStart;
        public int DayRangeEnd => dayRangeEnd;
        public IReadOnlyList<WaveDefinition> Waves => waves;

        public bool Matches(int currentDay)
        {
            if (day > 0 && currentDay == day)
            {
                return true;
            }

            return dayRangeStart > 0 && dayRangeEnd >= dayRangeStart && currentDay >= dayRangeStart && currentDay <= dayRangeEnd;
        }
    }

    [CreateAssetMenu(fileName = "DayWaveTable", menuName = "Home Protector/Day Wave Table")]
    public sealed class DayWaveTable : ScriptableObject
    {
        [SerializeField] private List<DayWaveEntry> entries = new List<DayWaveEntry>();

        public IReadOnlyList<DayWaveEntry> Entries => entries;

        public IReadOnlyList<WaveDefinition> GetWavesForDay(int day)
        {
            DayWaveEntry exactMatch = null;
            DayWaveEntry rangeMatch = null;

            foreach (DayWaveEntry entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.Day == day)
                {
                    exactMatch = entry;
                    break;
                }

                if (rangeMatch == null && entry.Matches(day))
                {
                    rangeMatch = entry;
                }
            }

            DayWaveEntry selected = exactMatch ?? rangeMatch;
            return selected != null ? selected.Waves : System.Array.Empty<WaveDefinition>();
        }

        public bool IsValid(out string message)
        {
            if (entries == null || entries.Count == 0)
            {
                message = "Day wave table has no entries.";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
