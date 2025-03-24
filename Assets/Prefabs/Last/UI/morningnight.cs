using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps; // Tilemap을 조작하기 위해 추가

public class morningnight : MonoBehaviour
{
    public Camera mainCamera; // 카메라 참조
    public List<Tilemap> tilemaps; // 여러 개의 타일맵을 저장할 리스트

    public Color dayColor = Color.cyan; // 낮 배경색
    public Color nightColor = Color.black; // 밤 배경색
    public Color dayTileColor = Color.white; // 낮 타일맵 색상
    public Color nightTileColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 연한 밤 타일맵 색상 (어둡게)

    [Range(0.5f, 2.0f)]
    public float transitionTime = 1.0f; // 변화 속도 (더 부드럽게)

    private bool isNight = false;
    public Button nightButton; // 밤으로 바꾸는 버튼

    private void Awake()
    {
        mainCamera.backgroundColor = dayColor; // 처음에는 낮 배경색
        foreach (var tilemap in tilemaps)
        {
            tilemap.color = dayTileColor; // 타일맵도 낮 색상으로 설정
        }
        nightButton.onClick.AddListener(ChangeNight);
    }

    public void ChangeNight()
    {
        if (isNight)
        {
            StartCoroutine(SwapColor(nightColor, dayColor, nightTileColor, dayTileColor)); // 밤 → 낮
        }
        else
        {
            StartCoroutine(SwapColor(dayColor, nightColor, dayTileColor, nightTileColor)); // 낮 → 밤
        }
        isNight = !isNight;
    }

    IEnumerator SwapColor(Color startBg, Color endBg, Color startTile, Color endTile)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / transitionTime;

            // 배경색 변경
            mainCamera.backgroundColor = Color.Lerp(startBg, endBg, t);

            // 모든 타일맵의 색상 변경
            foreach (var tilemap in tilemaps)
            {
                tilemap.color = Color.Lerp(startTile, endTile, t);
            }

            yield return null;
        }

        // 최종 색상 적용
        mainCamera.backgroundColor = endBg;
        foreach (var tilemap in tilemaps)
        {
            tilemap.color = endTile;
        }
    }
}
