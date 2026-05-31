using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class AIVtuberController : MonoBehaviour
{
    [Header("컴포넌트 연결")]
    public WindowsSTT windowsSTT;
    public GPTManager gpt;
    public VoicevoxTTS tts;

    [Header("UI (선택)")]
    public TMP_Text subtitleText;
    public TMP_Text statusText;

    public enum State { Idle, Listening, Thinking, Speaking }
    public State CurrentState { get; private set; } = State.Idle;

    void Start()
    {
        windowsSTT.OnTranscriptionComplete += OnSTTDone;  // ← stt → windowsSTT
        gpt.OnResponseReceived += OnGPTDone;
        tts.OnSpeakingFinished += OnSpeakingFinished;     // ← OnSpeak → OnSpeakingFinished

        SetStatus("대기 중... (스페이스바를 누르세요)");
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && CurrentState == State.Idle)
        {
            StartListening();
        }
    }

    void StartListening()
    {
        CurrentState = State.Listening;
        SetStatus("🎤 듣는 중...");
        windowsSTT.StartListening();  // ← mic.StartRecording() 대신
    }

    void OnSTTDone(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            CurrentState = State.Idle;
            SetStatus("대기 중... (스페이스바를 누르세요)");
            return;
        }
        SetSubtitle($"사용자: {text}");
        SetStatus("🤖 생각 중...");
        gpt.SendMessage(text);
    }

    void OnGPTDone(string reply)
    {
        CurrentState = State.Speaking;
        SetSubtitle($"AI: {reply}");
        SetStatus("🔊 말하는 중...");
        tts.Speak(reply);
    }

    void OnSpeakingFinished()  // ← OnSpeak에서 이름 변경
    {
        CurrentState = State.Idle;
        SetStatus("대기 중... (스페이스바를 누르세요)");
    }

    void SetSubtitle(string t) { if (subtitleText) subtitleText.text = t; }
    void SetStatus(string t)
    {
        if (statusText) statusText.text = t;
        Debug.Log($"[상태] {t}");
    }
}