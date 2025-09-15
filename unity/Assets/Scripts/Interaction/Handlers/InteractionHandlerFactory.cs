using System;
using Models;

namespace Interaction.Handlers
{
    public static class InteractionHandlerFactory
    {
        /// <summary>
        /// 상호작용 타입에 따라 적절한 핸들러를 반환
        /// </summary>
        /// <param name="type">상호작용 타입</param>
        /// <returns>해당 타입의 핸들러</returns>
        public static IInteractionHandler GetHandler(InteractionType type)
        {
            return type switch
            {
                InteractionType.Chest => new ChestInteractionHandler(),
                InteractionType.Dig => new DigInteractionHandler(),
                InteractionType.Feeding => new FeedingInteractionHandler(),
                InteractionType.Custom => new ExitInteractionHandler(), // 탈출은 Custom 타입 사용
                InteractionType.Revive => new DigInteractionHandler(), // Revive도 진행바 필요하므로 Dig와 동일하게 처리
                InteractionType.Faint => new ExitInteractionHandler(), // Faint는 즉시 처리
                _ => throw new NotSupportedException($"Handler not implemented for InteractionType: {type}")
            };
        }

        /// <summary>
        /// 특정 핸들러 타입의 인스턴스를 반환
        /// </summary>
        /// <typeparam name="T">핸들러 타입</typeparam>
        /// <returns>핸들러 인스턴스</returns>
        public static T GetHandler<T>() where T : IInteractionHandler, new()
        {
            return new T();
        }
    }
}