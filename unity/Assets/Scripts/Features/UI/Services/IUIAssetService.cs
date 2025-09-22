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
        void SetPlayerStateSprites(Sprite defaultIcon, Sprite faintIcon, Sprite deadIcon, Sprite escapeIcon);
    }
}