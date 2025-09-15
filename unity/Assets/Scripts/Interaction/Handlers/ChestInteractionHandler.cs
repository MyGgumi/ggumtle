using Models;
using ViewModels.UI;

namespace Interaction.Handlers
{
    public class ChestInteractionHandler : IInteractionHandler
    {
        public InteractionType HandlerType => InteractionType.Chest;

        public bool ShouldShowProgressBar()
        {
            // 상자는 즉시 열리므로 진행바 불필요
            return false;
        }

        public bool RequiresHold()
        {
            // 상자는 한 번 클릭으로 열기/닫기
            return false;
        }

        public void OnInteractionCompleted(Models.InteractionData data)
        {
            // 상자가 열리면 ChestView가 자동으로 표시됨
            // 별도 처리 불필요 (ChestViewModel에서 이미 처리)
        }

        public void OnHoldStart(IInteractable interactable)
        {
            // 상자는 홀드가 필요하지 않으므로 빈 구현
        }

        public void OnHoldProgress(IInteractable interactable, float progress)
        {
            // 상자는 홀드가 필요하지 않으므로 빈 구현
        }

        public void OnHoldComplete(IInteractable interactable)
        {
            // 상자는 홀드가 필요하지 않으므로 빈 구현
        }

        public void OnHoldCancelled(IInteractable interactable)
        {
            // 상자는 홀드가 필요하지 않으므로 빈 구현
        }
    }
}