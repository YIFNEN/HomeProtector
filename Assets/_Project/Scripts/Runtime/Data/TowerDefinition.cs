using System.Collections.Generic;
using UnityEngine;

namespace HomeProtector.Core
{
    [System.Serializable]
    public sealed class TowerLevelDefinition
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private ProjectileDefinition projectile;
        [SerializeField] private float damage = 1f;
        [SerializeField] private float fireRate = 1f;
        [SerializeField] private float range = 4f;
        [SerializeField] private int cost = 1;
        [SerializeField] private int sellValue = 1;

        public Sprite Sprite => sprite;
        public ProjectileDefinition Projectile => projectile;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float Range => range;
        public int Cost => cost;
        public int SellValue => sellValue;
    }

    [CreateAssetMenu(fileName = "TowerDefinition", menuName = "Home Protector/Tower Definition")]
    public sealed class TowerDefinition : ScriptableObject
    {
        [SerializeField] private string id = "tower";
        [SerializeField] private string displayName = "Tower";
        [SerializeField] private GameObject towerPrefab;
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private List<TowerLevelDefinition> levels = new List<TowerLevelDefinition>();

        public string Id => id;
        public string DisplayName => displayName;
        public GameObject TowerPrefab => towerPrefab;
        public GameObject PreviewPrefab => previewPrefab != null ? previewPrefab : towerPrefab;
        public IReadOnlyList<TowerLevelDefinition> Levels => levels;

        public TowerLevelDefinition GetLevel(int zeroBasedLevel)
        {
            if (levels == null || levels.Count == 0)
            {
                return null;
            }

            return levels[Mathf.Clamp(zeroBasedLevel, 0, levels.Count - 1)];
        }

        public bool IsValid(out string message)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                message = "Tower id is empty.";
                return false;
            }

            if (towerPrefab == null)
            {
                message = $"Tower '{id}' has no prefab.";
                return false;
            }

            if (levels == null || levels.Count == 0)
            {
                message = $"Tower '{id}' has no level data.";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
