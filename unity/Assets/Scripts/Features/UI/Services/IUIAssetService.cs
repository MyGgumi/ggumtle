using UnityEngine;

namespace Features.UI.Services
{
    public interface IUIAssetService
    {
        void InitializeAllSprites();
        void SetGgumtleSprite(Sprite sprite);
        void SetLightJellySprite(Sprite sprite);
        void SetQuickChatIconSprite(Sprite sprite);
        void SetHpIconSprite(Sprite sprite);
        void SetItemSlotBackgroundSprite(Sprite sprite);
        // SetPlayerStateSprites 제거 - PlayerListUIView에서 Inspector로 직접 관리
    }
}