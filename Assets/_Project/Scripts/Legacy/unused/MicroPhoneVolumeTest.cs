using UnityEngine;

public class MicroPhoneVolumeTest : MonoBehaviour
{
    private AudioClip micClip;
    private string micName;

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
            Debug.LogWarning("마이크 없음!");
        }
    }

    void Update()
    {
        if (Microphone.IsRecording(micName))
        {
            float volume = GetMaxVolume();
            Debug.Log("현재 볼륨: " + volume);
        }

        int micPosition = Microphone.GetPosition(micName);
        Debug.Log("마이크 현재 위치: " + micPosition);

        if (micPosition > 0)
        {
            float volume = GetMaxVolume();
            Debug.Log("현재 볼륨: " + volume);
        }
    }

    float GetMaxVolume()
    {       if (micClip == null) return 0;

            float[] samples = new float[128];
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

    }

