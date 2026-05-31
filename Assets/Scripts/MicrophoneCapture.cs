using System;
using System.IO;
using UnityEngine;

public class MicrophoneCapture : MonoBehaviour
{
    [Header("녹음 설정")]
    public int sampleRate = 16000;
    public float silenceThreshold = 0.02f;
    public float silenceDuration = 1.2f;
    public float maxRecordDuration = 15f;

    private AudioClip _clip;
    private bool _isRecording;
    private float _silenceTimer;
    private bool _voiceDetected;
    private string _selectedDevice;

    public string[] AvailableDevices => Microphone.devices;
    public string SelectedDevice => _selectedDevice;

    public event Action<byte[]> OnRecordingComplete;

    void Start()
    {
        // 기본값: 첫 번째 마이크
        if (Microphone.devices.Length > 0)
            _selectedDevice = Microphone.devices[0];
        else
            Debug.LogError("마이크를 찾을 수 없습니다.");
    }

    void Update()
    {
        if (!_isRecording) return;

        float volume = GetCurrentVolume();

        if (volume > silenceThreshold)
        {
            _voiceDetected = true;
            _silenceTimer = 0f;
        }
        else if (_voiceDetected)
        {
            _silenceTimer += Time.deltaTime;
            if (_silenceTimer >= silenceDuration)
                StopRecording();
        }
    }

    /// <summary>마이크 장치 선택 (인덱스)</summary>
    public void SelectDevice(int index)
    {
        if (index < 0 || index >= Microphone.devices.Length) return;
        _selectedDevice = Microphone.devices[index];
        Debug.Log($"🎤 마이크 선택: {_selectedDevice}");
    }

    /// <summary>마이크 장치 선택 (이름)</summary>
    public void SelectDevice(string deviceName)
    {
        _selectedDevice = deviceName;
        Debug.Log($"🎤 마이크 선택: {_selectedDevice}");
    }

    public void StartRecording()
    {
        if (_isRecording || string.IsNullOrEmpty(_selectedDevice)) return;
        _isRecording = true;
        _voiceDetected = false;
        _silenceTimer = 0f;
        _clip = Microphone.Start(_selectedDevice, false, (int)maxRecordDuration, sampleRate);
        Debug.Log($"🎤 녹음 시작: {_selectedDevice}");
    }

    public void StopRecording()
    {
        if (!_isRecording) return;
        _isRecording = false;

        int pos = Microphone.GetPosition(_selectedDevice);
        Microphone.End(_selectedDevice);

        var trimmed = TrimClip(_clip, pos);
        byte[] wav = ConvertToWav(trimmed);
        OnRecordingComplete?.Invoke(wav);
    }

    float GetCurrentVolume()
    {
        if (_clip == null) return 0f;
        int pos = Microphone.GetPosition(_selectedDevice);
        int sampleCount = 128;
        float[] samples = new float[sampleCount];
        int start = Mathf.Max(0, pos - sampleCount);
        _clip.GetData(samples, start);
        float sum = 0f;
        foreach (var s in samples) sum += s * s;
        return Mathf.Sqrt(sum / sampleCount);
    }

    AudioClip TrimClip(AudioClip original, int endSample)
    {
        float[] data = new float[endSample * original.channels];
        original.GetData(data, 0);
        var trimmed = AudioClip.Create("trimmed", endSample,
            original.channels, original.frequency, false);
        trimmed.SetData(data, 0);
        return trimmed;
    }

    byte[] ConvertToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int byteCount = samples.Length * 2;
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + byteCount);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); bw.Write((short)1);
        bw.Write((short)clip.channels);
        bw.Write(clip.frequency);
        bw.Write(clip.frequency * clip.channels * 2);
        bw.Write((short)(clip.channels * 2));
        bw.Write((short)16);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(byteCount);
        foreach (var s in samples)
        {
            short v = (short)Mathf.Clamp(s * 32767f, -32768f, 32767f);
            bw.Write(v);
        }
        return ms.ToArray();
    }
}