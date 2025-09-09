using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class HealthBarManager : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement _root;
    private VisualElement _healthBar;
    private VisualElement _faintOverlay;
    
    [Header("Health Bar Elements")]
    private VisualElement _bar1;
    private VisualElement _bar1Fill;
    private VisualElement _bar2;
    private VisualElement _bar2Fill;
    private VisualElement _bar3;
    private VisualElement _bar3Fill;
    
    [Header("Settings")]
    public int maxHP = 100;
    public float faintReviveTime = 5f;
    public bool enableFaintSystem = true;
    
    [Header("Current State")]
    public int currentHP = 100;
    public bool isFainted = false;
    public float faintTimeRemaining = 0f;
    
    [Header("Colors")]
    public Color normalBarColor = new Color(255f/255f, 255f/255f, 255f/255f, 0.4f);
    public Color criticalBarColor = new Color(154f/255f, 8f/255f, 11f/255f, 1.0f);
    public Color thirdBarColor = new Color(235f/255f, 255f/255f, 123f/255f, 0.4f);
    
    // Static 이벤트는 HUDEvents로 이동됨
    
    private Coroutine _faintCountdownCoroutine;
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        CacheUIElements();
        UpdateHealthBarUI();
        SetFaintState(false, immediate: true);
    }
    
    private void CacheUIElements()
    {
        _healthBar = _root.Q<VisualElement>("healthBar");
        _faintOverlay = _root.Q<VisualElement>("faintOverlay");
        
        _bar1 = _root.Q<VisualElement>("bar1");
        _bar1Fill = _root.Q<VisualElement>("bar1Fill");
        _bar2 = _root.Q<VisualElement>("bar2");
        _bar2Fill = _root.Q<VisualElement>("bar2Fill");
        _bar3 = _root.Q<VisualElement>("bar3");
        _bar3Fill = _root.Q<VisualElement>("bar3Fill");
    }
    
    public void SetHealth(int newHP, int newMaxHP = -1)
    {
        if (newMaxHP > 0)
        {
            maxHP = newMaxHP;
        }
        
        int previousHP = currentHP;
        currentHP = Mathf.Clamp(newHP, 0, maxHP);
        
        UpdateHealthBarUI();
        HUDEvents.TriggerHealthChange(currentHP, maxHP);
        
        // 기절 상태 체크
        if (enableFaintSystem)
        {
            if (currentHP <= 0 && !isFainted)
            {
                SetFaintState(true, faintReviveTime);
                // 기절 이벤트 트리거 (SetFaintState 내부가 아닌 여기서)
                HUDEvents.TriggerFaintState(true);
            }
            else if (currentHP > 0 && isFainted)
            {
                SetFaintState(false); // 체력이 회복되면 기절 해제
                // 기절 해제 이벤트 트리거
                HUDEvents.TriggerFaintState(false);
            }
        }
        
        Debug.Log($"[HealthBarManager] 체력 변경: {previousHP} -> {currentHP}/{maxHP}");
    }
    
    public void DamageHealth(int damage)
    {
        SetHealth(currentHP - damage);
    }
    
    public void HealHealth(int healAmount)
    {
        SetHealth(currentHP + healAmount);
    }
    
    public void SetMaxHealth(int newMaxHP)
    {
        maxHP = newMaxHP;
        SetHealth(currentHP, maxHP);
    }
    
    private void UpdateHealthBarUI()
    {
        // 체력바는 40HP씩 3개 구간으로 나뉨 (총 120HP 표시 가능, 실제 최대는 100HP)
        int hp1 = Mathf.Clamp(currentHP, 0, 40);
        int hp2 = Mathf.Clamp(currentHP - 40, 0, 40);
        int hp3 = Mathf.Clamp(currentHP - 80, 0, 20); // 3번째는 20HP만 (최대 100HP)
        
        // 첫 번째 체력바 (0-40HP)
        if (_bar1Fill != null)
        {
            if (currentHP <= 0)
            {
                // 기절 상태일 때는 12%로 고정하고 빨간색
                _bar1Fill.style.width = new Length(12f, LengthUnit.Percent);
                _bar1Fill.style.backgroundColor = criticalBarColor;
            }
            else
            {
                // 정상 상태일 때는 실제 체력 비례하고 원래 색상
                float percent1 = (float)hp1 / 40f * 100f;
                _bar1Fill.style.width = new Length(percent1, LengthUnit.Percent);
                _bar1Fill.style.backgroundColor = normalBarColor;
            }
        }
        
        // 두 번째 체력바 (41-80HP)
        if (_bar2Fill != null)
        {
            float percent2 = (float)hp2 / 40f * 100f;
            _bar2Fill.style.width = new Length(percent2, LengthUnit.Percent);
            _bar2Fill.style.backgroundColor = normalBarColor;
        }
        
        // 세 번째 체력바 (81-100HP)
        if (_bar3Fill != null && _bar3 != null)
        {
            if (hp3 > 0)
            {
                _bar3.style.display = DisplayStyle.Flex;
                float percent3 = (float)hp3 / 20f * 100f;
                _bar3Fill.style.width = new Length(percent3, LengthUnit.Percent);
                _bar3Fill.style.backgroundColor = thirdBarColor;
            }
            else
            {
                _bar3.style.display = DisplayStyle.None;
            }
        }
        
        Debug.Log($"[HealthBarManager] 체력바 UI 업데이트: {currentHP}/{maxHP} (바1: {hp1}/40, 바2: {hp2}/40, 바3: {hp3}/20)");
    }
    
    public void SetFaintState(bool isFainted, float reviveTime = 5f, bool immediate = false)
    {
        this.isFainted = isFainted;
        
        if (_faintOverlay != null)
        {
            if (isFainted)
            {
                // 기절 상태 진입
                if (immediate)
                {
                    _faintOverlay.style.opacity = 1f;
                }
                else
                {
                    _faintOverlay.RemoveFromClassList("ui-hidden");
                    _faintOverlay.AddToClassList("ui-visible");
                }
                
                faintTimeRemaining = reviveTime;
                
                // 기절 카운트다운 시작
                if (_faintCountdownCoroutine != null)
                {
                    StopCoroutine(_faintCountdownCoroutine);
                }
                _faintCountdownCoroutine = StartCoroutine(FaintCountdown());
                
                Debug.Log($"[HealthBarManager] 기절 상태 진입 ({reviveTime}초)");
            }
            else
            {
                // 기절 상태 해제
                if (immediate)
                {
                    _faintOverlay.style.opacity = 0f;
                }
                else
                {
                    _faintOverlay.AddToClassList("ui-hidden");
                    _faintOverlay.RemoveFromClassList("ui-visible");
                }
                
                faintTimeRemaining = 0f;
                
                // 기절 카운트다운 중단
                if (_faintCountdownCoroutine != null)
                {
                    StopCoroutine(_faintCountdownCoroutine);
                    _faintCountdownCoroutine = null;
                }
                
                // 이벤트는 외부에서 호출해야 함 (무한 루프 방지)
                // HUDEvents.TriggerFaintState(false);
                // HUDEvents.TriggerPlayerRevive();
                Debug.Log("[HealthBarManager] 기절 상태 해제");
            }
        }
    }
    
    private IEnumerator FaintCountdown()
    {
        while (faintTimeRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
            faintTimeRemaining -= 1f;
            
            Debug.Log($"[HealthBarManager] 기절 카운트다운: {faintTimeRemaining:F0}초 남음");
        }
        
        // 부활 시간이 끝나면 사망 처리
        if (isFainted && faintTimeRemaining <= 0)
        {
            HUDEvents.TriggerPlayerDeath();
            Debug.Log("[HealthBarManager] 부활 시간 종료 - 사망 처리");
        }
        
        _faintCountdownCoroutine = null;
    }
    
    public void RevivePlayer(int reviveHP = 30)
    {
        if (!isFainted) return;
        
        SetFaintState(false);
        SetHealth(reviveHP);
        
        Debug.Log($"[HealthBarManager] 플레이어 부활 (체력: {reviveHP})");
    }
    
    public void SetHealthBarVisibility(bool visible)
    {
        if (_healthBar != null)
        {
            _healthBar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
    
    public void SetFaintOverlayVisibility(bool visible)
    {
        if (_faintOverlay != null)
        {
            _faintOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
    
    public float GetHealthPercentage()
    {
        return maxHP > 0 ? (float)currentHP / maxHP : 0f;
    }
    
    public bool IsLowHealth(float threshold = 0.3f)
    {
        return GetHealthPercentage() <= threshold;
    }
    
    public bool IsCriticalHealth(float threshold = 0.1f)
    {
        return GetHealthPercentage() <= threshold;
    }
    
    public void SetBarColors(Color normal, Color critical, Color third)
    {
        normalBarColor = normal;
        criticalBarColor = critical;
        thirdBarColor = third;
        
        UpdateHealthBarUI();
    }
    
    public void AnimateHealthChange(int targetHP, float duration = 0.5f)
    {
        StartCoroutine(AnimateHealthChangeCoroutine(targetHP, duration));
    }
    
    private IEnumerator AnimateHealthChangeCoroutine(int targetHP, float duration)
    {
        int startHP = currentHP;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            int interpolatedHP = Mathf.RoundToInt(Mathf.Lerp(startHP, targetHP, progress));
            currentHP = interpolatedHP;
            UpdateHealthBarUI();
            
            yield return null;
        }
        
        SetHealth(targetHP);
    }
    
    // Getter 메서드들
    public int GetCurrentHealth() => currentHP;
    public int GetMaxHealth() => maxHP;
    public bool IsFainted() => isFainted;
    public float GetFaintTimeRemaining() => faintTimeRemaining;
    public bool IsAlive() => currentHP > 0 || isFainted;
    public bool IsDead() => currentHP <= 0 && !isFainted;
    
    // 디버그/테스트 메서드들
    public void DebugSetHealth(int hp)
    {
        SetHealth(hp);
    }
    
    public void DebugDamage(int amount = 10)
    {
        DamageHealth(amount);
    }
    
    public void DebugHeal(int amount = 10)
    {
        HealHealth(amount);
    }
    
    public void DebugToggleFaint()
    {
        SetFaintState(!isFainted, faintReviveTime);
    }
}