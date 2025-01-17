
using UnityEngine;

public class PlayerGold : MonoBehaviour
{
    [SerializeField]
    private int currentGold = 300;

    public int CurrentGold
    { 
        set => currentGold = Mathf.Max(0, value);
        get => currentGold;
    }
}
