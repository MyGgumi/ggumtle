using System;
using System.Collections;
using Features.Game.Services;
using Features.Scenes.Loading.Managers;
using Features.Scenes.Loading.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

[System.Serializable]
public class LoadingPhase
{
    public string phaseName = "낮";
    public string bodyText = "잠시만 기다려 주세요...";
    public Sprite sunOrMoonSprite; // 해/달/별 이미지
    public Sprite cloudSprite; // 구름 이미지 (필요시)
}

public class LoadingBackgroundController : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField]
    private bool enableDebugLogs = false;
    [Header("UI Document")]
    [SerializeField]
    private UIDocument uiDocument;

    [Header("전환 설정")]
    [SerializeField]
    private bool useProgressBasedTransition = true; // 진행률 기반 전환 사용

    [Header("고정 텍스트")]
    [SerializeField]
    private string titleText = "꿈속으로";

    [Header("로딩 단계 설정")]
    [SerializeField]
    private LoadingPhase[] loadingPhases;

    private VisualElement root;
    private VisualElement sunElement;
    private VisualElement cloudElement;
    private Label titleLabel;
    private Label bodyLabel;

    private int currentPhaseIndex = 0;
    private Coroutine transitionCoroutine;
    private StyleBackground[] cachedBackgrounds; // 이미지 캐싱

    // 이벤트 구독용 플래그
    private bool _isSubscribedToEvents = false;
    private bool _shouldChangeSkybox = true; // 스카이박스 변경 허용 플래그

    private void Awake()
    {
        // 이미지를 미리 StyleBackground로 변환하여 캐싱
        PreloadImages();

        if (enableDebugLogs) Debug.Log("[LoadingBackgroundController] UI 이미지 캐싱 완료");
    }

    private void PreloadImages()
    {
        if (loadingPhases != null && loadingPhases.Length > 0)
        {
            cachedBackgrounds = new StyleBackground[loadingPhases.Length];
            for (int i = 0; i < loadingPhases.Length; i++)
            {
                if (loadingPhases[i].sunOrMoonSprite != null)
                {
                    cachedBackgrounds[i] = new StyleBackground(loadingPhases[i].sunOrMoonSprite);
                }
            }
            if (enableDebugLogs) Debug.Log(
                $"[LoadingBackgroundController] {cachedBackgrounds.Length}개 이미지 프리로드 완료"
            );
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            InitializeUI();

            // 진행률 기반 전환 사용 시에는 자동 전환하지 않음
            if (!useProgressBasedTransition)
            {
                // 시간 기반 전환 (기존 방식)
                if (transitionCoroutine != null)
                {
                    StopCoroutine(transitionCoroutine);
                }
                transitionCoroutine = StartCoroutine(PhaseTransitionLoop());
            }
            else
            {
                // LoadingSceneManager 이벤트 구독
                SubscribeToLoadingSceneManager();
                if (enableDebugLogs) Debug.Log("[LoadingBackgroundController] 진행률 기반 전환 모드 활성화");
            }
        }
    }

    private void OnDisable()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        // LoadingSceneManager 이벤트 구독 해제
        UnsubscribeFromLoadingSceneManager();
    }

    private void InitializeUI()
    {
        root = uiDocument.rootVisualElement;

        // 요소들 찾기
        sunElement = root.Q<VisualElement>("Sun");
        cloudElement = root.Q<VisualElement>("Cloud");
        titleLabel = root.Q<Label>("TitleText");
        bodyLabel = root.Q<Label>("BodyText");

        // 초기 설정
        if (titleLabel != null)
        {
            titleLabel.text = titleText; // 고정 타이틀
        }

        // 해/달 요소 초기화 (스프라이트 없을 때 보이지 않게)
        if (sunElement != null)
        {
            sunElement.style.display = DisplayStyle.Flex;
            sunElement.style.backgroundColor = StyleKeyword.Null;
        }

        // 첫 번째 단계 즉시 적용 (동기화)
        if (loadingPhases.Length > 0)
        {
            currentPhaseIndex = 0;

            if (bodyLabel != null)
            {
                bodyLabel.text = loadingPhases[0].bodyText;
            }

            // 이미지는 ApplyPhase로 처리
            ApplyPhase(0);

            if (enableDebugLogs) Debug.Log("[LoadingBackgroundController] 첫 번째 페이즈 UI 설정 완료");
        }
    }

    private IEnumerator PhaseTransitionLoop()
    {
        while (true)
        {
            // yield return new WaitForSeconds(3.0f); // 기본 3초 간격

            // 다음 단계로 전환
            currentPhaseIndex = (currentPhaseIndex + 1) % loadingPhases.Length;
            ApplyPhase(currentPhaseIndex);
        }
    }

    private void ApplyPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= loadingPhases.Length)
        {
            Debug.LogError(
                $"❌ [LoadingBackgroundController] 잘못된 페이즈 인덱스: {phaseIndex} (범위: 0-{loadingPhases.Length - 1})"
            );
            return;
        }

        LoadingPhase phase = loadingPhases[phaseIndex];

        if (enableDebugLogs) Debug.Log(
            $"🔧 [LoadingBackgroundController] ApplyPhase({phaseIndex}) 시작 - {phase.phaseName} 적용"
        );

        // 모든 변경사항을 즉시 적용
        ApplyPhaseImmediate(phase);

        if (enableDebugLogs) Debug.Log(
            $"🏁 [LoadingBackgroundController] ApplyPhase({phaseIndex}) 완료 - {phase.phaseName} 적용됨"
        );
    }

    private void ApplyPhaseImmediate(LoadingPhase phase)
    {
        if (enableDebugLogs) Debug.Log(
            $"🎨 [LoadingBackgroundController] 아이콘 변경 시작 - {phase.phaseName} (Index: {currentPhaseIndex})"
        );

        // 모든 요소를 즉시 변경

        // 이미지 변경
        if (cachedBackgrounds != null && currentPhaseIndex < cachedBackgrounds.Length)
        {
            // 캐싱된 이미지 사용 (더 빠름)
            if (sunElement != null)
            {
                if (cachedBackgrounds[currentPhaseIndex].value != null)
                {
                    var oldIcon = sunElement.style.backgroundImage.value.sprite?.name ?? "None";
                    sunElement.style.backgroundImage = cachedBackgrounds[currentPhaseIndex];
                    sunElement.style.display = DisplayStyle.Flex;
                    var newIcon = cachedBackgrounds[currentPhaseIndex].value.sprite?.name ?? "None";
                    if (enableDebugLogs) Debug.Log(
                        $"🔄 [LoadingBackgroundController] 아이콘 변경: {oldIcon} → {newIcon}"
                    );
                }
                else
                {
                    sunElement.style.display = DisplayStyle.None;
                    if (enableDebugLogs) Debug.Log("🚫 [LoadingBackgroundController] 아이콘 숨김 (스프라이트 없음)");
                }
            }
        }
        else
        {
            // 캐시가 없으면 직접 로드 (폴백)
            if (sunElement != null && phase.sunOrMoonSprite != null)
            {
                var oldIcon = sunElement.style.backgroundImage.value.sprite?.name ?? "None";
                sunElement.style.backgroundImage = new StyleBackground(phase.sunOrMoonSprite);
                sunElement.style.display = DisplayStyle.Flex;
                if (enableDebugLogs) Debug.Log(
                    $"🔄 [LoadingBackgroundController] 아이콘 변경 (폴백): {oldIcon} → {phase.sunOrMoonSprite.name}"
                );
            }
        }

        if (cloudElement != null && phase.cloudSprite != null)
        {
            cloudElement.style.backgroundImage = new StyleBackground(phase.cloudSprite);
        }

        if (bodyLabel != null)
        {
            var oldText = bodyLabel.text;
            bodyLabel.text = phase.bodyText;
            Debug.Log(
                $"📝 [LoadingBackgroundController] 텍스트 변경: '{oldText}' → '{phase.bodyText}'"
            );
        }

        Debug.Log(
            $"✅ [LoadingBackgroundController] {phase.phaseName} UI 단계로 즉시 전환 완료 - Index: {currentPhaseIndex}"
        );
    }

    // Inspector에서 값이 변경될 때 자동 업데이트 (Editor에서만)
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (
            Application.isPlaying
            && root != null
            && loadingPhases != null
            && loadingPhases.Length > 0
        )
        {
            ApplyPhase(currentPhaseIndex);
        }
    }
#endif

    #region LoadingSceneManager Integration

    /// <summary>
    /// LoadingSceneManager 이벤트 구독
    /// </summary>
    private void SubscribeToLoadingSceneManager()
    {
        if (_isSubscribedToEvents)
            return;

        // 진행률 이벤트 구독
        LoadingSceneManager.OnProgressUpdated += OnLoadProgressChanged;

        // 메인 씬 로딩 완료 이벤트 구독
        LoadingSceneManager.OnMainSceneLoadCompleted += OnMainSceneLoadCompleted;

        _isSubscribedToEvents = true;
        Debug.Log("[LoadingBackgroundController] LoadingSceneManager 이벤트 구독 완료");
    }

    /// <summary>
    /// LoadingSceneManager 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromLoadingSceneManager()
    {
        if (!_isSubscribedToEvents)
            return;

        LoadingSceneManager.OnProgressUpdated -= OnLoadProgressChanged;
        LoadingSceneManager.OnMainSceneLoadCompleted -= OnMainSceneLoadCompleted;

        _isSubscribedToEvents = false;
        Debug.Log("[LoadingBackgroundController] LoadingSceneManager 이벤트 구독 해제 완료");
    }

    /// <summary>
    /// 로딩 진행률 변경 이벤트
    /// </summary>
    private void OnLoadProgressChanged(float progress)
    {
        int targetPhaseIndex = GetPhaseIndexFromProgress(progress);

        if (enableDebugLogs) Debug.Log(
            $"📊 [LoadingBackgroundController] 진행률 {progress:P0} → 대상 페이즈: {targetPhaseIndex}, 현재 페이즈: {currentPhaseIndex}"
        );

        // 현재 페이즈와 다르면 전환
        if (targetPhaseIndex != currentPhaseIndex && targetPhaseIndex < loadingPhases.Length)
        {
            var oldPhaseName = currentPhaseIndex < loadingPhases.Length ? loadingPhases[currentPhaseIndex].phaseName : "Unknown";
            var newPhaseName = loadingPhases[targetPhaseIndex].phaseName;

            Debug.Log(
                $"🔄 [LoadingBackgroundController] 페이즈 전환 시작: {oldPhaseName}({currentPhaseIndex}) → {newPhaseName}({targetPhaseIndex}) [진행률: {progress:P0}]"
            );

            currentPhaseIndex = targetPhaseIndex;

            Debug.Log(
                $"🎯 [LoadingBackgroundController] ApplyPhase({currentPhaseIndex}) 호출 직전 - {newPhaseName} 적용 예정"
            );

            ApplyPhase(currentPhaseIndex);

            // SkyboxTransitionManager에 직접 스카이박스 변경 요청
            ApplySkyboxChange(targetPhaseIndex, progress);

            Debug.Log(
                $"✅ [LoadingBackgroundController] 진행률 {progress:P0}에 따라 {newPhaseName}({currentPhaseIndex})번 페이즈로 전환 완료"
            );
        }
        else
        {
            var currentPhaseName = currentPhaseIndex < loadingPhases.Length ? loadingPhases[currentPhaseIndex].phaseName : "Unknown";
            if (enableDebugLogs) Debug.Log(
                $"⏭️ [LoadingBackgroundController] 페이즈 전환 불필요 (이미 {currentPhaseName}({currentPhaseIndex})번 페이즈) [진행률: {progress:P0}]"
            );
        }
    }

    /// <summary>
    /// 진행률에 따른 페이즈 인덱스 계산
    /// </summary>
    private int GetPhaseIndexFromProgress(float progress)
    {
        if (progress < 0.3f)
            return 0; // 0-30%: 첫 번째 페이즈 (낮)
        else if (progress < 0.75f)
            return 1; // 30-75%: 두 번째 페이즈 (밤)
        else
            return 1; // 75-100%: 밤 페이즈 유지 (Dream은 메인 씬 로딩 완료 시에만)
    }

    /// <summary>
    /// 메인 씬 로딩 완료 이벤트 (Dream 페이즈로 전환)
    /// </summary>
    private void OnMainSceneLoadCompleted()
    {
        var previousSkyboxFlag = _shouldChangeSkybox;
        var previousPhaseIndex = currentPhaseIndex;
        var previousPhaseName = currentPhaseIndex < loadingPhases.Length ? loadingPhases[currentPhaseIndex].phaseName : "Unknown";

        Debug.Log(
            $"🏁 [LoadingBackgroundController] 메인 씬 로딩 완료 이벤트 수신 - 현재 페이즈: {previousPhaseName}({previousPhaseIndex}), 스카이박스 변경 허용: {previousSkyboxFlag}"
        );

        // 스카이박스 변경 중단 (메인 씬의 기본 스카이박스 유지)
        _shouldChangeSkybox = false;

        // Dream 페이즈로 강제 전환 (UI만)
        if (loadingPhases.Length > 2)
        {
            var dreamPhaseName = loadingPhases[2].phaseName;

            Debug.Log(
                $"🌙 [LoadingBackgroundController] Dream 페이즈({dreamPhaseName}) 강제 전환 시작 - 스카이박스 변경 중단됨"
            );

            currentPhaseIndex = 2;

            Debug.Log(
                $"🎯 [LoadingBackgroundController] ApplyPhase(2) 호출 직전 - {dreamPhaseName} UI만 적용 예정 (스카이박스 변경 없음)"
            );

            ApplyPhase(currentPhaseIndex);

            Debug.Log(
                $"✅ [LoadingBackgroundController] 메인 씬 로딩 완료 - {dreamPhaseName} 페이즈로 UI 전환 완료 (스카이박스는 메인씬 기본값 유지)"
            );
        }
        else
        {
            Debug.LogWarning(
                $"⚠️ [LoadingBackgroundController] Dream 페이즈 없음 - loadingPhases 길이: {loadingPhases.Length}"
            );
        }
    }

    /// <summary>
    /// 스카이박스 변경 적용
    /// </summary>
    private void ApplySkyboxChange(int phaseIndex, float progress)
    {
        // 메인 씬 로딩 완료 후에는 스카이박스 변경 금지
        if (!_shouldChangeSkybox)
        {
            Debug.Log("[LoadingBackgroundController] 메인 씬 로딩 완료로 스카이박스 변경 중단됨");
            return;
        }

        var skyboxManager = SkyboxTransitionManager.Instance;
        if (skyboxManager == null)
        {
            Debug.LogWarning(
                "[LoadingBackgroundController] SkyboxTransitionManager.Instance가 null입니다"
            );
            return;
        }

        // 페이즈별 스카이박스 변경
        switch (phaseIndex)
        {
            case 0: // Day 페이즈
                var dayMaterial = Resources.Load<Material>("sky_day");
                if (dayMaterial != null)
                {
                    skyboxManager.SetSkyboxImmediate(dayMaterial);
                    Debug.Log("[LoadingBackgroundController] Day 스카이박스로 변경");
                }
                break;

            case 1: // Night 페이즈
                var nightMaterial = Resources.Load<Material>("sky_night");
                if (nightMaterial != null)
                {
                    skyboxManager.SetSkyboxImmediate(nightMaterial);
                    Debug.Log("[LoadingBackgroundController] Night 스카이박스로 변경");
                }
                break;

            case 2: // Dream 페이즈 - 스카이박스 변경 없음
                Debug.Log("[LoadingBackgroundController] Dream 페이즈 - 스카이박스 변경 없음");
                break;
        }
    }

    #endregion

    #region Manual Phase Control (Debug)

    // 수동으로 특정 단계로 전환
    public void SetPhase(int phaseIndex)
    {
        if (phaseIndex >= 0 && phaseIndex < loadingPhases.Length)
        {
            currentPhaseIndex = phaseIndex;
            ApplyPhase(currentPhaseIndex);
        }
    }

    // 다음 단계로 전환
    public void NextPhase()
    {
        currentPhaseIndex = (currentPhaseIndex + 1) % loadingPhases.Length;
        ApplyPhase(currentPhaseIndex);
    }

    #endregion
}
