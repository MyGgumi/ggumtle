using System;
using UnityEngine;
using UnityEngine.UIElements;

public class GameTimeManager : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement _root;
    private Label _timeLabel;
    private Label _statusLabel;

    [Header("Ggumtle Progress UI")]
    private VisualElement _ggumtleArea;
    private VisualElement[] _fillMasks = new VisualElement[3];
    private VisualElement[] _fills = new VisualElement[3];
    private Label _countLabel;

    [Header("Current State")]
    public TimeSpan currentTime = TimeSpan.Zero;
    public string currentStatus = "";
    public int ggumtleLevel = 1;
    public float ggumtleProgress = 0f;

    // Static 이벤트는 HUDEvents로 이동됨

    private void Awake()
    {
        // UIElements는 메인 컨트롤러에서 주입받음
    }

    public void Initialize(VisualElement root)
    {
        _root = root;
        CacheUIElements();
        UpdateUI();
    }

    private void CacheUIElements()
    {
        _timeLabel = _root.Q<Label>("timeLabel");
        _statusLabel = _root.Q<Label>("statusText");
        _ggumtleArea = _root.Q<VisualElement>("ggumtleArea");
        _countLabel = _root.Q<Label>("count")?.Q<Label>();

        // UXML의 하드코딩된 텍스트들 즉시 제거
        if (_timeLabel != null)
        {
            _timeLabel.text = "";
        }

        if (_statusLabel != null)
        {
            _statusLabel.text = "";
        }

        if (_countLabel != null)
        {
            _countLabel.text = "";
        }

        // 꿈틀 진행도 마스크들 캐싱
        for (int i = 0; i < 3; i++)
        {
            _fillMasks[i] = _root.Q<VisualElement>($"fillMask_{i + 1}of3");
            _fills[i] = _root.Q<VisualElement>($"fill_{i + 1}of3");

            // 모든 fill 요소의 UXML 기본 높이를 0%로 초기화
            if (_fills[i] != null)
            {
                _fills[i].style.height = new Length(0, LengthUnit.Percent);
            }
        }
    }

    public void SetTimeRemaining(TimeSpan t)
    {
        currentTime = t;
        if (_root == null)
        {
            Debug.LogError(
                "[GameTimeManager] _root가 null입니다. Initialize가 호출되지 않았습니다."
            );
            return;
        }

        if (_timeLabel != null)
        {
            _timeLabel.text = $"{t.Minutes:00}:{t.Seconds:00}";
        }
    }

    public void SetGameTime(TimeSpan time)
    {
        SetTimeRemaining(time);
    }

    public void SetStatusMessage(string message)
    {
        currentStatus = message;
        if (_statusLabel != null)
        {
            _statusLabel.text = message;
        }
    }

    public void SetGgumtleProgress(int level, float progress)
    {
        int previousLevel = ggumtleLevel;
        SetGgumtleProgressInternal(level, progress);

        // 레벨이 올라갔을 때만 이벤트 발생 (실제 게임 로직)
        if (level > previousLevel && level > 0)
        {
            HUDEvents.TriggerGgumtleProgress(level);
        }
    }

    public void SetGgumtleProgressInternal(int level, float progress)
    {
        ggumtleLevel = level;
        ggumtleProgress = progress;
        UpdateGgumtleUI();
    }

    private void UpdateGgumtleUI()
    {
        // 단계별 고정 높이 정의
        float[] stageHeights = { 0f, 33f, 67f, 100f }; // 0단계=0%, 1단계=33%, 2단계=67%, 3단계=100%

        // 모든 마스크 숨기기
        for (int i = 1; i <= 3; i++)
        {
            var mask = _root.Q<VisualElement>($"fillMask_{i}of3");
            if (mask != null)
                mask.style.display = DisplayStyle.None;
        }

        // 현재 단계에 맞는 마스크 표시하고 높이 설정
        if (ggumtleLevel >= 1 && ggumtleLevel <= 3)
        {
            var currentMask = _root.Q<VisualElement>($"fillMask_{ggumtleLevel}of3");
            if (currentMask != null)
            {
                currentMask.style.display = DisplayStyle.Flex;

                var fill = currentMask.Q<VisualElement>($"fill_{ggumtleLevel}of3");
                if (fill != null)
                {
                    // 단계별 고정 높이 설정
                    float heightPercent = stageHeights[ggumtleLevel];
                    fill.style.height = new Length(heightPercent, LengthUnit.Percent);

                    Debug.Log(
                        $"[GameTimeManager] {ggumtleLevel}단계: fill_{ggumtleLevel}of3 높이 {heightPercent}% 설정"
                    );
                }
            }
        }
        else if (ggumtleLevel == 0)
        {
            // 0단계는 모든 마스크 숨김 (이미 위에서 처리됨)
            Debug.Log($"[GameTimeManager] 0단계: 모든 진행도 숨김 (0%)");
        }

        // 카운트 텍스트 업데이트
        var countContainer = _root.Q<VisualElement>("count");
        var countLabel = countContainer?.Q<Label>();
        if (countLabel != null)
        {
            countLabel.text = $"<size=32>{ggumtleLevel}</size><size=20>/3</size>";
        }

        Debug.Log(
            $"[GameTimeManager] 꿈틀 진행도 업데이트: {ggumtleLevel}단계 (고정 높이: {(ggumtleLevel >= 0 && ggumtleLevel <= 3 ? stageHeights[ggumtleLevel] : 0)}%)"
        );
    }

    private void UpdateUI()
    {
        SetGameTime(currentTime);
        SetStatusMessage(currentStatus);
        UpdateGgumtleUI();
    }

    public TimeSpan GetCurrentTime() => currentTime;

    public string GetCurrentStatus() => currentStatus;

    public (int level, float progress) GetGgumtleProgress() => (ggumtleLevel, ggumtleProgress);
}
