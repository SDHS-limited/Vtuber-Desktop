using System;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class WindowsSTT : MonoBehaviour
{
    [Header("인식 설정")]
    public ConfidenceLevel minimumConfidence = ConfidenceLevel.Medium;

    public event Action<string> OnTranscriptionComplete;
    public bool IsListening { get; private set; }

    private DictationRecognizer _dictationRecognizer;

    void Start()
    {
        InitRecognizer();
    }

    void InitRecognizer()
    {
        try
        {
            // UnityEngine.Windows.Speech.DictationRecognizer는 시스템 기본 음성 언어를 사용합니다.
            _dictationRecognizer = new DictationRecognizer();

            _dictationRecognizer.DictationResult += OnDictationResult;
            _dictationRecognizer.DictationComplete += OnDictationComplete;
            _dictationRecognizer.DictationError += OnDictationError;

            Debug.Log("Windows STT (UnityEngine.Windows.Speech) 초기화 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"Windows STT 초기화 실패: {e.Message}\n" +
                           "Windows 설정 -> 시간 및 언어 -> 음성에서 한국어 음성팩과 온라인 음성 인식이 활성화되어 있는지 확인하세요.");
        }
    }

    public void StartListening()
    {
        if (_dictationRecognizer == null || IsListening) return;

        if (_dictationRecognizer.Status == SpeechSystemStatus.Stopped)
        {
            try
            {
                _dictationRecognizer.Start();
                IsListening = true;
                Debug.Log("🎤 Windows STT 듣는 중...");
            }
            catch (Exception e)
            {
                Debug.LogError($"STT 시작 실패: {e.Message}");
            }
        }
    }

    public void StopListening()
    {
        if (_dictationRecognizer == null || !IsListening) return;

        if (_dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            _dictationRecognizer.Stop();
            IsListening = false;
        }
    }

    private void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        if (confidence < minimumConfidence)
        {
            Debug.Log($"[STT] 신뢰도 낮음 ({confidence}): {text}");
            return;
        }

        Debug.Log($"[STT] 인식됨: {text}");
        OnTranscriptionComplete?.Invoke(text);
    }

    private void OnDictationComplete(DictationCompletionCause cause)
    {
        if (cause != DictationCompletionCause.Complete)
        {
            Debug.LogWarning($"[STT] 인식 중단 원인: {cause}");
        }
        IsListening = false;
    }

    private void OnDictationError(string error, int hresult)
    {
        Debug.LogError($"[STT] 오류: {error} (HRESULT: {hresult})");
        IsListening = false;
    }

    void OnDestroy()
    {
        if (_dictationRecognizer != null)
        {
            _dictationRecognizer.DictationResult -= OnDictationResult;
            _dictationRecognizer.DictationComplete -= OnDictationComplete;
            _dictationRecognizer.DictationError -= OnDictationError;

            if (_dictationRecognizer.Status == SpeechSystemStatus.Running)
                _dictationRecognizer.Stop();

            _dictationRecognizer.Dispose();
        }
    }
}