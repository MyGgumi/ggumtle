using Models;
using UnityEngine;

namespace Interaction.Handlers
{
    public class ExitInteractionHandler : IInteractionHandler
    {
        public InteractionType HandlerType => InteractionType.Custom; // 탈출은 Custom 타입 사용

        public bool ShouldShowProgressBar()
        {
            // 탈출은 즉시 실행되므로 진행바 불필요
            return false;
        }

        public bool RequiresHold()
        {
            // 탈출은 한 번 클릭으로 즉시 실행
            return false;
        }

        public void OnInteractionCompleted(Models.InteractionData data)
        {
            // 탈출 성공 알림 표시
            Debug.Log("[ExitInteractionHandler] 탈출 성공!");
        }

        public void OnHoldStart(IInteractable interactable)
        {
            // 탈출은 홀드가 필요하지 않으므로 빈 구현
        }

        public void OnHoldProgress(IInteractable interactable, float progress)
        {
            // 탈출은 홀드가 필요하지 않으므로 빈 구현
        }

        public void OnHoldComplete(IInteractable interactable)
        {
            // 탈출은 홀드가 필요하지 않으므로 빈 구현
        }

        public void OnHoldCancelled(IInteractable interactable)
        {
            // 탈출은 홀드가 필요하지 않으므로 빈 구현
        }
    }
}