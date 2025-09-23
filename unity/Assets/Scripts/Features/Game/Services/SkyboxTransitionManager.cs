using System;
using System.Collections;
using Cinemachine;
using UnityEngine;

namespace Features.Game.Services
{
    /// <summary>
    /// 스카이박스 및 카메라 전환을 관리하는 싱글톤 매니저
    /// 로딩 씬 → 메인 씬 → 게임 시작 시 스카이박스와 카메라 전환 처리
    /// </summary>
    public class SkyboxTransitionManager : MonoBehaviour
    {
        #region Singleton

        private static SkyboxTransitionManager _instance;
        public static SkyboxTransitionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[SkyboxTransitionManager]");
                    _instance = go.AddComponent<SkyboxTransitionManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        [Header("설정")]
        [SerializeField]
        private float transitionDuration = 2f;

        [SerializeField]
        private float cameraBlendDuration = 1f; // 카메라 전환 블렌드 시간

        [SerializeField]
        private bool enableDebugLogs = true;

        [Header("카메라 설정")]
        [SerializeField]
        private Camera skyCamera; // 하늘을 보여주는 전용 카메라

        [SerializeField]
        private Camera mainCamera; // 메인 카메라 (CinemachineBrain 포함)

        [SerializeField]
        private CinemachineFreeLook playerCamera; // 플레이어를 따라가는 FreeLook 카메라

        // CinemachineBrain for smooth blending
        private CinemachineBrain _cinemachineBrain;

        // 스카이박스 상태
        private Material _loadingSkybox; // 로딩에서 사용한 skyDream
        private Material _gameSkybox; // 실제 게임 스카이박스
        private Material _defaultSkybox; // 기본 스카이박스

        // 전환 상태
        private bool _isTransitioning = false;
        private Coroutine _transitionCoroutine;
        private Coroutine _cameraTransitionCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 기본 스카이박스 저장
            _defaultSkybox = RenderSettings.skybox;

            DebugLog("SkyboxTransitionManager 초기화 완료");
        }

        #endregion

        #region Public Methods


        /// <summary>
        /// 로딩 배경에서 사용한 스카이박스 저장
        /// </summary>
        public void SetLoadingSkybox(Material skyboxMaterial)
        {
            _loadingSkybox = skyboxMaterial;
            DebugLog($"로딩 스카이박스 저장: {skyboxMaterial?.name ?? "NULL"}");
        }

        /// <summary>
        /// 게임용 스카이박스 설정
        /// </summary>
        public void SetGameSkybox(Material skyboxMaterial)
        {
            _gameSkybox = skyboxMaterial;
            DebugLog($"게임 스카이박스 설정: {skyboxMaterial?.name ?? "NULL"}");
        }

        /// <summary>
        /// 카메라 레퍼런스 설정 (런타임에서 찾기)
        /// </summary>
        public void SetupCameras()
        {
            DebugLog("카메라 찾기 시작...");

            var cameras = FindObjectsOfType<Camera>();
            DebugLog($"총 {cameras.Length}개의 Camera 발견");

            // Sky Camera 찾기
            if (skyCamera == null)
            {
                foreach (var cam in cameras)
                {
                    DebugLog($"Camera 발견: {cam.name}");
                    if (cam.name.Contains("Sky"))
                    {
                        skyCamera = cam;
                        DebugLog($"Sky Camera로 설정: {cam.name}");
                        break;
                    }
                }
            }

            // Main Camera 찾기 (CinemachineBrain 포함)
            if (mainCamera == null)
            {
                foreach (var cam in cameras)
                {
                    if (cam.name.Contains("Main"))
                    {
                        mainCamera = cam;
                        DebugLog($"Main Camera로 설정: {cam.name}");

                        // CinemachineBrain 찾기
                        _cinemachineBrain = cam.GetComponent<CinemachineBrain>();
                        if (_cinemachineBrain != null)
                        {
                            DebugLog($"CinemachineBrain 발견: {_cinemachineBrain.name}");
                            // 기본 블렌드 시간 설정
                            _cinemachineBrain.m_DefaultBlend.m_Time = cameraBlendDuration;
                        }
                        break;
                    }
                }
            }

            // Player Camera 찾기 (Cinemachine FreeLook Camera)
            if (playerCamera == null)
            {
                var freeLookCameras = FindObjectsOfType<CinemachineFreeLook>();
                DebugLog($"총 {freeLookCameras.Length}개의 FreeLook Camera 발견");

                foreach (var freelook in freeLookCameras)
                {
                    DebugLog($"FreeLook Camera 발견: {freelook.name}");
                    if (
                        freelook.name.Contains("Player")
                        || freelook.name.Contains("FreeLook")
                        || freelook.name.Contains("Follow")
                        || freelook.name.Contains("CM")
                    )
                    {
                        playerCamera = freelook;
                        DebugLog($"Player Camera로 설정: {freelook.name}");
                        break;
                    }
                }

                // 첫 번째 FreeLook 카메라를 Player Camera로 사용 (폴백)
                if (playerCamera == null && freeLookCameras.Length > 0)
                {
                    playerCamera = freeLookCameras[0];
                    DebugLog(
                        $"첫 번째 FreeLook 카메라를 Player Camera로 사용: {playerCamera.name}"
                    );
                }
            }

            DebugLog(
                $"최종 카메라 설정 - Sky: {skyCamera?.name ?? "NULL"}, Main: {mainCamera?.name ?? "NULL"}, Player: {playerCamera?.name ?? "NULL"}"
            );
        }

        /// <summary>
        /// 메인 씬에서 Sky Camera 활성화 (기본 스카이박스 유지)
        /// </summary>
        public void ApplyLoadingSkyboxInMainScene()
        {
            DebugLog("메인 씬에서 기본 스카이박스 유지 - 별도 변경 없음");

            // Sky Camera로 전환 (하늘만 보여주기)
            SetSkyViewMode();
        }

        /// <summary>
        /// Sky View 모드 설정 (Sky Camera 활성화, Main Camera 비활성화)
        /// </summary>
        public void SetSkyViewMode()
        {
            SetupCameras();

            // Sky Camera 활성화
            if (skyCamera != null)
            {
                DebugLog($"Sky Camera 활성화: {skyCamera.name}");
                skyCamera.enabled = true;
            }
            else
            {
                DebugLog("Sky Camera가 없습니다!");
            }

            // Main Camera 비활성화
            if (mainCamera != null)
            {
                DebugLog($"Main Camera 비활성화: {mainCamera.name}");
                mainCamera.enabled = false;
            }

            // Player Camera 비활성화
            if (playerCamera != null)
            {
                playerCamera.Priority = 0;
                DebugLog($"Player Camera Priority 0 설정: {playerCamera.name}");
            }

            DebugLog("Sky View 모드 활성화 완료");
        }

        /// <summary>
        /// Player View 모드 설정 (Main Camera 활성화, Sky Camera 비활성화)
        /// </summary>
        public void SetPlayerViewMode()
        {
            SetPlayerViewModeSmooth();
        }

        /// <summary>
        /// 부드러운 Player View 모드 전환
        /// </summary>
        public void SetPlayerViewModeSmooth()
        {
            if (_cameraTransitionCoroutine != null)
            {
                StopCoroutine(_cameraTransitionCoroutine);
            }

            _cameraTransitionCoroutine = StartCoroutine(SmoothCameraTransitionCoroutine());
        }

        /// <summary>
        /// 즉시 Player View 모드 설정 (깜빡거림 방식)
        /// </summary>
        public void SetPlayerViewModeImmediate()
        {
            SetupCameras();

            // Sky Camera 비활성화
            if (skyCamera != null)
            {
                DebugLog($"Sky Camera 비활성화: {skyCamera.name}");
                skyCamera.enabled = false;
            }

            // Main Camera 활성화
            if (mainCamera != null)
            {
                DebugLog($"Main Camera 활성화: {mainCamera.name}");
                mainCamera.enabled = true;
            }

            // Player Camera 활성화
            if (playerCamera != null)
            {
                playerCamera.Priority = 10;
                DebugLog($"Player Camera Priority 10 설정: {playerCamera.name}");
            }

            DebugLog("Player View 모드 활성화 완료");
        }

        /// <summary>
        /// 게임 시작 시 게임 스카이박스 및 플레이어 카메라로 전환
        /// </summary>
        public void TransitionToGameSkybox()
        {
            if (_isTransitioning)
            {
                DebugLog("이미 전환 중입니다.");
                return;
            }

            if (_gameSkybox == null)
            {
                DebugLog("게임 스카이박스가 설정되지 않았습니다. 기본 스카이박스 사용");
                _gameSkybox = _defaultSkybox;
            }

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            _transitionCoroutine = StartCoroutine(TransitionToGameCoroutine(_gameSkybox));
        }

        /// <summary>
        /// 즉시 스카이박스 변경 (전환 효과 없음)
        /// </summary>
        public void SetSkyboxImmediate(Material skyboxMaterial)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
                _isTransitioning = false;
            }

            RenderSettings.skybox = skyboxMaterial;
            DynamicGI.UpdateEnvironment();
            DebugLog($"즉시 스카이박스 변경: {skyboxMaterial?.name ?? "NULL"}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 부드러운 카메라 전환 코루틴
        /// </summary>
        private IEnumerator SmoothCameraTransitionCoroutine()
        {
            SetupCameras();

            DebugLog("부드러운 카메라 전환 시작");

            // Sky Camera와 Main Camera가 모두 있는지 확인
            if (skyCamera == null || mainCamera == null)
            {
                DebugLog("카메라가 설정되지 않아 즉시 전환으로 폴백");
                SetPlayerViewModeImmediate();
                yield break;
            }

            // 1단계: 두 카메라 모두 활성화하고 블렌딩 준비
            skyCamera.enabled = true;
            mainCamera.enabled = true;

            // 2단계: Cinemachine Priority를 이용한 부드러운 전환
            if (playerCamera != null && _cinemachineBrain != null)
            {
                DebugLog("Cinemachine Priority 기반 부드러운 전환 시작");

                // Player Camera Priority를 높여서 부드럽게 전환
                playerCamera.Priority = 10;

                // CinemachineBrain이 블렌딩을 완료할 때까지 대기
                yield return new WaitForSeconds(cameraBlendDuration);

                DebugLog("Cinemachine 블렌딩 완료");
            }
            else
            {
                // 3단계: 수동 알파 블렌딩 (폴백)
                DebugLog("수동 알파 블렌딩으로 카메라 전환");

                float elapsed = 0f;
                while (elapsed < cameraBlendDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / cameraBlendDuration;

                    // Sky Camera 알파를 점진적으로 줄임 (폴백 방법)
                    if (skyCamera != null)
                    {
                        // 주의: Camera에는 직접적인 알파 설정이 없으므로
                        // 화면 전환 효과를 위해 환경 강도 조절
                        // 이것은 임시 해결책이며, 실제로는 CanvasGroup이나 UI 오버레이를 사용하는 것이 좋음
                    }

                    yield return null;
                }
            }

            // 4단계: Sky Camera 완전히 비활성화
            if (skyCamera != null)
            {
                skyCamera.enabled = false;
                DebugLog($"Sky Camera 비활성화: {skyCamera.name}");
            }

            DebugLog("부드러운 카메라 전환 완료");
            _cameraTransitionCoroutine = null;
        }

        private IEnumerator TransitionToGameCoroutine(Material targetSkybox)
        {
            _isTransitioning = true;
            DebugLog($"게임 전환 시작 → 스카이박스: {targetSkybox?.name ?? "NULL"}");

            // 현재 스카이박스에서 검은색으로 페이드 아웃
            float elapsed = 0f;
            float halfDuration = transitionDuration * 0.5f;

            // 페이드 아웃 단계
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;

                // 환경 강도를 줄여서 어둡게 만들기
                RenderSettings.ambientIntensity = Mathf.Lerp(1f, 0.3f, t);

                yield return null;
            }

            // 스카이박스 교체
            RenderSettings.skybox = targetSkybox;
            DynamicGI.UpdateEnvironment();

            // 카메라를 플레이어 뷰로 부드럽게 전환
            SetPlayerViewModeSmooth();

            // 페이드 인 단계
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;

                // 환경 강도를 다시 복구
                RenderSettings.ambientIntensity = Mathf.Lerp(0.3f, 1f, t);

                yield return null;
            }

            // 최종 정리
            RenderSettings.ambientIntensity = 1f;
            _isTransitioning = false;
            _transitionCoroutine = null;

            DebugLog("게임 전환 완료 - 스카이박스 및 카메라 전환");
        }

        private void DebugLog(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[SkyboxTransitionManager] {message}");
        }

        #endregion

        private void OnDestroy()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            if (_cameraTransitionCoroutine != null)
            {
                StopCoroutine(_cameraTransitionCoroutine);
                _cameraTransitionCoroutine = null;
            }

            DebugLog("SkyboxTransitionManager 파괴됨");
        }
    }
}
