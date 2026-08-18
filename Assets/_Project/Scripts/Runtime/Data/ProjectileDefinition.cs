using UnityEngine;

namespace HomeProtector.Core
{
    public enum ProjectileBehaviourType
    {
        Straight,
        Homing,
        Area,
        SlowDebuff,
        AttackSpeedDebuff,
        ComboDebuff
    }

    [CreateAssetMenu(fileName = "ProjectileDefinition", menuName = "Home Protector/Projectile Definition")]
    public sealed class ProjectileDefinition : ScriptableObject
    {
        [SerializeField] private string id = "projectile";
        [SerializeField] private string displayName = "Projectile";
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite icon;
        [SerializeField] private ProjectileBehaviourType behaviourType = ProjectileBehaviourType.Straight;
        [SerializeField] private float baseDamage = 1f;
        [SerializeField] private float speed = 8f;
        [SerializeField] private float areaRadius = 0f;
        [SerializeField] private float debuffDuration = 0f;
        [SerializeField] private float debuffMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public Sprite Icon => icon;
        public ProjectileBehaviourType BehaviourType => behaviourType;
        public float BaseDamage => baseDamage;
        public float Speed => speed;
        public float AreaRadius => areaRadius;
        public float DebuffDuration => debuffDuration;
        public float DebuffMultiplier => debuffMultiplier;

        public bool IsValid(out string message)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                message = "Projectile id is empty.";
                return false;
            }

            if (prefab == null)
            {
                message = $"Projectile '{id}' has no prefab.";
                return false;
            }

            if (baseDamage < 0f)
            {
                message = $"Projectile '{id}' has negative damage.";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
