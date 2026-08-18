using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    public static List<Vector3> FindPath(Vector3 start, Vector3 target)
    {
        List<Vector3> path = new List<Vector3>();

        // 간단한 A* 알고리즘
        Vector3[] directions = { Vector3.right, Vector3.left, Vector3.up, Vector3.down }; // 2D 평면 이동

        // 큐와 방문 목록
        Queue<Vector3> frontier = new Queue<Vector3>();
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        frontier.Enqueue(start);
        cameFrom[start] = start;

        while (frontier.Count > 0)
        {
            Vector3 current = frontier.Dequeue();

            if (current == target)
            {
                // 경로를 찾으면 추적하여 반환
                Vector3 temp = target;
                while (!temp.Equals(start))
                {
                    path.Add(temp);
                    temp = cameFrom[temp];
                }
                path.Add(start);
                path.Reverse(); // 경로 순서를 반대로
                return path;
            }

            foreach (Vector3 direction in directions)
            {
                Vector3 next = current + direction;

                // 타일이 벽이나 유효하지 않으면 넘어가기
                if (IsValid(next) && !cameFrom.ContainsKey(next))
                {
                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }

        return path; // 경로를 찾지 못했을 경우 빈 리스트 반환
    }

    // 벽 체크 로직 (간단히, 게임에 맞춰서 변경 필요)
    private static bool IsValid(Vector3 position)
    {
        // 벽을 피하거나, 범위 밖으로 나가지 않도록 처리
        return position.x >= 0 && position.y >= 0; // 예시 조건
    }
}
