using UnityEngine;
using VContainer;

namespace Features.UI.Services
{
    public class UIAssetServiceImpl : IUIAssetService
    {
        private Features.Ggumtle.Views.GgumtleUIView _ggumtleUIView;
        private Features.Feeding.Views.FeedingUIView _feedingUIView;
        private Features.Chat.Views.ChatUIView _chatUIView;
        private Features.PlayerHealth.Views.PlayerHealthUIView _playerHealthUIView;
        private Features.Inventory.Views.InventoryUIView _inventoryUIView;
        private Features.PlayerList.Views.PlayerListUIView _playerListUIView;

        private readonly bool _enableDebugLogs = true;

        [Inject]
        public UIAssetServiceImpl(
            Features.Ggumtle.Views.GgumtleUIView ggumtleUIView,
            Features.Feeding.Views.FeedingUIView feedingUIView,
            Features.Chat.Views.ChatUIView chatUIView,
            Features.PlayerHealth.Views.PlayerHealthUIView playerHealthUIView,
            Features.Inventory.Views.InventoryUIView inventoryUIView,
            Features.PlayerList.Views.PlayerListUIView playerListUIView
        )
        {
            _ggumtleUIView = ggumtleUIView;
            _feedingUIView = feedingUIView;
            _chatUIView = chatUIView;
            _playerHealthUIView = playerHealthUIView;
            _inventoryUIView = inventoryUIView;
            _playerListUIView = playerListUIView;

            DebugLog("UIAsset Service 초기화 완료");
        }

        public void InitializeAllSprites()
        {
            DebugLog("모든 UI 스프라이트 초기화 시작");
            // 각 View에서 자체적으로 스프라이트를 관리하므로 별도 처리 불필요
        }

        public void SetGgumtleSprite(Sprite sprite)
        {
            if (sprite != null)
                _ggumtleUIView?.SetGgumtleSprite(sprite);
        }

        public void SetLightJellySprite(Sprite sprite)
        {
            if (sprite != null)
                _feedingUIView?.SetFeedingSprite(sprite);
        }

        public void SetQuickChatIconSprite(Sprite sprite)
        {
            if (sprite != null)
                _chatUIView?.SetQuickChatIconSprite(sprite);
        }

        public void SetHpIconSprite(Sprite sprite)
        {
            if (sprite != null)
                _playerHealthUIView?.SetHpIconSprite(sprite);
        }

        public void SetItemSlotBackgroundSprite(Sprite sprite)
        {
            if (sprite != null)
                _inventoryUIView?.SetSlotBackgroundSprite(sprite);
        }

        public void SetPlayerStateSprites(Sprite defaultIcon, Sprite faintIcon, Sprite deadIcon, Sprite escapeIcon)
        {
            if (defaultIcon != null && faintIcon != null && deadIcon != null && escapeIcon != null)
                _playerListUIView?.SetPlayerStateSprites(defaultIcon, faintIcon, deadIcon, escapeIcon);
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[UIAssetService] {message}");
        }
    }
}