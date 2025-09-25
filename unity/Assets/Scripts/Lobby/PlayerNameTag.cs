using UnityEngine;
using UnityEngine.UI;

public class PlayerNameTag : MonoBehaviour
{
    [Header("네임태그 설정")]
    public Canvas nameTagCanvas;
    public Text playerNameText;
    public float heightOffset = 2f; // 캐릭터 머리 위 높이
    
    [Header("카메라 따라보기")]
    public bool lookAtCamera = true;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        CreateNameTag();
    }
    
    void CreateNameTag()
    {
        if (nameTagCanvas == null)
        {
            Debug.Log("네임태그 Canvas 생성 시작...");
            
            // Canvas 생성
            GameObject canvasGO = new GameObject("NameTagCanvas");
            canvasGO.transform.SetParent(transform);
            
            nameTagCanvas = canvasGO.AddComponent<Canvas>();
            nameTagCanvas.renderMode = RenderMode.WorldSpace;
            nameTagCanvas.worldCamera = mainCamera;
            
            // CanvasScaler 추가 (중요!)
            CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            
            // Canvas 크기 조정
            RectTransform canvasRect = nameTagCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(600, 150); // 크기를 2배로 늘림
            canvasRect.localScale = Vector3.one * 0.004f;
            
            // 위치 조정
            canvasRect.localPosition = new Vector3(0, heightOffset, 0);
            
            Debug.Log($"Canvas 크기: {canvasRect.sizeDelta}, 위치: {canvasRect.localPosition}");
            
            // Text 생성
            GameObject textGO = new GameObject("PlayerName");
            textGO.transform.SetParent(canvasGO.transform, false);
            
            playerNameText = textGO.AddComponent<Text>();
            playerNameText.text = "";
            
            // 폰트 설정
            playerNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (playerNameText.font == null)
            {
                Debug.LogWarning("LegacyRuntime.ttf 로드 실패, 기본 폰트 사용");
            }
            
            playerNameText.fontSize = 96;
            playerNameText.color = Color.white;
            playerNameText.alignment = TextAnchor.MiddleCenter;
            
            // 텍스트에 Outline 추가 (가독성 향상)
            Outline outline = textGO.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(1, 1);
            
            // Text RectTransform 설정
            RectTransform textRect = playerNameText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Debug.Log("네임태그 Text 생성 완료");
        }
    }
    
    void Update()
    {
        // 카메라 방향으로 회전
        if (lookAtCamera && mainCamera != null && nameTagCanvas != null)
        {
            nameTagCanvas.transform.LookAt(mainCamera.transform);
            nameTagCanvas.transform.Rotate(0, 180, 0); // 뒤집힌 텍스트 수정
        }
    }
    
    public void SetPlayerName(string name)
    {
        if (playerNameText != null)
        {
            playerNameText.text = name;
        }
    }
    
    public void SetNameColor(Color color)
    {
        if (playerNameText != null)
        {
            playerNameText.color = color;
        }
    }
    
    public void SetNameTagVisible(bool visible)
    {
        if (nameTagCanvas != null)
        {
            nameTagCanvas.gameObject.SetActive(visible);
        }
    }
    
    public void SetHeightOffset(float offset)
    {
        heightOffset = offset;
        if (nameTagCanvas != null)
        {
            nameTagCanvas.transform.localPosition = new Vector3(0, heightOffset, 0);
        }
    }
}