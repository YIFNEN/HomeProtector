using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    [Header("BGM Clips")]
    [SerializeField] private AudioClip morningBGM; // 아침 배경음악
    [SerializeField] private AudioClip eveningBGM; // 저녁 배경음악

    [Header("Settings")]
    [SerializeField] private float fadeTime = 2.0f; // 페이드 인/아웃 시간
    [SerializeField] private float volumeMorning = 0.7f; // 아침 음악 기본 볼륨
    [SerializeField] private float volumeEvening = 0.7f; // 저녁 음악 기본 볼륨
    [SerializeField] private bool playOnAwake = true; // 시작 시 자동 재생 여부
    [SerializeField] private bool dontDestroyOnLoad = true; // 씬 전환 시 유지 여부
    [SerializeField] private bool debugMode = false; // 디버그 로그 출력 여부

    private AudioSource audioSource; // 오디오 소스
    private bool isFading = false; // 페이드 중인지 여부
    private TimeOfDay currentTimeOfDay; // 현재 시간
    private TimeSystem timeSystem; // 시간 시스템 참조

    private void Awake()
    {
        // 싱글톤 패턴 구현 (필요시)
        SetupSingleton();

        // 오디오 소스 가져오기 또는 생성
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 오디오 소스 설정
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        // 시간 시스템 찾기
        timeSystem = FindObjectOfType<TimeSystem>();
        if (timeSystem != null)
        {
            // 현재 시간 가져오기
            currentTimeOfDay = timeSystem.CurrentTime;

            // 이벤트 구독
            timeSystem.onMorningStart.AddListener(OnMorningStart);
            timeSystem.onEveningStart.AddListener(OnEveningStart);

            if (debugMode)
            {
                Debug.Log($"BGMManager: TimeSystem 연결 성공, 현재 시간: {currentTimeOfDay}");
            }
        }
        else
        {
            Debug.LogWarning("BGMManager: TimeSystem을 찾을 수 없습니다. 수동으로 음악을 관리하세요.");
        }

        // 자동 재생 설정이면 현재 시간에 맞는 BGM 재생
        if (playOnAwake)
        {
            PlayBGMForCurrentTime();
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (timeSystem != null)
        {
            timeSystem.onMorningStart.RemoveListener(OnMorningStart);
            timeSystem.onEveningStart.RemoveListener(OnEveningStart);
        }
    }

    // 싱글톤 설정 (하나의 BGMManager만 존재하도록)
    private void SetupSingleton()
    {
        if (dontDestroyOnLoad)
        {
            // 이미 존재하는 BGMManager 확인
            BGMManager[] managers = FindObjectsOfType<BGMManager>();
            if (managers.Length > 1)
            {
                // 이미 다른 인스턴스가 있으면 현재 인스턴스 파괴
                Destroy(gameObject);
                return;
            }

            // 씬 전환 시 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);

            if (debugMode)
            {
                Debug.Log("BGMManager: 싱글톤 인스턴스로 설정됨");
            }
        }
    }

    // 아침 시작 이벤트 핸들러
    private void OnMorningStart()
    {
        if (debugMode)
        {
            Debug.Log("BGMManager: 아침 시작 이벤트 감지");
        }

        currentTimeOfDay = TimeOfDay.Morning;
        ChangeBGM(morningBGM, volumeMorning);
    }

    // 저녁 시작 이벤트 핸들러
    private void OnEveningStart()
    {
        if (debugMode)
        {
            Debug.Log("BGMManager: 저녁 시작 이벤트 감지");
        }

        currentTimeOfDay = TimeOfDay.Evening;
        ChangeBGM(eveningBGM, volumeEvening);
    }

    // 현재 시간에 맞는 BGM 재생
    private void PlayBGMForCurrentTime()
    {
        // 시간 시스템이 없으면 기본값으로 아침 BGM 재생
        if (timeSystem == null)
        {
            PlayMorningBGM();
            return;
        }

        // 현재 시간에 따라 BGM 설정
        if (timeSystem.CurrentTime == TimeOfDay.Morning)
        {
            PlayMorningBGM();
        }
        else
        {
            PlayEveningBGM();
        }
    }

    // 아침 BGM 재생
    public void PlayMorningBGM()
    {
        if (debugMode)
        {
            Debug.Log("BGMManager: 아침 BGM 재생 요청");
        }

        ChangeBGM(morningBGM, volumeMorning);
    }

    // 저녁 BGM 재생
    public void PlayEveningBGM()
    {
        if (debugMode)
        {
            Debug.Log("BGMManager: 저녁 BGM 재생 요청");
        }

        ChangeBGM(eveningBGM, volumeEvening);
    }

    // BGM 변경 (페이드 인/아웃 효과 포함)
    private void ChangeBGM(AudioClip newClip, float targetVolume)
    {
        // 새 클립이 없거나 이미 같은 클립이 재생 중이면 무시
        if (newClip == null || (audioSource.clip == newClip && audioSource.isPlaying))
        {
            return;
        }

        // 이미 페이드 중이면 이전 코루틴 중지
        if (isFading)
        {
            StopAllCoroutines();
            isFading = false;
        }

        // 새 BGM으로 페이드 전환
        StartCoroutine(FadeBGM(newClip, targetVolume));
    }

    // BGM 페이드 인/아웃 코루틴
    private IEnumerator FadeBGM(AudioClip newClip, float targetVolume)
    {
        isFading = true;

        // 현재 재생 중인 BGM이 있으면 페이드 아웃
        if (audioSource.isPlaying)
        {
            float startVolume = audioSource.volume;

            // 페이드 아웃
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                yield return null;
            }

            // 완전히 볼륨을 0으로 설정
            audioSource.volume = 0;
            audioSource.Stop();
        }

        // 새 BGM 설정 및 재생
        audioSource.clip = newClip;
        audioSource.Play();

        // 페이드 인
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0, targetVolume, t / fadeTime);
            yield return null;
        }

        // 완전히 목표 볼륨으로 설정
        audioSource.volume = targetVolume;

        isFading = false;
    }

    // 오디오 볼륨 설정
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    // 음악 일시정지
    public void PauseBGM()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    // 음악 재개
    public void ResumeBGM()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }

    // 음악 중지
    public void StopBGM()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}