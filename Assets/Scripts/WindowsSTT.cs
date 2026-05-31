using System;
using System.Collections;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Speech.Recognition;
#endif

public class WindowsSTT : MonoBehaviour
{
    [Header("언어 설정")]
    public string language = "ko-KR"; // 한국어

    [Header("인식 설정")]
    public float confidenceThreshold = 0.5f; // 이 이하 신뢰도는 무시

    public event Action<string> OnTranscriptionComplete;
    public bool IsListening { get; private set; }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private SpeechRecognitionEngine _recognizer;
    private string _pendingResult;
    private bool _hasResult;

    void Start()
    {
        InitRecognizer();
    }

    void InitRecognizer()
    {
        try
        {
            var culture = new System.Globalization.CultureInfo(language);
            _recognizer = new SpeechRecognitionEngine(culture);

            // 자유 발화 인식 (딕테이션 모드)
            _recognizer.LoadGrammar(new DictationGrammar());

            _recognizer.SpeechRecognized += OnSpeechRecognized;
            _recognizer.SpeechRecognitionRejected += OnSpeechRejected;

            // 기본 마이크 사용
            _recognizer.SetInputToDefaultAudioDevice();

            Debug.Log($"✅ Windows STT 초기화 완료 ({language})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Windows STT 초기화 실패: {e.Message}\n" +
                           "Windows 설정 → 시간 및 언어 → 음성에서 한국어 음성팩을 설치하세요.");
        }
    }

    void Update()
    {
        // 메인 스레드에서 이벤트 처리 (Unity는 멀티스레드 이벤트 직접 호출 불가)
        if (_hasResult)
        {
            _hasResult = false;
            OnTranscriptionComplete?.Invoke(_pendingResult);
        }
    }

    public void StartListening()
    {
        if (_recognizer == null || IsListening) return;
        IsListening = true;
        _recognizer.RecognizeAsync(RecognizeMode.Single);
        Debug.Log("🎤 Windows STT 듣는 중...");
    }

    public void StopListening()
    {
        if (_recognizer == null || !IsListening) return;
        _recognizer.RecognizeAsyncStop();
        IsListening = false;
    }

    void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result.Confidence < confidenceThreshold)
        {
            Debug.Log($"신뢰도 낮음 ({e.Result.Confidence:F2}), 무시됨");
            IsListening = false;
            return;
        }

        string text = e.Result.Text;
        Debug.Log($"📝 인식됨 ({e.Result.Confidence:F2}): {text}");

        // 메인 스레드로 전달
        _pendingResult = text;
        _hasResult = true;
        IsListening = false;
    }

    void OnSpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
    {
        Debug.Log("❌ 음성 인식 실패 (다시 말씀해 주세요)");
        IsListening = false;

        // 인식 실패도 컨트롤러에 알려줘야 다시 대기 상태로 전환
        _pendingResult = "";
        _hasResult = true;
    }

    void OnDestroy()
    {
        _recognizer?.RecognizeAsyncStop();
        _recognizer?.Dispose();
    }

#else
    void Start() => Debug.LogError("Windows STT는 Windows에서만 동작합니다.");
    public void StartListening() {}
    public void StopListening() {}
#endif
}