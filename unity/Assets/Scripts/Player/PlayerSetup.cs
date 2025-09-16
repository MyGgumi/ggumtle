using UnityEngine;
using MVVM.Movement;
using StarterAssets;

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
        [SerializeField] private bool autoSetup = true;
        [SerializeField] private bool setupOnAwake = true;

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

            // 3. PlayerMovementViewModel 설정 (싱글톤)
            var movementViewModel = GetComponent<PlayerMovementViewModel>();
            if (movementViewModel == null)
            {
                // 직접 컴포넌트 추가
                movementViewModel = gameObject.AddComponent<PlayerMovementViewModel>();
                Debug.Log("[PlayerSetup] PlayerMovementViewModel 컴포넌트 추가 완료");
            }

            // 싱글톤 인스턴스 확인
            var instance = PlayerMovementViewModel.Instance;
            if (instance != null && instance.gameObject == gameObject)
            {
                Debug.Log("[PlayerSetup] PlayerMovementViewModel 싱글톤 연결 완료");
            }
            else
            {
                Debug.LogError("[PlayerSetup] PlayerMovementViewModel 싱글톤 설정 실패!");
            }

            // 4. ThirdPersonController 설정
            var thirdPersonController = GetComponent<ThirdPersonController>();
            if (thirdPersonController == null)
            {
                thirdPersonController = gameObject.AddComponent<ThirdPersonController>();
                Debug.Log("[PlayerSetup] ThirdPersonController 추가 완료");
            }

            // 5. Animator 확인 (선택사항)
            var animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("[PlayerSetup] Animator가 없습니다. 애니메이션이 작동하지 않을 수 있습니다.");
            }

            Debug.Log("[PlayerSetup] ===== 플레이어 설정 완료 =====");
            Debug.Log($"[PlayerSetup] 현재 컴포넌트 목록:");
            Debug.Log($"  - CharacterController: {characterController != null}");
            Debug.Log($"  - PlayerMovementViewModel: {PlayerMovementViewModel.Instance != null}");
            Debug.Log($"  - ThirdPersonController: {thirdPersonController != null}");
            Debug.Log($"  - Animator: {animator != null}");
            Debug.Log($"  - Tag: {gameObject.tag}");
        }

        [ContextMenu("Test Jump")]
        public void TestJump()
        {
            var viewModel = PlayerMovementViewModel.Instance;
            if (viewModel != null)
            {
                Debug.Log("[PlayerSetup] 테스트 점프 시작!");
                viewModel.SetJumpInput(true);

                // 0.5초 후 점프 해제
                Invoke(nameof(ReleaseJump), 0.5f);
            }
        }

        private void ReleaseJump()
        {
            var viewModel = PlayerMovementViewModel.Instance;
            if (viewModel != null)
            {
                viewModel.SetJumpInput(false);
                Debug.Log("[PlayerSetup] 테스트 점프 종료!");
            }
        }

        [ContextMenu("Test Movement")]
        public void TestMovement()
        {
            var viewModel = PlayerMovementViewModel.Instance;
            if (viewModel != null)
            {
                Debug.Log("[PlayerSetup] 테스트 이동 시작!");
                viewModel.SetMoveInput(Vector2.up);

                // 2초 후 이동 정지
                Invoke(nameof(StopMovement), 2f);
            }
        }

        private void StopMovement()
        {
            var viewModel = PlayerMovementViewModel.Instance;
            if (viewModel != null)
            {
                viewModel.SetMoveInput(Vector2.zero);
                Debug.Log("[PlayerSetup] 테스트 이동 종료!");
            }
        }

        [ContextMenu("Check Player Status")]
        public void CheckPlayerStatus()
        {
            Debug.Log("[PlayerSetup] ===== 플레이어 상태 체크 =====");

            var viewModel = PlayerMovementViewModel.Instance;
            if (viewModel != null)
            {
                viewModel.LogCurrentState();
            }

            var thirdPerson = GetComponent<ThirdPersonController>();
            if (thirdPerson != null)
            {
                Debug.Log($"[PlayerSetup] ThirdPersonController:");
                Debug.Log($"  - canMove: {thirdPerson.canMove}");
                Debug.Log($"  - Grounded: {thirdPerson.Grounded}");
                Debug.Log($"  - MoveSpeed: {thirdPerson.MoveSpeed}");
                Debug.Log($"  - JumpHeight: {thirdPerson.JumpHeight}");
            }

            // KeyboardDebugController 확인
            var keyboardDebugController = FindObjectOfType<InputSystem.Debug.KeyboardDebugController>();
            if (keyboardDebugController != null)
            {
                Debug.Log($"[PlayerSetup] KeyboardDebugController 발견 - GameObject: {keyboardDebugController.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[PlayerSetup] KeyboardDebugController를 찾을 수 없습니다!");
            }
        }

        [ContextMenu("Enable Keyboard Debug")]
        public void EnableKeyboardDebug()
        {
            var keyboardDebugController = FindObjectOfType<InputSystem.Debug.KeyboardDebugController>();
            if (keyboardDebugController != null)
            {
                keyboardDebugController.SetKeyboardInputEnabled(true);
                keyboardDebugController.SetDebugLogsEnabled(true);
                Debug.Log("[PlayerSetup] 키보드 디버그 활성화 완료!");
            }
            else
            {
                Debug.LogError("[PlayerSetup] KeyboardDebugController를 찾을 수 없어서 활성화할 수 없습니다!");
            }
        }
    }
}