using Features.PlayerHealth.Models;

namespace Features.PlayerHealth.Messages
{
    public readonly struct HealthChangedMessage
    {
        public readonly int currentHp;
        public readonly int maxHp;
        public readonly float healthPercentage;
        public readonly PlayerState state;
        public readonly bool isDamage;

        public HealthChangedMessage(int currentHp, int maxHp, float healthPercentage, PlayerState state, bool isDamage)
        {
            this.currentHp = currentHp;
            this.maxHp = maxHp;
            this.healthPercentage = healthPercentage;
            this.state = state;
            this.isDamage = isDamage;
        }
    }

    public readonly struct DamageReceivedMessage
    {
        public readonly int damage;
        public readonly int currentHp;
        public readonly int remainingHp;
        public readonly PlayerState newState;

        public DamageReceivedMessage(int damage, int currentHp, int remainingHp, PlayerState newState)
        {
            this.damage = damage;
            this.currentHp = currentHp;
            this.remainingHp = remainingHp;
            this.newState = newState;
        }
    }

    public readonly struct HealReceivedMessage
    {
        public readonly int healAmount;
        public readonly int previousHp;
        public readonly int currentHp;
        public readonly PlayerState newState;

        public HealReceivedMessage(int healAmount, int previousHp, int currentHp, PlayerState newState)
        {
            this.healAmount = healAmount;
            this.previousHp = previousHp;
            this.currentHp = currentHp;
            this.newState = newState;
        }
    }

    public readonly struct PlayerFaintedMessage
    {
        public readonly float faintDuration;
        public readonly PlayerState previousState;

        public PlayerFaintedMessage(float faintDuration, PlayerState previousState)
        {
            this.faintDuration = faintDuration;
            this.previousState = previousState;
        }
    }

    public readonly struct PlayerRevivedMessage
    {
        public readonly int reviveHp;
        public readonly PlayerState newState;

        public PlayerRevivedMessage(int reviveHp, PlayerState newState)
        {
            this.reviveHp = reviveHp;
            this.newState = newState;
        }
    }

    public readonly struct PlayerDiedMessage
    {
        public readonly PlayerState previousState;
        public readonly bool wasFromFaint;

        public PlayerDiedMessage(PlayerState previousState, bool wasFromFaint)
        {
            this.previousState = previousState;
            this.wasFromFaint = wasFromFaint;
        }
    }

    public readonly struct FaintTimeUpdatedMessage
    {
        public readonly float remainingTime;
        public readonly float totalTime;
        public readonly bool shouldDie;

        public FaintTimeUpdatedMessage(float remainingTime, float totalTime, bool shouldDie)
        {
            this.remainingTime = remainingTime;
            this.totalTime = totalTime;
            this.shouldDie = shouldDie;
        }
    }

    public readonly struct HealthBarUpdatedMessage
    {
        public readonly HealthBarData bar1;
        public readonly HealthBarData bar2;
        public readonly HealthBarData bar3;

        public HealthBarUpdatedMessage(HealthBarData bar1, HealthBarData bar2, HealthBarData bar3)
        {
            this.bar1 = bar1;
            this.bar2 = bar2;
            this.bar3 = bar3;
        }
    }

    public readonly struct HealthStateChangedMessage
    {
        public readonly PlayerState previousState;
        public readonly PlayerState currentState;
        public readonly int currentHp;
        public readonly float healthPercentage;

        public HealthStateChangedMessage(PlayerState previousState, PlayerState currentState, int currentHp, float healthPercentage)
        {
            this.previousState = previousState;
            this.currentState = currentState;
            this.currentHp = currentHp;
            this.healthPercentage = healthPercentage;
        }
    }

    public readonly struct HealthSettingsChangedMessage
    {
        public readonly int maxHp;
        public readonly float faintReviveTime;
        public readonly bool enableFaintSystem;

        public HealthSettingsChangedMessage(int maxHp, float faintReviveTime, bool enableFaintSystem)
        {
            this.maxHp = maxHp;
            this.faintReviveTime = faintReviveTime;
            this.enableFaintSystem = enableFaintSystem;
        }
    }
}