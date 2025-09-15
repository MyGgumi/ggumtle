using System;
using System.Collections.Generic;
using UnityEngine;
using Models;

public enum PlayerRole
{
    Mongging, // 몽깅이 (일반 플레이어) - 기존
    Mongdung, // 몽둥이 (술래)
}

[System.Serializable]
public class UniversalHUDData
{
    [Header("Player Role")]
    public PlayerRole playerRole = PlayerRole.Mongging;

    [Header("Game Status")]
    public TimeSpan remainingTime;
    public string statusMessage;
    public int currentLight;
    public int maxLight;
    public string bannerMessage;
    public bool showBanner;

    [Header("Player Data")]
    public List<PlayerData> players = new List<PlayerData>();

    [Header("Progress")]
    public ProgressData ggumtleProgress;
    public ProgressData digProgress;
    public HealthData playerHealth;

    [Header("Inventory Data")]
    public InventoryData inventoryData;

    [Header("Player State")]
    public bool isFainted = false;
    public float faintTimeRemaining = 0f;
    public int currentHP = 100;
    public int maxHP = 100;

    [Header("Interaction State")]
    public bool showInteractionUI = false;
    public string interactionText = "";
    public float interactionProgress = 0f;
    public bool isInteracting = false;
    public Models.InteractionType currentInteractionType = Models.InteractionType.Dig;

    [Header("UI State")]
    public bool showInteractionButton;
    public bool showDigUI;
    public bool showChatIcon;
}

[System.Serializable]
public class PlayerData
{
    public int playerId;
    public string nickname;
    public string colorTheme; // "yellow", "mint", "pink"
    public string status = "default"; // "default", "dead", "escape", "faint"
    public Sprite avatarSprite;
    public bool isOnline;
    public bool isHost;
}

[System.Serializable]
public class ProgressData
{
    public float currentValue; // 0.0 ~ 1.0
    public int level; // 1, 2, 3 for ggumtle progress
    public string displayText;
}

[System.Serializable]
public class HealthData
{
    public int currentHP;
    public int maxHP;
    public int heartCharges;
}

[System.Serializable]
public class InventoryData
{
    public InventorySlot slot1;
    public InventorySlot slot2;
    public InventorySlot slot3;

    public InventoryData()
    {
        slot1 = new InventorySlot
        {
            slotNumber = 1,
            itemCount = 0,
            maxCount = 3,
        };
        slot2 = new InventorySlot
        {
            slotNumber = 2,
            itemCount = 0,
            maxCount = 3,
        };
        slot3 = new InventorySlot
        {
            slotNumber = 3,
            itemCount = 0,
            maxCount = 3,
        };
    }
}

[System.Serializable]
public class InventorySlot
{
    public int slotNumber;
    public int itemCount;
    public int maxCount = 3;
    public string itemType = ""; // 아이템 종류 (나중에 확장 가능)

    public bool CanUse() => itemCount > 0;

    public bool CanAdd() => itemCount < maxCount;

    public bool UseItem()
    {
        if (CanUse())
        {
            itemCount--;
            return true;
        }
        return false;
    }

    public bool AddItem(int amount = 1)
    {
        if (itemCount + amount <= maxCount)
        {
            itemCount += amount;
            return true;
        }
        return false;
    }
}

// 상호작용 타입은 Models.InteractionType을 사용
