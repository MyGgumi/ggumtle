using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerListManager : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement _root;
    private VisualElement _topRightIcons;
    
    [Header("Player Icons")]
    private VisualElement[] _playerIcons = new VisualElement[4];
    private Label[] _playerNames = new Label[4];
    private VisualElement[] _playerAreas = new VisualElement[4];
    
    [Header("Sprites")]
    public Sprite iconMongingDefault;
    public Sprite iconMongingFaint;
    public Sprite iconMongingDead;
    public Sprite iconMongingEscape;
    
    [Header("Current State")]
    public Dictionary<int, PlayerData> playerDataCache = new Dictionary<int, PlayerData>();
    
    // Static 이벤트는 HUDEvents로 이동됨
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        CacheUIElements();
        SetupDefaultPlayers();
    }
    
    private void CacheUIElements()
    {
        _topRightIcons = _root.Q<VisualElement>("topRightIcons");
        
        // 플레이어 아이콘과 이름 캐싱
        for (int i = 0; i < 4; i++)
        {
            int playerNum = i + 1;
            _playerIcons[i] = _root.Q<VisualElement>($"icon{playerNum}");
            _playerNames[i] = _root.Q<Label>($"player{playerNum}Name");
            _playerAreas[i] = _root.Q<VisualElement>($"player{playerNum}Area");
            
            // UXML의 하드코딩된 플레이어 이름 초기화
            if (_playerNames[i] != null)
            {
                _playerNames[i].text = "";
            }
        }
    }
    
    private void SetupDefaultPlayers()
    {
        // 기본 플레이어 아이콘 설정
        for (int i = 0; i < 4; i++)
        {
            if (_playerIcons[i] != null && iconMongingDefault != null)
            {
                _playerIcons[i].style.backgroundImage = new StyleBackground(iconMongingDefault);
            }
        }
    }
    
    public void UpdatePlayer(int playerId, string nickname, string colorTheme, string status = "default", bool isOnline = true, bool isHost = false)
    {
        if (playerId < 1 || playerId > 4) return;
        
        var playerData = new PlayerData
        {
            playerId = playerId,
            nickname = nickname,
            colorTheme = colorTheme,
            status = status,
            avatarSprite = GetStatusSprite(status),
            isOnline = isOnline,
            isHost = isHost
        };
        
        playerDataCache[playerId] = playerData;
        
        // UI 업데이트 (기존 UniversalHUDController 로직)
        var icon = GetPlayerIcon(playerId);
        var nameLabel = GetPlayerNameLabel(playerId);
        
        if (icon != null)
        {
            SetPlayerColor(playerId, colorTheme);
            ApplyPlayerStateClasses(icon, playerData);
            if (playerData.avatarSprite != null)
            {
                icon.style.backgroundImage = new StyleBackground(playerData.avatarSprite);
            }
        }
        
        if (nameLabel != null)
        {
            nameLabel.text = nickname;
        }
        
        HUDEvents.TriggerPlayerStatusChange(playerId, status, nickname);
        HUDEvents.TriggerPlayerConnectionChange(playerId, isOnline);
        
        Debug.Log($"[PlayerListManager] 플레이어 {playerId} 업데이트: {nickname} ({status})");
    }
    
    private VisualElement GetPlayerIcon(int playerIndex)
    {
        if (playerIndex < 1 || playerIndex > 4) return null;
        return _playerIcons[playerIndex - 1];
    }
    
    private Label GetPlayerNameLabel(int playerIndex)
    {
        if (playerIndex < 1 || playerIndex > 4) return null;
        return _playerNames[playerIndex - 1];
    }
    
    private void ApplyPlayerStateClasses(VisualElement icon, PlayerData player)
    {
        // 기존 상태 클래스 제거
        icon.RemoveFromClassList("player-online");
        icon.RemoveFromClassList("player-offline");
        
        // 새 상태 클래스 적용
        if (player.isOnline)
            icon.AddToClassList("player-online");
        else
            icon.AddToClassList("player-offline");
    }
    
    public void UpdatePlayerFromData(List<PlayerData> players)
    {
        for (int i = 0; i < players.Count && i < 4; i++)
        {
            var player = players[i];
            UpdatePlayerUI(player.playerId, player);
            playerDataCache[player.playerId] = player;
        }
    }
    
    private void UpdatePlayerUI(int playerId, PlayerData playerData)
    {
        if (playerId < 1 || playerId > 4) return;
        if (_root == null)
        {
            Debug.LogError("[PlayerListManager] _root가 null입니다. Initialize가 호출되지 않았습니다.");
            return;
        }
        
        int index = playerId - 1;
        
        // 닉네임 직접 교체
        if (_playerNames[index] != null)
        {
            _playerNames[index].text = playerData.nickname;
        }
        
        // 아이콘 업데이트 (기존 요소 완전히 초기화 후 새로 설정)
        if (_playerIcons[index] != null)
        {
            // 모든 기존 스타일 완전히 초기화
            _playerIcons[index].style.backgroundImage = StyleKeyword.None;
            _playerIcons[index].style.unityBackgroundImageTintColor = StyleKeyword.None;
            _playerIcons[index].style.backgroundColor = StyleKeyword.None;
            _playerIcons[index].ClearClassList(); // 모든 CSS 클래스 제거
            
            // 새 스프라이트 설정
            if (playerData.avatarSprite != null)
            {
                _playerIcons[index].style.backgroundImage = new StyleBackground(playerData.avatarSprite);
                _playerIcons[index].style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                _playerIcons[index].style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                _playerIcons[index].style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                _playerIcons[index].style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            }
            
            // 색상 적용 (기존 색상 제거 후)
            SetPlayerColor(playerId, playerData.colorTheme);
        }
    }
    
    
    public void SetPlayerColor(int playerIndex, string colorType)
    {
        if (playerIndex < 1 || playerIndex > 4) return;
        
        VisualElement playerIcon = _playerIcons[playerIndex - 1];
        
        if (playerIcon != null)
        {
            Color color = colorType switch
            {
                "yellow" => new Color(1f, 0.78f, 0.31f, 1f),
                "mint" => new Color(0.31f, 1f, 0.78f, 1f),
                "pink" => new Color(1f, 0.59f, 0.78f, 1f),
                "blue" => new Color(0.31f, 0.78f, 1f, 1f),
                _ => Color.white
            };
            
            playerIcon.style.unityBackgroundImageTintColor = color;
        }
    }
    
    public void SetPlayerStatus(int playerId, string status)
    {
        if (playerDataCache.ContainsKey(playerId))
        {
            var playerData = playerDataCache[playerId];
            playerData.status = status;
            playerData.avatarSprite = GetStatusSprite(status);
            
            UpdatePlayerUI(playerId, playerData);
        }
    }
    
    public void SetPlayerOnlineStatus(int playerId, bool isOnline)
    {
        if (playerDataCache.ContainsKey(playerId))
        {
            var playerData = playerDataCache[playerId];
            playerData.isOnline = isOnline;
            
            UpdatePlayerUI(playerId, playerData);
        }
    }
    
    public void SetPlayerListVisibility(bool visible)
    {
        if (_topRightIcons != null)
        {
            _topRightIcons.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
    
    public void HighlightPlayer(int playerId, bool highlight, float duration = 1f)
    {
        if (playerId < 1 || playerId > 4) return;
        
        var icon = _playerIcons[playerId - 1];
        if (icon != null)
        {
            if (highlight)
            {
                icon.AddToClassList("player-highlighted");
                // 하이라이트 자동 해제
                StartCoroutine(RemoveHighlightAfterDelay(icon, duration));
            }
            else
            {
                icon.RemoveFromClassList("player-highlighted");
            }
        }
    }
    
    private System.Collections.IEnumerator RemoveHighlightAfterDelay(VisualElement icon, float delay)
    {
        yield return new WaitForSeconds(delay);
        icon.RemoveFromClassList("player-highlighted");
    }
    
    
    private Sprite GetStatusSprite(string status)
    {
        return status switch
        {
            "default" => iconMongingDefault,
            "dead" => iconMongingDead,
            "escape" => iconMongingEscape,
            "faint" => iconMongingFaint,
            _ => iconMongingDefault
        };
    }
    
    public void SetSprites(Sprite defaultIcon, Sprite faintIcon, Sprite deadIcon, Sprite escapeIcon)
    {
        iconMongingDefault = defaultIcon;
        iconMongingFaint = faintIcon;
        iconMongingDead = deadIcon;
        iconMongingEscape = escapeIcon;
        
        // 현재 플레이어들의 스프라이트 다시 적용
        foreach (var kvp in playerDataCache)
        {
            var playerData = kvp.Value;
            playerData.avatarSprite = GetStatusSprite(playerData.status);
            UpdatePlayerUI(playerData.playerId, playerData);
        }
    }
    
    public PlayerData GetPlayer(int playerId)
    {
        return playerDataCache.ContainsKey(playerId) ? playerDataCache[playerId] : null;
    }
    
    public List<PlayerData> GetAllPlayers()
    {
        return new List<PlayerData>(playerDataCache.Values);
    }
    
    public int GetOnlinePlayerCount()
    {
        int count = 0;
        foreach (var player in playerDataCache.Values)
        {
            if (player.isOnline) count++;
        }
        return count;
    }
    
    public void ClearAllPlayers()
    {
        playerDataCache.Clear();
        
        // UI 초기화
        for (int i = 0; i < 4; i++)
        {
            if (_playerNames[i] != null)
                _playerNames[i].text = $"플레이어{i + 1}";
                
            if (_playerIcons[i] != null && iconMongingDefault != null)
                _playerIcons[i].style.backgroundImage = new StyleBackground(iconMongingDefault);
        }
        
        Debug.Log("[PlayerListManager] 모든 플레이어 데이터 초기화");
    }
}