using UnityEngine;

namespace HomeProtector.Core
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Home Protector/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string id = "enemy";
        [SerializeField] private string displayName = "Enemy";
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite icon;
        [SerializeField] private float maxHealth = 1f;
        [SerializeField] private int goldReward = 1;
        [SerializeField] private int experienceReward = 1;
        [SerializeField] private float moveSpeedMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public Sprite Icon => icon;
        public float MaxHealth => maxHealth;
        public int GoldReward => goldReward;
        public int ExperienceReward => experienceReward;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;

        public bool IsValid(out string message)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                message = "Enemy id is empty.";
                return false;
            }

            if (prefab == null)
            {
                message = $"Enemy '{id}' has no prefab.";
                return false;
            }

            if (maxHealth <= 0f)
            {
                message = $"Enemy '{id}' must have positive health.";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
