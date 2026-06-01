using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class WhisperSTT : MonoBehaviour
{
    [Header("API 설정")]
    public string apiKey = "YOUR_OPENAI_API_KEY";
    public string language = "ko";

    public event Action<string> OnTranscriptionComplete;
    public bool IsListening { get; private set; }

    private string _micDevice;
    private AudioClip _recording;
    private float _startTime;

    public void StartListening()
    {
        if (IsListening) return;

        _micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (_micDevice == null)
        {
            Debug.LogError("마이크를 찾을 수 없습니다!");
            return;
        }

        IsListening = true;
        _recording = Microphone.Start(_micDevice, false, 10, 44100);
        _startTime = Time.time;
        Debug.Log("🎤 Whisper 녹음 시작...");
    }

    public void StopListening()
    {
        if (!IsListening) return;

        int lastPos = Microphone.GetPosition(_micDevice);
        Microphone.End(_micDevice);
        IsListening = false;

        if (lastPos > 0)
        {
            StartCoroutine(TranscribeCoroutine(_recording, lastPos));
        }
    }

    IEnumerator TranscribeCoroutine(AudioClip clip, int samples)
    {
        Debug.Log("🤖 Whisper 분석 중...");
        byte[] wavData = SaveWav(clip, samples);

        var form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");
        //form.AddField("model", "whisper-1");
        //form.AddField("language", language);

    var req = UnityWebRequest.Post(
        "http://127.0.0.1:5000/transcribe", form);  // ← URL 변경
        //req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Whisper 오류: {req.error}\n{req.downloadHandler.text}");
            OnTranscriptionComplete?.Invoke("");
            yield break;
        }

        var json = JsonUtility.FromJson<WhisperResponse>(req.downloadHandler.text);
        Debug.Log($"📝 인식됨: {json.text}");
        OnTranscriptionComplete?.Invoke(json.text);
    }

    private byte[] SaveWav(AudioClip clip, int samples)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                float[] data = new float[samples * clip.channels];
                clip.GetData(data, 0);

                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + data.Length * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2);
                writer.Write((short)(clip.channels * 2));
                writer.Write((short)16);
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(data.Length * 2);

                foreach (float f in data)
                {
                    writer.Write((short)(f * 32767f));
                }
            }
            return stream.ToArray();
        }
    }

    [Serializable] class WhisperResponse { public string text; }
}
