using UnityEngine;

public class ChestInteractionHandler : MonoBehaviour
{
    [Header("설정")]
    public AudioClip chestOpenSound;
    public AudioClip chestCloseSound;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void HandleChestInteraction(InteractableChest chest)
    {
        if (chest == null)
            return;

        // 상호작용 시작
        InteractionManager.Instance?.BeginInteraction();

        // 상자 열기
        chest.OpenChest();

        // 사운드 재생
        PlaySound(chestOpenSound);

        // UI 표시
        UIManager.Instance?.ShowChestInventory(chest);

        Debug.Log($"상자 상호작용: {chest.chestName}");
    }

    public void HandleChestClose()
    {
        // 사운드 재생
        PlaySound(chestCloseSound);

        Debug.Log("상자 닫기");
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
