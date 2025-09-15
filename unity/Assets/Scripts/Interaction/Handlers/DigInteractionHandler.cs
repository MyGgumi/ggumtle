using Models;
using UnityEngine;

namespace Interaction.Handlers
{
    public class DigInteractionHandler : IInteractionHandler
    {
        public InteractionType HandlerType => InteractionType.Dig;

        public bool ShouldShowProgressBar()
        {
            // 땅파기는 시간이 걸리므로 진행바 필요
            return true;
        }

        public bool RequiresHold()
        {
            // 땅파기는 홀드가 필요
            return true;
        }

        public void OnInteractionCompleted(Models.InteractionData data)
        {
            // 땅파기 완료 알림 표시
            Debug.Log("[DigInteractionHandler] 땅파기 완료!");
        }

        public void OnHoldStart(IInteractable interactable)
        {
            // 꿈틀이에게 홀드 시작 전달
            interactable.OnHoldStart();
            Debug.Log("[DigInteractionHandler] 홀드 시작");
        }

        public void OnHoldProgress(IInteractable interactable, float progress)
        {
            // 꿈틀이에게 홀드 진행도 전달
            interactable.OnHoldProgress(progress);
        }

        public void OnHoldComplete(IInteractable interactable)
        {
            // 꿈틀이에게 홀드 완료 전달
            interactable.OnHoldComplete();
            Debug.Log("[DigInteractionHandler] 홀드 완료");
        }

        public void OnHoldCancelled(IInteractable interactable)
        {
            // 꿈틀이에게 홀드 취소 전달
            interactable.OnHoldCancelled();
            Debug.Log("[DigInteractionHandler] 홀드 취소");
        }
    }
}