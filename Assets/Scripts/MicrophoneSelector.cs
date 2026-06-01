using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MicrophoneSelector : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Dropdown micDropdown;
    public Text statusText;

    private List<string> _devices = new List<string>();

    void Start()
    {
        RefreshMicrophones();
        
        if (micDropdown != null)
        {
            micDropdown.onValueChanged.AddListener(OnMicChanged);
        }
    }

    public void RefreshMicrophones()
    {
        _devices.Clear();
        _devices.AddRange(Microphone.devices);

        if (micDropdown != null)
        {
            micDropdown.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            
            if (_devices.Count == 0)
            {
                options.Add(new Dropdown.OptionData("마이크를 찾을 수 없음"));
            }
            else
            {
                foreach (var device in _devices)
                {
                    options.Add(new Dropdown.OptionData(device));
                }
            }
            
            micDropdown.AddOptions(options);
            
            if (_devices.Count > 0)
            {
                UpdateStatus($"현재 선택된 마이크: {_devices[0]} (시스템 기본값 권장)");
            }
        }
    }

    void OnMicChanged(int index)
    {
        if (index < 0 || index >= _devices.Count) return;
        
        string selectedMic = _devices[index];
        UpdateStatus($"마이크 변경: {selectedMic}");
        
        PlayerPrefs.SetString("SelectedMicrophone", selectedMic);
        Debug.Log($"[Microphone] 선택됨: {selectedMic}. Windows STT 사용을 위해 시스템 기본 장치로 설정되어 있는지 확인하세요.");
    }

    void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        else Debug.Log(msg);
    }
}