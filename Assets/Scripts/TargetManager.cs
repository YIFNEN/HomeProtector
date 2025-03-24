using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 타겟 관리 시스템 - 적이 타겟을 찾는 중앙 관리 시스템
/// </summary>
public class TargetManager : MonoBehaviour
{
    private static TargetManager _instance;
    public static TargetManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("TargetManager");
                _instance = go.AddComponent<TargetManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 태그별 타겟 캐시
    private Dictionary<string, List<Transform>> taggedTargets = new Dictionary<string, List<Transform>>();

    // 타겟 추가/제거 이벤트
    public delegate void TargetEvent(string tag, Transform target);
    public event TargetEvent OnTargetAdded;
    public event TargetEvent OnTargetRemoved;

    [SerializeField]
    private float targetRefreshInterval = 5f; // 주기적 갱신 간격
    [SerializeField]
    private string defaultTargetTag = "Goods"; // 기본 타겟 태그

    [SerializeField, Tooltip("디버그 로그 활성화")]
    private bool debugMode = false;

    private void Awake()
    {
        // 싱글톤 설정
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 초기화
        InitializeTargets();
    }

    private void Start()
    {
        // 주기적으로 타겟 갱신
        StartCoroutine(PeriodicTargetRefresh());
    }

    private void OnEnable()
    {
        // 씬 로드 시 타겟 갱신
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        InitializeTargets();
    }

    /// <summary>
    /// 모든 타겟 초기화
    /// </summary>
    public void InitializeTargets()
    {
        taggedTargets.Clear();

        // 씬의 모든 TargetObject 찾기
        TargetObject[] targetObjects = FindObjectsOfType<TargetObject>();

        foreach (TargetObject targetObj in targetObjects)
        {
            RegisterTarget(targetObj.TargetTag, targetObj.transform);
        }

        // 모든 태그 리프레시
        string[] allTags = UnityEngine.Object.FindObjectsOfType<GameObject>()
            .Select(go => go.tag)
            .Distinct()
            .Where(tag => tag != "Untagged")
            .ToArray();

        foreach (string tag in allTags)
        {
            RefreshTaggedTargets(tag);
        }

        if (debugMode)
        {
            Debug.Log($"타겟 초기화 완료: {taggedTargets.Count}개 태그, 총 {taggedTargets.Values.Sum(list => list.Count)}개 타겟");
        }
    }

    /// <summary>
    /// 주기적 타겟 갱신 코루틴
    /// </summary>
    private IEnumerator PeriodicTargetRefresh()
    {
        while (true)
        {
            // 모든 캐시된 태그에 대해 갱신
            string[] tags = taggedTargets.Keys.ToArray();
            foreach (string tag in tags)
            {
                RefreshTaggedTargets(tag);
            }

            yield return new WaitForSeconds(targetRefreshInterval);
        }
    }

    /// <summary>
    /// 특정 태그의 타겟 갱신
    /// </summary>
    private void RefreshTaggedTargets(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        // 이전 상태 저장
        List<Transform> previousTargets = new List<Transform>();
        if (taggedTargets.ContainsKey(tag))
        {
            previousTargets.AddRange(taggedTargets[tag]);
        }

        // 새로 찾기
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
        List<Transform> newTargets = new List<Transform>();

        foreach (GameObject obj in taggedObjects)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                newTargets.Add(obj.transform);
            }
        }

        // 딕셔너리 업데이트
        taggedTargets[tag] = newTargets;

        // 변경사항 처리 - 추가된 타겟
        foreach (Transform newTarget in newTargets)
        {
            if (!previousTargets.Contains(newTarget))
            {
                OnTargetAdded?.Invoke(tag, newTarget);

                if (debugMode)
                {
                    Debug.Log($"타겟 추가됨: {newTarget.name} (태그: {tag})");
                }
            }
        }

        // 변경사항 처리 - 제거된 타겟
        foreach (Transform oldTarget in previousTargets)
        {
            if (oldTarget != null && !newTargets.Contains(oldTarget))
            {
                OnTargetRemoved?.Invoke(tag, oldTarget);

                if (debugMode)
                {
                    Debug.Log($"타겟 제거됨: {oldTarget.name} (태그: {tag})");
                }
            }
        }
    }

    /// <summary>
    /// 가장 가까운 타겟 찾기
    /// </summary>
    public Transform FindNearestTarget(string tag, Vector3 position, float maxDistance = float.MaxValue)
    {
        // 태그가 비어있으면 기본 태그 사용
        string searchTag = string.IsNullOrEmpty(tag) ? defaultTargetTag : tag;

        // 해당 태그 캐시가 없으면 갱신
        if (!taggedTargets.ContainsKey(searchTag))
        {
            RefreshTaggedTargets(searchTag);
        }

        // 태그가 없거나 타겟이 없으면 null 반환
        if (!taggedTargets.ContainsKey(searchTag) || taggedTargets[searchTag].Count == 0)
        {
            // 기본 태그로 다시 시도
            if (searchTag != defaultTargetTag)
            {
                return FindNearestTarget(defaultTargetTag, position, maxDistance);
            }
            return null;
        }

        // 가장 가까운 타겟 찾기
        Transform closest = null;
        float minDistance = maxDistance;

        foreach (Transform target in taggedTargets[searchTag])
        {
            if (target == null || !target.gameObject.activeInHierarchy) continue;

            float distance = Vector3.Distance(position, target.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = target;
            }
        }

        return closest;
    }

    /// <summary>
    /// 우선순위에 따른 타겟 찾기
    /// </summary>
    /// <summary>
    /// 우선순위에 따른 타겟 찾기
    /// </summary>
    public Transform FindTargetByPriority(string[] priorityTags, Vector3 position, float maxDistance = float.MaxValue)
    {
        // 우선순위 태그 배열 검사
        if (priorityTags == null || priorityTags.Length == 0)
        {
            return FindNearestTarget(defaultTargetTag, position, maxDistance);
        }

        // 우선 우선순위 태그 검색
        foreach (string tag in priorityTags)
        {
            Transform target = FindNearestTarget(tag, position, maxDistance);
            if (target != null)
            {
                if (debugMode)
                {
                    Debug.Log($"우선순위 타겟 찾음: {target.name} (태그: {tag})");
                }
                return target;
            }
        }

        // 모든 태그에서 찾지 못했으면 기본 태그로 시도
        return FindNearestTarget(defaultTargetTag, position, maxDistance);
    }
    /// <summary>
    /// 직접 타겟 등록
    /// </summary>
    public void RegisterTarget(string tag, Transform target)
    {
        if (string.IsNullOrEmpty(tag) || target == null) return;

        if (!taggedTargets.ContainsKey(tag))
        {
            taggedTargets[tag] = new List<Transform>();
        }

        if (!taggedTargets[tag].Contains(target))
        {
            taggedTargets[tag].Add(target);
            OnTargetAdded?.Invoke(tag, target);

            if (debugMode)
            {
                Debug.Log($"타겟 수동 등록: {target.name} (태그: {tag})");
            }
        }
    }

    /// <summary>
    /// 직접 타겟 제거
    /// </summary>
    public void UnregisterTarget(string tag, Transform target)
    {
        if (string.IsNullOrEmpty(tag) || target == null) return;

        if (taggedTargets.ContainsKey(tag) && taggedTargets[tag].Contains(target))
        {
            taggedTargets[tag].Remove(target);
            OnTargetRemoved?.Invoke(tag, target);

            if (debugMode)
            {
                Debug.Log($"타겟 수동 제거: {target.name} (태그: {tag})");
            }
        }
    }

    // 유효한 타겟인지 검사
    public bool IsTargetValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        // ResourceObject 체크 (HP가 0이면 무효)
        ResourceObject resource = target.GetComponent<ResourceObject>();
        if (resource != null && resource.CurrentHP <= 0)
            return false;

        return true;
    }

    // 태그별 타겟 개수 확인 메서드
    public int GetTargetCountForTag(string tag)
    {
        if (string.IsNullOrEmpty(tag) || !taggedTargets.ContainsKey(tag))
        {
            return 0;
        }

        // 유효한 타겟만 카운트
        int validCount = 0;
        foreach (Transform target in taggedTargets[tag])
        {
            if (IsTargetValid(target))
            {
                validCount++;
            }
        }

        return validCount;
    }

    private void OnApplicationQuit()
    {
        _instance = null;
    }
}