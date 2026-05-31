using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class VoicevoxTTS : MonoBehaviour
{
    [Header("VOICEVOX 설정 (로컬 서버)")]
    public string voicevoxUrl = "http://127.0.0.1:50021";
    public int speakerId = 3;

    [Header("컴포넌트 연결")]
    public AudioSource audioSource;

    [Header("Live2D 립싱크 파라미터 이름")]
    public string lipSyncParamName = "ParamMouthOpenY"; // Live2D 모델의 입 파라미터

    [Header("립싱크 설정")]
    public float lipSyncMultiplier = 8f;
    public float lipSyncSmoothing = 10f;

    private float _currentMouthValue = 0f;
    private Live2D.Cubism.Core.CubismModel _cubismModel;

    public bool IsSpeaking { get; private set; }
    public event Action OnSpeakingFinished;

    void Start()
    {
        // Live2D 모델 자동 탐색
        _cubismModel = FindAnyObjectByType<Live2D.Cubism.Core.CubismModel>();
        if (_cubismModel == null)
            Debug.LogWarning("CubismModel을 찾을 수 없습니다. Live2D 오브젝트를 확인하세요.");
    }

    void Update()
    {
        if (_cubismModel == null) return;

        // 오디오 볼륨으로 입 움직임 계산
        float targetMouth = 0f;
        if (audioSource != null && audioSource.isPlaying)
        {
            float[] samples = new float[256];
            audioSource.GetOutputData(samples, 0);
            float rms = 0f;
            foreach (var s in samples) rms += s * s;
            rms = Mathf.Sqrt(rms / samples.Length);
            targetMouth = Mathf.Clamp01(rms * lipSyncMultiplier);
        }

        // 부드럽게 보간
        _currentMouthValue = Mathf.Lerp(
            _currentMouthValue, targetMouth, Time.deltaTime * lipSyncSmoothing);

        // Live2D 파라미터에 적용
        var parameters = _cubismModel.Parameters;
        foreach (var param in parameters)
        {
            if (param.Id == lipSyncParamName)
            {
                param.Value = _currentMouthValue;
                break;
            }
        }
    }

    public void Speak(string text)
    {
        StartCoroutine(SpeakCoroutine(text));
    }

    IEnumerator SpeakCoroutine(string text)
    {
        IsSpeaking = true;

        // Step 1: audio_query 생성
        string queryUrl = $"{voicevoxUrl}/audio_query" +
            $"?text={UnityWebRequest.EscapeURL(text)}&speaker={speakerId}";

        var queryReq = UnityWebRequest.PostWwwForm(queryUrl, "");
        yield return queryReq.SendWebRequest();

        if (queryReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"VOICEVOX query 오류: {queryReq.error}");
            IsSpeaking = false;
            yield break;
        }

        string queryJson = queryReq.downloadHandler.text;

        // Step 2: synthesis 요청
        string synthUrl = $"{voicevoxUrl}/synthesis?speaker={speakerId}";
        var synthReq = new UnityWebRequest(synthUrl, "POST");
        synthReq.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(queryJson));
        synthReq.downloadHandler = new DownloadHandlerAudioClip(synthUrl, AudioType.WAV);
        synthReq.SetRequestHeader("Content-Type", "application/json");

        yield return synthReq.SendWebRequest();

        if (synthReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"VOICEVOX synthesis 오류: {synthReq.error}");
            IsSpeaking = false;
            yield break;
        }

        var clip = DownloadHandlerAudioClip.GetContent(synthReq);
        audioSource.clip = clip;
        audioSource.Play();

        // 재생 완료 대기
        yield return new WaitUntil(() => !audioSource.isPlaying);

        IsSpeaking = false;
        OnSpeakingFinished?.Invoke();
    }
}