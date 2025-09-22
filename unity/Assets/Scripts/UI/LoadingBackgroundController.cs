using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

[System.Serializable]
public class LoadingPhase
{
    public string phaseName = "낮";
    public string bodyText = "잠시만 기다려 주세요...";
    public Sprite sunOrMoonSprite;  // 해/달/별 이미지
    public Sprite cloudSprite;       // 구름 이미지 (필요시)
    public Material skyboxMaterial;  // 스카이박스 머터리얼
}

public class LoadingBackgroundController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("전환 설정")]
    [SerializeField] private float transitionInterval = 3f; // 3초마다 전환

    [Header("고정 텍스트")]
    [SerializeField] private string titleText = "꿈속으로";

    [Header("로딩 단계 설정")]
    [SerializeField] private LoadingPhase[] loadingPhases;

    private VisualElement root;
    private VisualElement sunElement;
    private VisualElement cloudElement;
    private Label titleLabel;
    private Label bodyLabel;

    private int currentPhaseIndex = 0;
    private Coroutine transitionCoroutine;
    private StyleBackground[] cachedBackgrounds; // 이미지 캐싱
    private Material defaultSkybox; // 기본 스카이박스 저장

    private void Awake()
    {
        // 스카이박스 머터리얼 로드
        LoadSkyboxMaterials();

        // 이미지를 미리 StyleBackground로 변환하여 캐싱
        PreloadImages();

        // 기본 스카이박스 저장
        defaultSkybox = RenderSettings.skybox;

        Debug.Log("[LoadingBackgroundController] 스카이박스 설정 완료");
    }

    private void LoadSkyboxMaterials()
    {
        // Inspector에서 설정된 loadingPhases에 스카이박스 머터리얼만 추가
        if (loadingPhases != null && loadingPhases.Length >= 3)
        {
            // Resources 폴더에서 스카이박스 머터리얼 로드
            Material skyDay = Resources.Load<Material>("sky_day");
            Material skyNight = Resources.Load<Material>("sky_night");
            Material skyDream = Resources.Load<Material>("New Material");

            if (loadingPhases.Length > 0) loadingPhases[0].skyboxMaterial = skyDay;
            if (loadingPhases.Length > 1) loadingPhases[1].skyboxMaterial = skyNight;
            if (loadingPhases.Length > 2) loadingPhases[2].skyboxMaterial = skyDream;

            Debug.Log($"[LoadingBackgroundController] 스카이박스 머터리얼 로드 완료");
            for (int i = 0; i < loadingPhases.Length; i++)
            {
                Debug.Log($"Phase {i}: {loadingPhases[i].phaseName}, Sprite: {(loadingPhases[i].sunOrMoonSprite != null ? loadingPhases[i].sunOrMoonSprite.name : "NULL")}, Material: {(loadingPhases[i].skyboxMaterial != null ? loadingPhases[i].skyboxMaterial.name : "NULL")}");
            }
        }
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
            Debug.Log($"[LoadingBackgroundController] {cachedBackgrounds.Length}개 이미지 프리로드 완료");
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
            // 전환 시작
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(PhaseTransitionLoop());
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

            // 첫 번째 스카이박스와 텍스트 즉시 적용
            if (loadingPhases[0].skyboxMaterial != null)
            {
                RenderSettings.skybox = loadingPhases[0].skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }

            if (bodyLabel != null)
            {
                bodyLabel.text = loadingPhases[0].bodyText;
            }

            // 이미지는 ApplyPhase로 처리
            ApplyPhase(0);
        }
    }

    private IEnumerator PhaseTransitionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(transitionInterval);

            // 다음 단계로 전환
            currentPhaseIndex = (currentPhaseIndex + 1) % loadingPhases.Length;
            ApplyPhase(currentPhaseIndex);
        }
    }

    private void ApplyPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= loadingPhases.Length)
            return;

        LoadingPhase phase = loadingPhases[phaseIndex];

        // 모든 변경사항을 동시에 적용
        StartCoroutine(ApplyPhaseWithSync(phase));
    }

    private IEnumerator ApplyPhaseWithSync(LoadingPhase phase)
    {
        // 1단계: 이미지 먼저 변경
        if (cachedBackgrounds != null && currentPhaseIndex < cachedBackgrounds.Length)
        {
            // 캐싱된 이미지 사용 (더 빠름)
            if (sunElement != null)
            {
                if (cachedBackgrounds[currentPhaseIndex].value != null)
                {
                    sunElement.style.backgroundImage = cachedBackgrounds[currentPhaseIndex];
                    sunElement.style.display = DisplayStyle.Flex;
                }
                else
                {
                    sunElement.style.display = DisplayStyle.None;
                }
            }
        }
        else
        {
            // 캐시가 없으면 직접 로드 (폴백)
            if (sunElement != null && phase.sunOrMoonSprite != null)
            {
                sunElement.style.backgroundImage = new StyleBackground(phase.sunOrMoonSprite);
                sunElement.style.display = DisplayStyle.Flex;
            }
        }

        if (cloudElement != null && phase.cloudSprite != null)
        {
            cloudElement.style.backgroundImage = new StyleBackground(phase.cloudSprite);
        }

        // 이미지 렌더링 완료 대기 (1초 대기)
        yield return new WaitForSeconds(1.0f);

        // 2단계: 스카이박스와 텍스트 동시 변경
        if (phase.skyboxMaterial != null)
        {
            RenderSettings.skybox = phase.skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }

        if (bodyLabel != null)
        {
            bodyLabel.text = phase.bodyText;
        }

        Debug.Log($"[LoadingBackgroundController] {phase.phaseName} 단계로 전환 완료 - Index: {currentPhaseIndex}");
    }



    // Inspector에서 값이 변경될 때 자동 업데이트 (Editor에서만)
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && root != null && loadingPhases != null && loadingPhases.Length > 0)
        {
            ApplyPhase(currentPhaseIndex);
        }
    }
#endif

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
}