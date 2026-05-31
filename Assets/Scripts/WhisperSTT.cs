using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
public class WhisperSTT : MonoBehaviour
{
    [Header("API 설정")]
    public string apiKey = "YOUR_OPENAI_API_KEY";
    public string language = "ko";   // 한국어

    public event Action<string> OnTranscriptionComplete;

    public void Transcribe(byte[] wavData)
    {
        StartCoroutine(TranscribeCoroutine(wavData));
    }

    IEnumerator TranscribeCoroutine(byte[] wavData)
    {
        var form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");
        form.AddField("model", "whisper-1");
        form.AddField("language", language);

        var req = UnityWebRequest.Post(
            "https://api.openai.com/v1/audio/transcriptions", form);
        req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Whisper 오류: {req.error}");
            yield break;
        }

        var json = JsonUtility.FromJson<WhisperResponse>(req.downloadHandler.text);
        Debug.Log($"📝 인식됨: {json.text}");
        OnTranscriptionComplete?.Invoke(json.text);
    }

    [Serializable] class WhisperResponse { public string text; }
}
