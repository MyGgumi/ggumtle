using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MonggingHUDExample : MonoBehaviour
{
    [Header("HUD Controller")]
    public UniversalHUDController hudController;
    
    [Header("Test Settings")]
    [SerializeField]
    private bool enableKeyboardTesting = false; // 키보드 테스트 활성화/비활성화
    private int currentTestHP = 100;
    private int currentGgumtleLevel = 0;
    private bool playersInitialized = false;
    
    [Header("Timer Settings")]
    private TimeSpan gameTimeRemaining = new TimeSpan(0, 15, 0);
    private bool isTimerRunning = false;
    private Coroutine timerCoroutine;
    
    void Start()
    {
        if (hudController == null)
        {
            hudController = FindFirstObjectByType<UniversalHUDController>();
            if (hudController == null)
            {
                Debug.LogError("[MonggingHUDExample] UniversalHUDController를 찾을 수 없습니다!");
                return;
            }
        }
        
        StartCoroutine(InitializeHUD());
    }
    
    IEnumerator InitializeHUD()
    {
        yield return new WaitForSeconds(1f);
        
        if (hudController == null) yield break;
        
        InitializeGameState();
        yield return new WaitForSeconds(1f);
        
        InitializePlayers();
        yield return new WaitForSeconds(1f);
        
        ShowKeyboardGuide();
    }
    
    private void InitializeGameState()
    {
        gameTimeRemaining = new TimeSpan(0, 15, 0);
        hudController.SetTimeRemaining(gameTimeRemaining);
        hudController.SetStatusMessage("• 꿈 속을 탈출하세요.");
        hudController.SetGgumtleProgress(currentGgumtleLevel, 0f);
        hudController.SetHealth(currentTestHP);
        
        StartGameTimer();
        Debug.Log("[MonggingHUDExample] 게임 상태 초기화 완료");
    }
    
    private void InitializePlayers()
    {
        if (playersInitialized) return;
        
        hudController.UpdatePlayer(1, "나", "yellow", "default", true, true);
        hudController.UpdatePlayer(2, "몽깅이A", "mint", "dead", true, false);
        hudController.UpdatePlayer(3, "몽깅이B", "pink", "faint", true, false);
        hudController.UpdatePlayer(4, "몽깅이C", "blue", "escape", true, false);
        
        playersInitialized = true;
        Debug.Log("[MonggingHUDExample] 플레이어 초기화 완료");
    }
    
    private void ShowKeyboardGuide()
    {
        if (!enableKeyboardTesting) return;
        
        Debug.Log("=== MonggingHUD 키보드 테스트 가이드 ===");
        Debug.Log("1~3키: 꿈틀 진행도 | 4~7키: 상호작용 테스트 | 8키: UI 숨김 | 9키: 땅파기 | 0키: 상호작용시작");
        Debug.Log("Q키: 땅파기애니메이션 | W키: 동료구조상호작용 | E키: 먹이주기상호작용");
        Debug.Log("G키: 슬롯1+ | N키: 슬롯1- | O키: 슬롯2+ | P키: 슬롯3+");
        Debug.Log("H키: 체력감소 | U키: 체력회복 | R키: 역할변경 | T키: 시간변경 | L키: 빛변경");
        Debug.Log("M키: 플레이어참가 | X키: 플레이어리셋 | Space키: 타이머정지/재개");
        Debug.Log("=== 이벤트 트리거 테스트 ===");
        Debug.Log("A키: 꿈틀진행도 | S키: 빛획득(추가) | D키: 플레이어기절 | Y키: 플레이어사망");
        Debug.Log("I키: 플레이어부활 | K키: 시간경고 | J키: 상호작용완료 | B키: 랜덤배너");
        Debug.Log("=== 직접 상호작용 트리거 테스트 (숫자패드) ===");
        Debug.Log("NumPad1: 땅파기(3초) | NumPad2: 동료구조(5초) | NumPad3: 먹이주기(2초) | NumPad0: 취소");
    }
    
    void Update()
    {
        if (hudController == null || Keyboard.current == null || !enableKeyboardTesting) return;
        
        // 꿈틀 진행도 테스트
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SetGgumtleLevel(1);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SetGgumtleLevel(2);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SetGgumtleLevel(3);

        
        // 인벤토리 테스트
        else if (Keyboard.current.gKey.wasPressedThisFrame) TestInventory(1, 1);
        else if (Keyboard.current.nKey.wasPressedThisFrame) TestInventoryUse(1);
        else if (Keyboard.current.oKey.wasPressedThisFrame) TestInventory(2, 1);
        else if (Keyboard.current.pKey.wasPressedThisFrame) TestInventory(3, 1);
        
        // 게임 상태 테스트
        else if (Keyboard.current.rKey.wasPressedThisFrame) TogglePlayerRole();
        else if (Keyboard.current.hKey.wasPressedThisFrame) ChangeHealth(-40);
        else if (Keyboard.current.uKey.wasPressedThisFrame) ChangeHealth(+40);
        else if (Keyboard.current.tKey.wasPressedThisFrame) { hudController.SetTimeRemaining(new TimeSpan(0, 5, 30)); Debug.Log("[MonggingHUDExample] 시간을 5분 30초로 설정"); }
        else if (Keyboard.current.lKey.wasPressedThisFrame) ChangeLightCount();
        
        // 플레이어 테스트
        else if (Keyboard.current.mKey.wasPressedThisFrame) SimulatePlayersJoining();
        else if (Keyboard.current.xKey.wasPressedThisFrame) ResetAllPlayers();
        
        // 타이머 및 기타
        else if (Keyboard.current.spaceKey.wasPressedThisFrame) ToggleTimer();
        else if (Keyboard.current.bKey.wasPressedThisFrame) ShowRandomBanner();
        
        // 이벤트 트리거 테스트
        else if (Keyboard.current.aKey.wasPressedThisFrame) TriggerGgumtleEvent();
        else if (Keyboard.current.sKey.wasPressedThisFrame) TriggerLightEvent();
        else if (Keyboard.current.dKey.wasPressedThisFrame) TriggerFaintEvent();
        else if (Keyboard.current.yKey.wasPressedThisFrame) TriggerDeathEvent();
        else if (Keyboard.current.iKey.wasPressedThisFrame) TriggerReviveEvent();
        else if (Keyboard.current.kKey.wasPressedThisFrame) TriggerTimeWarningEvent();
        else if (Keyboard.current.jKey.wasPressedThisFrame) TriggerInteractionCompleteEvent();
        
        // 상호작용 이벤트 트리거 테스트 - 숫자 패드 사용
        else if (Keyboard.current.numpad1Key.wasPressedThisFrame) TriggerDirectInteraction("dig", 3f, false);
        else if (Keyboard.current.numpad2Key.wasPressedThisFrame) TriggerDirectInteraction("revive", 5f, false);
        else if (Keyboard.current.numpad3Key.wasPressedThisFrame) TriggerDirectInteraction("feeding", 2f, false);
        else if (Keyboard.current.numpad0Key.wasPressedThisFrame) TriggerInteractionCancelEvent();
    }
    
    // === 헬퍼 메서드들 ===
    private void SetGgumtleLevel(int level)
    {
        currentGgumtleLevel = level;
        hudController.SetGgumtleProgress(level, 0.5f);
        Debug.Log($"[MonggingHUDExample] 꿈틀 진행도: {level}단계");
    }
    
    private void TestInteraction(InteractionType type, float progress)
    {
        hudController.SetInteractionUI(true, "", progress, type == InteractionType.Revive);
        Debug.Log($"[MonggingHUDExample] {type} UI 표시 ({progress * 100:F0}%)");
    }
    
    
    private void TestInventory(int slot, int amount)
    {
        hudController.AddInventoryItem(slot, amount);
        Debug.Log($"[MonggingHUDExample] 인벤토리 슬롯 {slot}에 아이템 {amount}개 추가");
    }
    
    private void TestInventoryUse(int slot)
    {
        bool success = hudController.UseInventoryItem(slot);
        Debug.Log($"[MonggingHUDExample] 인벤토리 슬롯 {slot} 사용: {success}");
    }
    
    private void TogglePlayerRole()
    {
        PlayerRole currentRole = hudController.GetCurrentRole();
        PlayerRole newRole = currentRole == PlayerRole.Mongging ? PlayerRole.Mongdung : PlayerRole.Mongging;
        hudController.ChangePlayerRole(newRole);
        Debug.Log($"[MonggingHUDExample] 역할 변경: {currentRole} -> {newRole}");
    }
    
    private void ChangeHealth(int delta)
    {
        currentTestHP = Mathf.Clamp(currentTestHP + delta, 0, 100);
        hudController.SetHealth(currentTestHP);
        Debug.Log($"[MonggingHUDExample] 체력 변경: {currentTestHP}");
    }
    
    private void ChangeLightCount()
    {
        int newLight = UnityEngine.Random.Range(0, 50);
        
        // ResourceManager에서 직접 빛 설정
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SetLightCount(newLight);
            Debug.Log($"[MonggingHUDExample] 빛 개수 설정: {newLight}개 (ResourceManager)");
        }
        else
        {
            hudController.SetLightCount(newLight);
            Debug.Log($"[MonggingHUDExample] 빛 개수 설정: {newLight}개 (HUDController)");
        }
    }
    
    // === 이벤트 트리거 메서드들 ===
    private void TriggerGgumtleEvent()
    {
        int randomLevel = UnityEngine.Random.Range(1, 4);
        HUDEvents.TriggerGgumtleProgress(randomLevel);
        Debug.Log($"[MonggingHUDExample] 꿈틀 진행도 {randomLevel}단계 이벤트");
    }
    
    private void TriggerLightEvent()
    {
        int randomCount = UnityEngine.Random.Range(1, 10);
        
        // ResourceManager에서 직접 빛 추가 (UI 업데이트 포함)
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddLight(randomCount);
            Debug.Log($"[MonggingHUDExample] 빛 {randomCount}개 추가됨 (총: {ResourceManager.Instance.GetCurrentLightCount()}개)");
        }
        else
        {
            // ResourceManager가 없으면 HUDController를 통해 추가
            hudController.SetLightCount(hudController.GetCurrentRole() == PlayerRole.Mongging ? 
                UnityEngine.Random.Range(5, 30) : 0);
            Debug.Log($"[MonggingHUDExample] ResourceManager 없음 - HUDController로 빛 개수 설정");
        }
    }
    
    private void TriggerFaintEvent()
    {
        // 직접 기절 상호작용만 트리거 (TriggerFaintState는 체력 시스템에서 호출)
        HUDEvents.TriggerFaintInteraction(5f);
        Debug.Log("[MonggingHUDExample] 플레이어 기절 상호작용 트리거");
    }
    
    private void TriggerDeathEvent()
    {
        HUDEvents.TriggerPlayerDeath();
        Debug.Log("[MonggingHUDExample] 플레이어 사망 이벤트");
    }
    
    private void TriggerReviveEvent()
    {
        HUDEvents.TriggerPlayerRevive();
        Debug.Log("[MonggingHUDExample] 플레이어 부활 이벤트");
    }
    
    private void TriggerTimeWarningEvent()
    {
        int[] warningTimes = { 1, 3 };
        int randomTime = warningTimes[UnityEngine.Random.Range(0, warningTimes.Length)];
        HUDEvents.TriggerTimeWarning(randomTime);
        Debug.Log($"[MonggingHUDExample] {randomTime}분 시간 경고 이벤트");
    }
    
    private void TriggerInteractionCompleteEvent()
    {
        HUDEvents.TriggerInteractionComplete(InteractionType.Dig, true);
        Debug.Log("[MonggingHUDExample] 상호작용 완료 이벤트");
    }
    
    // 직접 상호작용 트리거 테스트 (dig, revive, feeding)
    private void TriggerDirectInteraction(string interactionName, float duration, bool isCountdown)
    {
        HUDEvents.TriggerInteractionFromInput(interactionName, duration, isCountdown);
        Debug.Log($"[MonggingHUDExample] 직접 상호작용 트리거: {interactionName} ({duration}초, 카운트다운: {isCountdown})");
    }
    
    private void TriggerInteractionCancelEvent()
    {
        HUDEvents.TriggerInteractionCancel();
        Debug.Log("[MonggingHUDExample] 상호작용 취소 이벤트");
    }
    
    private void ShowRandomBanner()
    {
        int randomType = UnityEngine.Random.Range(0, 4);
        switch (randomType)
        {
            case 0: TriggerGgumtleEvent(); break;
            case 1: TriggerLightEvent(); break;
            case 2: TriggerDeathEvent(); break;
            case 3: TriggerInteractionCompleteEvent(); break;
        }
    }
    
    private void SimulatePlayersJoining()
    {
        string[] nicknames = { "김꿈틀", "이몽글", "박몽이", "최꿈꿈" };
        string[] statuses = { "default", "faint", "dead", "escape" };
        string[] colors = { "yellow", "mint", "pink", "blue" };
        
        for (int i = 1; i <= 4; i++)
        {
            string nickname = nicknames[i - 1];
            string status = statuses[UnityEngine.Random.Range(0, statuses.Length)];
            string color = colors[i - 1];
            bool isOnline = UnityEngine.Random.Range(0, 2) == 1;
            bool isHost = i == 1;
            
            hudController.UpdatePlayer(i, nickname, color, status, isOnline, isHost);
        }
        
        Debug.Log("[MonggingHUDExample] 플레이어들 랜덤 참가 시뮬레이션");
    }
    
    private void ResetAllPlayers()
    {
        string[] colors = { "yellow", "mint", "pink", "blue" };
        for (int i = 1; i <= 4; i++)
        {
            hudController.UpdatePlayer(i, $"플레이어{i}", colors[i - 1], "default", false, false);
        }
        Debug.Log("[MonggingHUDExample] 모든 플레이어 초기 상태로 리셋");
    }
    
    // === 타이머 시스템 ===
    private void StartGameTimer()
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        
        isTimerRunning = true;
        timerCoroutine = StartCoroutine(GameTimerCoroutine());
        Debug.Log("[MonggingHUDExample] 게임 타이머 시작");
    }
    
    private void ToggleTimer()
    {
        if (isTimerRunning)
            StopTimer();
        else
            StartGameTimer();
    }
    
    private void StopTimer()
    {
        isTimerRunning = false;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        Debug.Log("[MonggingHUDExample] 게임 타이머 정지");
    }
    
    private IEnumerator GameTimerCoroutine()
    {
        while (isTimerRunning && gameTimeRemaining.TotalSeconds > 0)
        {
            yield return new WaitForSeconds(1f);
            
            if (isTimerRunning)
            {
                gameTimeRemaining = gameTimeRemaining.Subtract(TimeSpan.FromSeconds(1));
                hudController.SetTimeRemaining(gameTimeRemaining);
                
                if (gameTimeRemaining.TotalMinutes <= 3 && gameTimeRemaining.TotalMinutes > 2.98)
                {
                    HUDEvents.TriggerTimeWarning(3);
                }
                else if (gameTimeRemaining.TotalMinutes <= 1 && gameTimeRemaining.TotalMinutes > 0.98)
                {
                    HUDEvents.TriggerTimeWarning(1);
                }
                else if (gameTimeRemaining.TotalSeconds <= 0)
                {
                    HUDEvents.TriggerTimeUp();
                    isTimerRunning = false;
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
    }
}