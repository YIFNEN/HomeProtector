using System.Collections.Generic;
using UnityEngine;

namespace HomeProtector.Core
{
    [System.Serializable]
    public sealed class EnemyWaveEntry
    {
        [SerializeField] private EnemyDefinition enemy;
        [SerializeField] private int count = 1;
        [SerializeField] private float spawnInterval = 1f;
        [SerializeField] private Transform spawnPointOverride;

        public EnemyDefinition Enemy => enemy;
        public int Count => Mathf.Max(0, count);
        public float SpawnInterval => Mathf.Max(0f, spawnInterval);
        public Transform SpawnPointOverride => spawnPointOverride;
    }

    [CreateAssetMenu(fileName = "WaveDefinition", menuName = "Home Protector/Wave Definition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [SerializeField] private string id = "wave";
        [SerializeField] private string displayName = "Wave";
        [SerializeField] private float duration = 30f;
        [SerializeField] private int rewardGold = 0;
        [SerializeField] private List<EnemyWaveEntry> enemyGroups = new List<EnemyWaveEntry>();

        public string Id => id;
        public string DisplayName => displayName;
        public float Duration => Mathf.Max(0f, duration);
        public int RewardGold => rewardGold;
        public IReadOnlyList<EnemyWaveEntry> EnemyGroups => enemyGroups;

        public bool IsValid(out string message)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                message = "Wave id is empty.";
                return false;
            }

            if (enemyGroups == null || enemyGroups.Count == 0)
            {
                message = $"Wave '{id}' has no enemy groups.";
                return false;
            }

            for (int i = 0; i < enemyGroups.Count; i++)
            {
                if (enemyGroups[i].Enemy == null)
                {
                    message = $"Wave '{id}' has an empty enemy slot at index {i}.";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }
    }
}
