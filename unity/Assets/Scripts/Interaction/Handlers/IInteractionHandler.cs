using Models;

namespace Interaction.Handlers
{
    public interface IInteractionHandler
    {
        /// <summary>
        /// 이 핸들러가 처리하는 상호작용 타입
        /// </summary>
        InteractionType HandlerType { get; }

        /// <summary>
        /// 진행바를 표시할지 여부
        /// </summary>
        /// <returns>진행바 표시 여부</returns>
        bool ShouldShowProgressBar();

        /// <summary>
        /// 홀드(길게 누르기)가 필요한지 여부
        /// </summary>
        /// <returns>홀드 필요 여부</returns>
        bool RequiresHold();

        /// <summary>
        /// 상호작용 완료 후 처리
        /// </summary>
        /// <param name="data">상호작용 데이터</param>
        void OnInteractionCompleted(Models.InteractionData data);

        /// <summary>
        /// 홀드 시작 처리
        /// </summary>
        /// <param name="interactable">상호작용 대상</param>
        void OnHoldStart(IInteractable interactable);

        /// <summary>
        /// 홀드 진행 처리
        /// </summary>
        /// <param name="interactable">상호작용 대상</param>
        /// <param name="progress">진행도 (0.0 ~ 1.0)</param>
        void OnHoldProgress(IInteractable interactable, float progress);

        /// <summary>
        /// 홀드 완료 처리
        /// </summary>
        /// <param name="interactable">상호작용 대상</param>
        void OnHoldComplete(IInteractable interactable);

        /// <summary>
        /// 홀드 취소 처리
        /// </summary>
        /// <param name="interactable">상호작용 대상</param>
        void OnHoldCancelled(IInteractable interactable);
    }
}