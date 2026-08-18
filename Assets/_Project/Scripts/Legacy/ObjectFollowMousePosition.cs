using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectFollowMousePosition : MonoBehaviour
{
    private Camera mainCamera;
  

    private void Awake()
    {
        mainCamera = Camera.main;
    }
  

    public void Update()
    {
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0f; // 2D 환경에서 Z 축 문제 방지
        transform.position = worldPosition;

    }
}
