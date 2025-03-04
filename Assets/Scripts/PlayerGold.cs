
using UnityEngine;

public class PlayerGold : MonoBehaviour//플레이어 관련 지표 관리
{
    [SerializeField]
    private int currentGold = 300;
    public int playerLevel = 1;// 레벨에 따라 강해짐
    public float playerFatigue = 0; //피로도

    public int CurrentGold
    { 
        set => currentGold = Mathf.Max(0, value);
        get => currentGold;
    }
}
