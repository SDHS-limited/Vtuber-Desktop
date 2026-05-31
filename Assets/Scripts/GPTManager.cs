using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GPTManager : MonoBehaviour
{
    [Header("API 설정")]
    public string apiKey = "YOUR_OPENAI_API_KEY";
    public string model = "gpt-4o";

    [Header("캐릭터 설정")]
    [TextArea(3, 6)]
    public string systemPrompt = 
        "당신은 귀엽고 활발한 AI 버튜버입니다. " +
        "짧고 자연스럽게 대화하며, 친근하게 말합니다. " +
        "한 번에 2~3문장 이내로 답변하세요.";

    private List<Message> _history = new();
    public event Action<string> OnResponseReceived;

    void Start()
    {
        _history.Add(new Message { role = "system", content = systemPrompt });
    }

    public void SendMessage(string userText)
    {
        _history.Add(new Message { role = "user", content = userText });
        StartCoroutine(SendCoroutine());
    }

    IEnumerator SendCoroutine()
    {
        var body = new RequestBody { model = model, messages = _history };
        string json = JsonUtility.ToJson(body);

        var req = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"GPT 오류: {req.error}\n{req.downloadHandler.text}");
            yield break;
        }

        var res = JsonUtility.FromJson<ChatResponse>(req.downloadHandler.text);
        string reply = res.choices[0].message.content;

        _history.Add(new Message { role = "assistant", content = reply });
        Debug.Log($"🤖 GPT: {reply}");
        OnResponseReceived?.Invoke(reply);
    }

    public void ClearHistory()
    {
        _history.Clear();
        _history.Add(new Message { role = "system", content = systemPrompt });
    }

    // ─── 직렬화용 클래스 ────────────────────────────────
    [Serializable] public class Message { public string role, content; }
    [Serializable] class RequestBody { public string model; public List<Message> messages; }
    [Serializable] class ChatResponse { public Choice[] choices; }
    [Serializable] class Choice { public Message message; }
}
