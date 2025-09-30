using System.Collections.Generic;
using Features.Notification.Models;
using R3;

namespace Features.Notification.Services
{
    /// <summary>
    /// 알림 시스템 관리 서비스 인터페이스
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// 현재 알림 표시 상태 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsShowing { get; }

        /// <summary>
        /// 현재 표시 중인 메시지 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<string> CurrentMessage { get; }

        /// <summary>
        /// 큐에 있는 알림 개수 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<int> QueueCount { get; }

        /// <summary>
        /// 큐에 대기 중인 알림이 있는지 여부 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<bool> HasQueuedNotifications { get; }

        /// <summary>
        /// 기본 표시 시간 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<float> DefaultDisplayDuration { get; }

        /// <summary>
        /// 페이드 인 시간 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<float> FadeInDuration { get; }

        /// <summary>
        /// 페이드 아웃 시간 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<float> FadeOutDuration { get; }

        /// <summary>
        /// 알림 즉시 표시
        /// </summary>
        void ShowNotification(string message, float duration = -1f, NotificationType type = NotificationType.Info);

        /// <summary>
        /// 알림 큐에 추가
        /// </summary>
        void QueueNotification(string message, float duration = -1f, NotificationType type = NotificationType.Info);

        /// <summary>
        /// 알림 숨김
        /// </summary>
        void HideNotification(bool immediate = false);

        /// <summary>
        /// 알림 큐 처리 (다음 알림 표시)
        /// </summary>
        void ProcessNotificationQueue();

        /// <summary>
        /// 알림 큐 초기화
        /// </summary>
        void ClearNotificationQueue();

        /// <summary>
        /// 애니메이션 시간 설정
        /// </summary>
        void SetAnimationDurations(float fadeIn, float fadeOut);

        /// <summary>
        /// 기본 표시 시간 설정
        /// </summary>
        void SetDefaultDisplayDuration(float duration);

        /// <summary>
        /// 최대 큐 크기 설정
        /// </summary>
        void SetMaxQueueSize(int maxSize);

        // 미리 정의된 알림들
        /// <summary>
        /// 게임 시작 알림
        /// </summary>
        void ShowGameStartNotification();

        /// <summary>
        /// 게임 종료 알림
        /// </summary>
        void ShowGameEndNotification();

        /// <summary>
        /// 플레이어 참가 알림
        /// </summary>
        void ShowPlayerJoinedNotification(string playerName);

        /// <summary>
        /// 플레이어 퇴장 알림
        /// </summary>
        void ShowPlayerLeftNotification(string playerName);

        /// <summary>
        /// 플레이어 사망 알림
        /// </summary>
        void ShowPlayerDiedNotification(string playerName);

        /// <summary>
        /// 플레이어 탈출 알림
        /// </summary>
        void ShowPlayerEscapedNotification(string playerName);

        /// <summary>
        /// 목표 알림
        /// </summary>
        void ShowObjectiveNotification(string objective);

        /// <summary>
        /// 경고 알림
        /// </summary>
        void ShowWarningNotification(string warning);

        /// <summary>
        /// 성공 알림
        /// </summary>
        void ShowSuccessNotification(string success);

        /// <summary>
        /// 아이템 사용 불가 알림
        /// </summary>
        void ShowItemCannotBeUsedNotification(string itemName, string reason);

        // 유틸리티 메서드들
        /// <summary>
        /// 큐에 있는 모든 알림 가져오기
        /// </summary>
        List<NotificationData> GetQueuedNotifications();

        /// <summary>
        /// 다음 알림 확인 (제거하지 않음)
        /// </summary>
        NotificationData PeekNextNotification();

        /// <summary>
        /// 현재 알림 데이터 가져오기
        /// </summary>
        NotificationData GetCurrentNotification();

        /// <summary>
        /// 만료된 알림들 정리
        /// </summary>
        void CleanupExpiredNotifications();
    }
}