using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class AIVtuberController : MonoBehaviour
{
    [Header("컴포넌트 연결")]
    public WhisperSTT whisperSTT; // WindowsSTT -> WhisperSTT
    public GPTManager gpt;
    public VoicevoxTTS tts;

    public enum State { Idle, Listening, Thinking, Speaking }
    public State CurrentState { get; private set; } = State.Idle;

    void Start()
    {
        whisperSTT.OnTranscriptionComplete += OnSTTDone;
        gpt.OnResponseReceived += OnGPTDone;
        tts.OnSpeakingFinished += OnSpeakingFinished;

        Debug.Log("준비 완료 (스페이스바를 누른 채로 말씀하세요)");
    }

    void Update()
    {
        // 스페이스바를 누르고 있을 때 녹음 시작
        if (Keyboard.current.spaceKey.wasPressedThisFrame && CurrentState == State.Idle)
        {
            StartListening();
        }
        
        // 스페이스바를 떼면 녹음 중지 및 분석 시작
        if (Keyboard.current.spaceKey.wasReleasedThisFrame && CurrentState == State.Listening)
        {
            StopListening();
        }
    }

    void StartListening()
    {
        CurrentState = State.Listening;
        Debug.Log("🎤 듣는 중... (말이 끝나면 스페이스바를 떼세요)");
        whisperSTT.StartListening();
    }

    void StopListening()
    {
        CurrentState = State.Thinking;
        whisperSTT.StopListening();
    }

    void OnSTTDone(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            CurrentState = State.Idle;
            Debug.Log("인식된 내용이 없습니다.");
            return;
        }

        Debug.Log($"사용자: {text}");
        Debug.Log("🤖 생각 중...");
        gpt.SendMessage(text);
    }

    void OnGPTDone(string reply)
    {
        CurrentState = State.Speaking;
        Debug.Log($"AI: {reply}");
        Debug.Log("🔊 말하는 중...");
        tts.Speak(reply);
    }

    void OnSpeakingFinished()
    {
        CurrentState = State.Idle;
        Debug.Log("대기 중... (스페이스바를 누르세요)");
    }
}
