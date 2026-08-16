using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

// 타워 이동 시 피로도 반영 컴포넌트 
public class TowerMovementFatigue : MonoBehaviour
{
    [Header("Fatigue Settings")]
    [SerializeField] private float movementFatigueRatio = 0.2f; // 이동 시 피로도는 배치 피로도의 20%
    [SerializeField] private float minMovementFatigue = 1f; // 최소 이동 피로도
    
    [Header("System References")]
    [SerializeField] private TowerSpawner towerSpawner; // 타워 스포너 참조
    [SerializeField] private PlayerGold playerGold; // 플레이어 골드(피로도) 참조
    [SerializeField] private Grid grid; // 그리드 참조
    [SerializeField] private Tilemap tilemap; // 타일맵 참조
    
    // 현재 선택된 타워 정보
    private GameObject selectedTower = null;
    private Vector3Int originalCellPosition;
    private bool isDraggingTower = false;
    
    private void Awake()
    {
        // 필요한 컴포넌트 참조 찾기
        if (towerSpawner == null) towerSpawner = FindObjectOfType<TowerSpawner>();
        if (playerGold == null) playerGold = FindObjectOfType<PlayerGold>();
        if (grid == null) grid = FindObjectOfType<Grid>();
        if (tilemap == null && towerSpawner != null) tilemap = towerSpawner.GetTilemap();
    }
    
    private void Update()
    {
        // 인터페이스 요소 위에 마우스가 있는 경우 무시
        if (EventSystem.current.IsPointerOverGameObject()) return;
        
        // 타워 선택 (마우스 왼쪽 버튼 클릭)
        if (Input.GetMouseButtonDown(0) && !isDraggingTower)
        {
            SelectTowerAtMousePosition();
        }
        
        // 타워 드래그 (마우스 이동)
        if (isDraggingTower && selectedTower != null)
        {
            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            Vector3Int cellPosition = grid.WorldToCell(mouseWorldPosition);
            
            // 타워 위치 업데이트 (타일 중앙)
            Vector3 cellCenterPosition = tilemap.GetCellCenterWorld(cellPosition);
            cellCenterPosition.z = cellCenterPosition.y; // 이소메트릭 z 조정
            selectedTower.transform.position = cellCenterPosition;
        }
        
        // 타워 배치 확정 (마우스 버튼 놓기)
        if (Input.GetMouseButtonUp(0) && isDraggingTower && selectedTower != null)
        {
            PlaceTowerAtCurrentPosition();
        }
    }
    
    // 마우스 위치의 타워 선택
    private void SelectTowerAtMousePosition()
    {
        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        
        // 레이캐스트로 타워 찾기
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);
        
        if (hit.collider != null && hit.collider.CompareTag("Tower"))
        {
            selectedTower = hit.collider.gameObject;
            originalCellPosition = grid.WorldToCell(selectedTower.transform.position);
            isDraggingTower = true;
            
            Debug.Log($"타워 선택됨: {selectedTower.name} at {originalCellPosition}");
        }
    }
    
    // 현재 위치에 타워 배치 확정
    private void PlaceTowerAtCurrentPosition()
    {
        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        Vector3Int newCellPosition = grid.WorldToCell(mouseWorldPosition);
        
        // 원래 위치와 같으면 이동하지 않음
        if (newCellPosition == originalCellPosition)
        {
            CancelTowerMovement();
            return;
        }
        
        // 새 위치가 유효한지 확인
        if (!IsValidTowerPosition(newCellPosition))
        {
            // 유효하지 않은 위치면 원래 위치로 되돌림
            ReturnTowerToOriginalPosition();
            Debug.Log("유효하지 않은 위치: 타워를 원래 위치로 되돌립니다.");
            return;
        }
        
        // 피로도 충분한지 확인
        float movementFatigue = CalculateMovementFatigue();
        if (playerGold.CurrentFatigue + movementFatigue > playerGold.MaxFatigue)
        {
            // 피로도 부족하면 원래 위치로 되돌림
            ReturnTowerToOriginalPosition();
            Debug.Log("피로도 부족: 타워를 원래 위치로 되돌립니다.");
            return;
        }
        
        // 이동 수행 (Dictionary 업데이트)
        if (towerSpawner != null)
        {
            // 기존 위치에서 제거
            towerSpawner.RemoveTowerWithoutDestroy(originalCellPosition);
            
            // 새 위치에 등록
            towerSpawner.RegisterExistingTower(newCellPosition, selectedTower);
            
            // 피로도 증가
            playerGold.IncreaseFatigueByAmount(movementFatigue);
            
            Debug.Log($"타워 이동 완료: {originalCellPosition} -> {newCellPosition}, 피로도 증가: {movementFatigue}");
        }
        
        // 드래그 완료
        isDraggingTower = false;
        selectedTower = null;
    }
    
    // 타워 이동 취소
    private void CancelTowerMovement()
    {
        isDraggingTower = false;
        selectedTower = null;
    }
    
    // 타워를 원래 위치로 되돌림
    private void ReturnTowerToOriginalPosition()
    {
        if (selectedTower != null && tilemap != null)
        {
            Vector3 originalPosition = tilemap.GetCellCenterWorld(originalCellPosition);
            originalPosition.z = originalPosition.y; // 이소메트릭 z 조정
            selectedTower.transform.position = originalPosition;
        }
        
        isDraggingTower = false;
        selectedTower = null;
    }
    
    // 타워 배치 가능한 위치인지 확인
    private bool IsValidTowerPosition(Vector3Int cellPosition)
    {
        // 타일맵 범위 내에 있는지
        if (!tilemap.HasTile(cellPosition))
        {
            return false;
        }
        
        // 다른 타워가 이미 있는지
        if (towerSpawner != null && towerSpawner.IsTileOccupied(cellPosition))
        {
            return false;
        }
        
        return true;
    }
    
    // 이동 피로도 계산
    private float CalculateMovementFatigue()
    {
        if (towerSpawner == null || playerGold == null) return 0f;
        
        // 배치 피로도의 20%로 설정
        float baseFatigue = towerSpawner.GetBaseFatiguePerTower();
        float movementFatigue = baseFatigue * movementFatigueRatio;
        
        // 최소값 보장
        return Mathf.Max(minMovementFatigue, movementFatigue);
    }
    
    // 마우스 월드 좌표 가져오기
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        mousePosition.z = 0;
        return mousePosition;
    }
}