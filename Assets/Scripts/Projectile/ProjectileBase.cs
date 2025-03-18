using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
    [SerializeField] protected GameObject hitEffect;
    protected Transform target;
    protected float damage;

    public virtual void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        this.target = target;
        this.damage = damage;
    }

    public abstract void Process();

    protected virtual void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Process();
    }
}