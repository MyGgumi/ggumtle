using Features.PlayerHealth.Models;
using R3;
using UnityEngine;

namespace Features.PlayerHealth.Services
{
    public interface IPlayerHealthService
    {
        ReadOnlyReactiveProperty<int> CurrentHp { get; }
        ReadOnlyReactiveProperty<int> MaxHp { get; }
        ReadOnlyReactiveProperty<float> HealthPercentage { get; }
        ReadOnlyReactiveProperty<PlayerState> CurrentState { get; }
        ReadOnlyReactiveProperty<bool> IsFainted { get; }
        ReadOnlyReactiveProperty<float> FaintTimeRemaining { get; }
        ReadOnlyReactiveProperty<bool> IsAlive { get; }
        ReadOnlyReactiveProperty<bool> IsDead { get; }
        ReadOnlyReactiveProperty<bool> IsLowHealth { get; }
        ReadOnlyReactiveProperty<bool> IsCriticalHealth { get; }
        ReadOnlyReactiveProperty<HealthBarData> Bar1 { get; }
        ReadOnlyReactiveProperty<HealthBarData> Bar2 { get; }
        ReadOnlyReactiveProperty<HealthBarData> Bar3 { get; }
        ReadOnlyReactiveProperty<float> FaintReviveTime { get; }
        ReadOnlyReactiveProperty<bool> EnableFaintSystem { get; }
        ReadOnlyReactiveProperty<Color> NormalBarColor { get; }
        ReadOnlyReactiveProperty<Color> CriticalBarColor { get; }
        ReadOnlyReactiveProperty<Color> ThirdBarColor { get; }

        void SetHealth(int newHp, int newMaxHp = -1);
        void DamageHealth(int damage);
        void HealHealth(int healAmount);
        void SetFaintState(bool fainted);
        void UpdateFaintTime(float deltaTime);
        void RevivePlayer(int reviveHp = 30);
        void SetBarColors(Color normal, Color critical, Color third);
        void SetHealthSettings(int maxHp, float faintReviveTime, bool enableFaintSystem);
        void Reset();

        bool ShouldBeFainted();
        bool ShouldDie();
        PlayerHealthData GetHealthData();
        void LoadHealthData(PlayerHealthData data);
    }
}