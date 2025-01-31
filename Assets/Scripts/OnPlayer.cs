using UnityEngine;

public class MicrophoneManager : MonoBehaviour
{
    public int activationThreshold = 50;
    public int sampleWindow = 128;
    [Range(1, 100)]
    public int scaledVolume;

    private AudioClip micClip;
    private string micName;
    public GameObject targetObject; // 활성화할 오브젝트

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            micName = Microphone.devices[0];
            micClip = Microphone.Start(micName, true, 10, AudioSettings.outputSampleRate);
            Debug.Log("마이크 시작됨: " + micName);
        }
        else
        {
            Debug.LogWarning("마이크 장치가 없습니다.");
        }
    }

    void Update()
    {
        if (Microphone.IsRecording(micName))
        {
            float volume = GetMaxVolume();
            scaledVolume = ScaleVolume(volume);
            Debug.Log("현재 볼륨: " + volume + " (스케일: " + scaledVolume + ")");

            if (scaledVolume >= activationThreshold && !targetObject.activeSelf)
            {
                ActivateObjectAtMousePosition();
            }
        }
    }

    void ActivateObjectAtMousePosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 10f));
        worldPosition.z = 0f;

        targetObject.transform.position = worldPosition;
        targetObject.SetActive(true);
        Debug.Log("오브젝트 활성화됨! 위치: " + worldPosition);
    }

    float GetMaxVolume()
    {
        if (micClip == null) return 0;

        float[] samples = new float[sampleWindow];
        int micPosition = Microphone.GetPosition(micName);

        if (micPosition < samples.Length)
        {
            Debug.Log("마이크 데이터가 충분히 쌓이지 않음");
            return 0;
        }

        micClip.GetData(samples, micPosition - samples.Length);

        float maxVolume = 0f;
        foreach (float sample in samples)
        {
            maxVolume = Mathf.Max(maxVolume, Mathf.Abs(sample));
        }

        return maxVolume;
    }

    int ScaleVolume(float volume)
    {
        float scaledVolume = Mathf.Log10(1 + volume * 9) * 100;
        return Mathf.RoundToInt(Mathf.Clamp(scaledVolume, 1, 100));
    }
}
