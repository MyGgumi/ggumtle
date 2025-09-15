using UnityEngine;
using UnityEngine.UIElements;

namespace InputSystem.Core
{
    /// <summary>
    /// 모든 입력 레이어가 구현해야 하는 기본 인터페이스
    /// </summary>
    public interface IInputLayer
    {
        /// <summary>
        /// 레이어 이름 (디버깅용)
        /// </summary>
        string LayerName { get; }

        /// <summary>
        /// 레이어 우선순위 (높을수록 우선)
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// 레이어 활성화 상태
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// UI 루트 요소로 초기화
        /// </summary>
        void Initialize(VisualElement root);

        /// <summary>
        /// 레이어 활성화
        /// </summary>
        void Enable();

        /// <summary>
        /// 레이어 비활성화
        /// </summary>
        void Disable();

        /// <summary>
        /// 모든 입력 상태 리셋
        /// </summary>
        void ResetInput();

        /// <summary>
        /// 리소스 정리
        /// </summary>
        void Cleanup();
    }
}