using Features.Player.Views;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// 플레이어 GameObject 자동 설정 스크립트
    /// 필요한 모든 컴포넌트를 자동으로 추가하고 설정
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerSetup : MonoBehaviour
    {
        [Header("Setup Settings")]
        [SerializeField]
        private bool autoSetup = true;

        [SerializeField]
        private bool setupOnAwake = true;

        void Awake()
        {
            if (setupOnAwake && autoSetup)
            {
                SetupPlayer();
            }
        }

        [ContextMenu("Setup Player Components")]
        public void SetupPlayer()
        {
            Debug.Log("[PlayerSetup] 플레이어 컴포넌트 설정 시작...");

            // 1. Player 태그 설정
            if (!gameObject.CompareTag("Player"))
            {
                gameObject.tag = "Player";
                Debug.Log("[PlayerSetup] Player 태그 설정 완료");
            }

            // 2. CharacterController 확인
            var characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                // 기본 설정
                characterController.center = new Vector3(0, 1, 0);
                characterController.height = 2;
                characterController.radius = 0.5f;
                Debug.Log("[PlayerSetup] CharacterController 추가 완료");
            }

            // 3. 입력 시스템은 MobileInputService로 자동 관리됨
            Debug.Log("[PlayerSetup] 입력 시스템: MobileInputService 자동 관리");

            // 4. PlayerGameObject 설정
            var playerGameObject = GetComponent<PlayerGameObject>();
            if (playerGameObject == null)
            {
                playerGameObject = gameObject.AddComponent<PlayerGameObject>();
                Debug.Log("[PlayerSetup] PlayerGameObject 추가 완료");
            }

            // 5. Animator 확인 (선택사항)
            var animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning(
                    "[PlayerSetup] Animator가 없습니다. 애니메이션이 작동하지 않을 수 있습니다."
                );
            }

            Debug.Log("[PlayerSetup] ===== 플레이어 설정 완료 =====");
            Debug.Log($"[PlayerSetup] 현재 컴포넌트 목록:");
            Debug.Log($"  - CharacterController: {characterController != null}");
            Debug.Log($"  - PlayerGameObject: {playerGameObject != null}");
            Debug.Log($"  - Animator: {animator != null}");
            Debug.Log($"  - Tag: {gameObject.tag}");
        }

        [ContextMenu("Check Player Status")]
        public void CheckPlayerStatus()
        {
            Debug.Log("[PlayerSetup] ===== 플레이어 상태 체크 =====");

            var playerGameObject = GetComponent<PlayerGameObject>();
            if (playerGameObject != null)
            {
                Debug.Log($"[PlayerSetup] PlayerGameObject:");
                Debug.Log($"  - canMove: {playerGameObject.canMove}");
                Debug.Log($"  - Grounded: {playerGameObject.Grounded}");
                Debug.Log($"  - MoveSpeed: {playerGameObject.MoveSpeed}");
                Debug.Log($"  - JumpHeight: {playerGameObject.JumpHeight}");
            }
        }
    }
}
