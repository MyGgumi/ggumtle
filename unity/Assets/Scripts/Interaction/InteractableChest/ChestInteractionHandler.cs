// DEPRECATED: MVVM 패턴으로 리팩토링되어 ChestViewModel으로 대체됨
// 이 코드는 레거시 코드로 주석 처리됨

/*
using UnityEngine;

/// <summary>
/// 상자 상호작용을 전담하는 글로벌 핸들러
/// - 싱글톤 패턴으로 모든 상자에서 공용
/// - 사운드, UI 표시 등의 공통 로직 처리
/// </summary>
public class ChestInteractionHandler : MonoBehaviour
{
    public static ChestInteractionHandler Instance;

    [Header("기본 사운드")]
    public AudioClip defaultOpenSound;
    public AudioClip defaultCloseSound;

    [Header("사운드 설정")]
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    private AudioSource audioSource;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSource();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // AudioSource 기본 설정
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
    }

    /// <summary>
    /// 상자 열기 상호작용 처리
    /// </summary>
    public void HandleChestInteraction(InteractableChest chest)
    {
        if (chest == null)
        {
            Debug.LogWarning("[ChestInteractionHandler] 상자가 null입니다!");
            return;
        }

        // 상자 열기
        chest.OpenChest();

        // 사운드 재생 (상자 개별 사운드 또는 기본 사운드)
        AudioClip openSound = GetChestOpenSound(chest);
        PlaySound(openSound);

        // UI 표시
        ShowChestUI(chest);

        Debug.Log($"[ChestInteractionHandler] 상자 열기: {chest.chestName}");
    }

    /// <summary>
    /// 상자 닫기 상호작용 처리
    /// </summary>
    public void HandleChestClose(InteractableChest chest = null)
    {
        // 사운드 재생
        AudioClip closeSound = chest != null ? GetChestCloseSound(chest) : defaultCloseSound;
        PlaySound(closeSound);

        Debug.Log($"[ChestInteractionHandler] 상자 닫기: {chest?.chestName ?? "미지정"}");
    }

    /// <summary>
    /// UI 표시 처리 (상자 종류에 따른 확장 가능)
    /// </summary>
    private void ShowChestUI(InteractableChest chest)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[ChestInteractionHandler] UIManager.Instance가 null입니다!");
            return;
        }

        // 기본 상자 UI 표시
        UIManager.Instance.ShowChestInventory(chest);

        // 상자 타입에 따른 추가 처리 가능
        // 예: 상점 상자, 퀘스트 상자 등
    }

    /// <summary>
    /// 상자별 열기 사운드 가져오기
    /// </summary>
    private AudioClip GetChestOpenSound(InteractableChest chest)
    {
        // 나중에 상자 타입에 따른 사운드 처리 가능
        // 예: chest.chestType에 따라 다른 사운드
        return defaultOpenSound;
    }

    /// <summary>
    /// 상자별 닫기 사운드 가져오기
    /// </summary>
    private AudioClip GetChestCloseSound(InteractableChest chest)
    {
        return defaultCloseSound;
    }

    /// <summary>
    /// 사운드 재생
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[ChestInteractionHandler] AudioSource가 null입니다!");
            return;
        }

        if (clip != null)
        {
            audioSource.volume = soundVolume;
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("[ChestInteractionHandler] 재생할 AudioClip이 null입니다!");
        }
    }

    /// <summary>
    /// 사운드 볼륨 조절
    /// </summary>
    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = soundVolume;
        }
    }

    /// <summary>
    /// 전역에서 상자 열기 사운드 재생 (스태틱 메서드)
    /// </summary>
    public static void PlayOpenSound(InteractableChest chest = null)
    {
        if (Instance != null)
        {
            AudioClip sound =
                chest != null ? Instance.GetChestOpenSound(chest) : Instance.defaultOpenSound;
            Instance.PlaySound(sound);
        }
    }

    /// <summary>
    /// 전역에서 상자 닫기 사운드 재생 (스태틱 메서드)
    /// </summary>
    public static void PlayCloseSound(InteractableChest chest = null)
    {
        if (Instance != null)
        {
            AudioClip sound =
                chest != null ? Instance.GetChestCloseSound(chest) : Instance.defaultCloseSound;
            Instance.PlaySound(sound);
        }
    }
}
*/