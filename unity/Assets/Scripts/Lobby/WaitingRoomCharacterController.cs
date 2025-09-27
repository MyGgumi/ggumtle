using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class WaitingRoomCharacterController : MonoBehaviour
{
    [Header("캐릭터 슬롯")]
    public GameObject[] characterSlots; // 5개 캐릭터 슬롯

    [Header("네임태그 UI")]
    public Canvas nameTagCanvas; // UI Canvas 참조
    public GameObject nameTagPrefab; // 네임태그 프리팹 (없으면 자동 생성)

    public enum CharacterType
    {
        HpMongging = 0,
        JobMongging = 1,
        HealMongging = 2
    }

    // 각 슬롯의 타입별 캐릭터들 (슬롯별로 3개씩)
    private GameObject[,] charactersByType; // [슬롯인덱스, 타입인덱스]
    private List<GameObject> nameTagUIList = new List<GameObject>(); // UI 네임태그 리스트
    private Camera mainCamera;
    
    // 캐릭터 관리를 위한 클래스
    [System.Serializable]
    public class CharacterData
    {
        public string nickname;
        public CharacterType currentType;
        public int slotIndex;
        public int level;

        public CharacterData(string name, CharacterType type, int slot, int lv)
        {
            nickname = name;
            currentType = type;
            slotIndex = slot;
            level = lv;
        }
    }

    // 현재 활성 캐릭터 리스트
    private List<CharacterData> activeCharacters = new List<CharacterData>();
    private int currentPlayerCount = 0;

    #region 초기화
    public void Initialize()
    {
        mainCamera = Camera.main;
        
        // UI Canvas 자동 생성
        if (nameTagCanvas == null)
        {
            CreateNameTagCanvas();
        }

        // 캐릭터 타입별 GameObject 배열 초기화
        charactersByType = new GameObject[characterSlots.Length, 3];
        
        // 각 슬롯의 자식 오브젝트들을 타입별로 매핑
        for (int slotIndex = 0; slotIndex < characterSlots.Length; slotIndex++)
        {
            Transform slotTransform = characterSlots[slotIndex].transform;
            
            // 각 슬롯의 3개 타입 찾기 (이름 기반)
            for (int childIndex = 0; childIndex < slotTransform.childCount; childIndex++)
            {
                GameObject child = slotTransform.GetChild(childIndex).gameObject;
                string childName = child.name.ToLower();
                
                if (childName.Contains("hp"))
                {
                    charactersByType[slotIndex, (int)CharacterType.HpMongging] = child;
                }
                else if (childName.Contains("job"))
                {
                    charactersByType[slotIndex, (int)CharacterType.JobMongging] = child;
                }
                else if (childName.Contains("heal"))
                {
                    charactersByType[slotIndex, (int)CharacterType.HealMongging] = child;
                }
            }
            
            // 모든 캐릭터는 활성화 상태로 두고 렌더러만 비활성화
            characterSlots[slotIndex].SetActive(true);
            HideAllTypesInSlot(slotIndex);
        }

        // 모든 캐릭터 슬롯 비활성화
        for (int i = 0; i < characterSlots.Length; i++)
        {
            characterSlots[i].SetActive(false);
        }

        currentPlayerCount = 0;
        activeCharacters.Clear();

        Debug.Log("캐릭터 컨트롤러 초기화 완료");
    }

    // UI Canvas 자동 생성
    void CreateNameTagCanvas()
    {
        GameObject canvasGO = new GameObject("NameTagCanvas");
        nameTagCanvas = canvasGO.AddComponent<Canvas>();
        nameTagCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        nameTagCanvas.sortingOrder = 10; // 다른 UI보다 위에 표시

        // CanvasScaler 추가
        CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        // GraphicRaycaster 추가
        GraphicRaycaster raycaster = canvasGO.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        Debug.Log("네임태그 Canvas 자동 생성 완료");
    }
    #endregion

    #region UI 네임태그 생성
    // UI 네임태그 생성 (생성 즉시 올바른 위치에 배치)
    GameObject CreateUINameTag(string nickname, int level, int slotIndex)
    {
        GameObject nameTagGO = new GameObject($"NameTag_{nickname}");
        nameTagGO.transform.SetParent(nameTagCanvas.transform, false);

        // 배경 이미지 (투명)
        Image backgroundImage = nameTagGO.AddComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0); // 완전 투명 배경
        
        // RectTransform 설정
        RectTransform rectTransform = nameTagGO.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 400);

        // 클릭 이벤트 추가
        Button nameTagButton = nameTagGO.AddComponent<Button>();
        nameTagButton.targetGraphic = backgroundImage;
        nameTagButton.onClick.AddListener(() => {
            OnNameTagClicked(nickname, level, slotIndex);
        });

        // 텍스트 생성
        CreateNameTagText(nameTagGO, nickname, level);

        // 첫 번째 캐릭터에만 새로고침 버튼 추가
        if (slotIndex == 0)
        {
            CreateRefreshButton(nameTagGO, nickname);
        }

        // 생성 즉시 올바른 위치에 배치
        SetNameTagPosition(nameTagGO, slotIndex);

        Debug.Log($"UI 네임태그 생성: {nickname} Lv.{level}");
        return nameTagGO;
    }

    // 네임태그 텍스트 생성
    void CreateNameTagText(GameObject parent, string nickname, int level)
    {
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(parent.transform, false);

        Text nameText = textGO.AddComponent<Text>();
        nameText.text = $"{nickname}\nLv.{level}";
        // 수성혜성체 폰트 로드 (Resources 폴더에 있어야 함)
        Font customFont = Resources.Load<Font>("SuseongHyejeong");
        if (customFont != null)
        {
            nameText.font = customFont;
        }
        else
        {
            // 폰트를 찾을 수 없으면 기본 폰트 사용
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Debug.LogWarning("수성혜성체 폰트를 찾을 수 없습니다. 기본 폰트를 사용합니다.");
        }
        nameText.fontSize = 70;
        nameText.color = Color.white;
        nameText.alignment = TextAnchor.MiddleCenter;

        // 텍스트에 Outline 추가
        Outline outline = textGO.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, 1);

        // 텍스트 RectTransform 설정
        RectTransform textRect = nameText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    // 새로고침 버튼 생성 (첫 번째 캐릭터 전용)
    void CreateRefreshButton(GameObject parentNameTag, string characterName)
    {
        GameObject refreshButtonGO = new GameObject("RefreshButton");
        refreshButtonGO.transform.SetParent(parentNameTag.transform, false);

        // 버튼 배경 이미지 - 하얀색 원형 배경
        Image buttonImage = refreshButtonGO.AddComponent<Image>();
        buttonImage.color = Color.white; // 하얀색 배경
        buttonImage.type = Image.Type.Simple;
        
        // 원형 스프라이트 생성 (코드로 생성)
        buttonImage.sprite = CreateCircleSprite();

        // RectTransform 설정 (네임태그 오른쪽에 배치)
        RectTransform buttonRect = refreshButtonGO.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(100, 100); // 버튼 크기 (정사각형으로 원형 만들기)
        buttonRect.anchoredPosition = new Vector2(220, 0); // 네임태그 오른쪽에 배치

        // 버튼 컴포넌트 추가
        Button refreshButton = refreshButtonGO.AddComponent<Button>();
        refreshButton.targetGraphic = buttonImage;

        // 버튼 색상 설정 (호버, 클릭 효과)
        ColorBlock colors = refreshButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f); // 약간 어두운 회색
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f); // 더 어두운 회색
        refreshButton.colors = colors;

        // 새로고침 아이콘 생성
        CreateRefreshIcon(refreshButtonGO);

        // 클릭 이벤트 등록
        refreshButton.onClick.AddListener(() => {
            OnRefreshButtonClicked(characterName);
        });

        Debug.Log($"새로고침 버튼 생성: {characterName}");
    }

    // 새로고침 아이콘 생성 - 완전 검정색 텍스트
    void CreateRefreshIcon(GameObject buttonParent)
    {
        GameObject iconGO = new GameObject("RefreshIcon");
        iconGO.transform.SetParent(buttonParent.transform, false);

        Text iconText = iconGO.AddComponent<Text>();
        iconText.text = "↻"; // 새로고침 기호
        iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconText.fontSize = 80; // 크기 조정
        iconText.color = new Color(0f, 0f, 0f, 1f); // 완전 검정색 (0,0,0,1)
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.raycastTarget = false; // 텍스트는 클릭 이벤트 차단하지 않음
        iconText.fontStyle = FontStyle.Bold; // 굵은 글씨

        // 아이콘 RectTransform 설정
        RectTransform iconRect = iconText.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
    }

    // 원형 스프라이트 생성 메서드 (추가)
    Sprite CreateCircleSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2; // 약간의 여백
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    // 원 내부는 완전 불투명
                    pixels[y * size + x] = Color.white;
                }
                else if (distance <= radius + 2)
                {
                    // 가장자리는 약간 부드럽게 (안티앨리어싱)
                    float alpha = 1f - (distance - radius) / 2f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    // 원 외부는 완전 투명
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    #endregion

    #region 네임태그 위치 관리
    // 네임태그 위치 설정 (개별)
    void SetNameTagPosition(GameObject nameTag, int slotIndex)
    {
        if (nameTag != null && mainCamera != null)
        {
            Vector3 worldPosition = GetStumpWorldPosition(slotIndex);
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            if (screenPosition.z > 0)
            {
                nameTag.SetActive(true);
                nameTag.transform.position = screenPosition;
            }
            else
            {
                nameTag.SetActive(false);
            }
        }
    }

    // 발판 위치 찾기
    Vector3 GetStumpWorldPosition(int slotIndex)
    {
        GameObject stumpsParent = GameObject.Find("Stumps");
        if (stumpsParent != null)
        {
            string stumpName = $"Stump {slotIndex + 1}";
            Transform stumpTransform = stumpsParent.transform.Find(stumpName);
            
            if (stumpTransform != null)
            {
                return stumpTransform.position + Vector3.down * 0.3f;
            }
        }
        
        // 발판을 못 찾으면 캐릭터 아래쪽 위치 사용
        if (characterSlots[slotIndex] != null)
        {
            return characterSlots[slotIndex].transform.position + Vector3.down * 1.0f;
        }
        
        return Vector3.zero;
    }

    // Update에서 네임태그 위치 업데이트 (카메라 움직임 대응)
    void Update()
    {
        UpdateNameTagPositions();
    }

    // 네임태그 위치 업데이트 (모든 네임태그)
    void UpdateNameTagPositions()
    {
        for (int i = 0; i < activeCharacters.Count && i < nameTagUIList.Count; i++)
        {
            SetNameTagPosition(nameTagUIList[i], activeCharacters[i].slotIndex);
        }
    }
    #endregion

    #region 캐릭터 가시성 제어
    // 특정 슬롯의 모든 타입 숨기기 (애니메이터는 유지)
    void HideAllTypesInSlot(int slotIndex)
    {
        for (int typeIndex = 0; typeIndex < 3; typeIndex++)
        {
            if (charactersByType[slotIndex, typeIndex] != null)
            {
                SetCharacterVisible(charactersByType[slotIndex, typeIndex], false);
            }
        }
    }

    // 캐릭터의 가시성만 제어 (애니메이터는 그대로 둠)
    void SetCharacterVisible(GameObject character, bool visible)
    {
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
        
        // 콜라이더도 함께 제어
        Collider[] colliders = character.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = visible;
            }
        }
    }

    // 특정 슬롯의 캐릭터 타입 설정 (렌더러만 제어)
    public void SetCharacterTypeInSlot(int slotIndex, CharacterType type)
    {
        // 해당 슬롯의 모든 타입 숨기기
        HideAllTypesInSlot(slotIndex);

        // 선택된 타입만 보이기
        if (charactersByType[slotIndex, (int)type] != null)
        {
            SetCharacterVisible(charactersByType[slotIndex, (int)type], true);
        }
    }
    #endregion

    #region 캐릭터 관리
    // 닉네임과 레벨로 캐릭터 추가 (기본: HP 타입)
    public void AddCharacterByNickname(string nickname, int level)
    {
        // 이미 같은 닉네임이 있는지 확인
        if (FindCharacterByNickname(nickname) != null)
        {
            Debug.LogWarning($"이미 존재하는 닉네임입니다: {nickname}");
            return;
        }

        // 최대 캐릭터 수 확인
        if (currentPlayerCount >= characterSlots.Length)
        {
            Debug.LogWarning("최대 캐릭터 수에 도달했습니다.");
            return;
        }

        // 캐릭터 슬롯 활성화 및 기본 타입(HP) 설정
        int slotIndex = currentPlayerCount;
        characterSlots[slotIndex].SetActive(true);
        
        // HP 타입으로 시작
        SetCharacterTypeInSlot(slotIndex, CharacterType.HpMongging);

        // 캐릭터 데이터 생성
        CharacterData newCharacter = new CharacterData(nickname, CharacterType.HpMongging, slotIndex, level);

        // UI 네임태그 생성 (생성 즉시 올바른 위치에 배치됨)
        GameObject nameTagUI = CreateUINameTag(nickname, level, slotIndex);
        nameTagUIList.Add(nameTagUI);

        // 캐릭터 데이터 저장
        activeCharacters.Add(newCharacter);
        currentPlayerCount++;

        Debug.Log($"캐릭터 추가: {nickname} 레벨 {level} (슬롯 {slotIndex + 1}, 타입: HP)");
    }

    public void AddCharacterByNickname(string data)
    {
        string[] parts = data.Split(',');
        if (parts.Length >= 2 && int.TryParse(parts[1], out int level))
        {
            AddCharacterByNickname(parts[0], level);
        }
    }

    // 닉네임으로 캐릭터 삭제
    public void RemoveCharacterByNickname(string nickname)
    {
        CharacterData targetCharacter = FindCharacterByNickname(nickname);
        if (targetCharacter == null)
        {
            Debug.LogWarning($"해당 닉네임의 캐릭터를 찾을 수 없습니다: {nickname}");
            return;
        }

        // UI 네임태그도 제거
        int characterIndex = activeCharacters.IndexOf(targetCharacter);
        if (characterIndex >= 0 && characterIndex < nameTagUIList.Count)
        {
            if (nameTagUIList[characterIndex] != null)
            {
                DestroyImmediate(nameTagUIList[characterIndex]);
            }
            nameTagUIList.RemoveAt(characterIndex);
        }

        // 리스트에서 제거
        activeCharacters.Remove(targetCharacter);
        currentPlayerCount--;

        Debug.Log($"캐릭터 삭제: {nickname}");

        // 슬롯 재정렬 (당기기 방식)
        ReorganizeCharacterSlots();
    }
    // 해당 닉네임의 캐릭터 타입 변경
    public void ChangeCharacterTypeByNickname(string nickname, string characterType, int level)
    {
        // 해당 닉네임의 캐릭터 찾기
        CharacterData targetCharacter = FindCharacterByNickname(nickname);
        if (targetCharacter == null)
        {
            Debug.LogWarning($"해당 닉네임의 캐릭터를 찾을 수 없습니다: {nickname}");
            return;
        }

        // 문자열을 CharacterType enum으로 변환
        CharacterType newType;
        if (!System.Enum.TryParse(characterType, true, out newType))
        {
            Debug.LogError($"잘못된 캐릭터 타입입니다: {characterType}");
            return;
        }

        // 캐릭터 타입 변경
        SetCharacterTypeInSlot(targetCharacter.slotIndex, newType);
        targetCharacter.currentType = newType;
        
        // 레벨 업데이트
        targetCharacter.level = level;
        
        // 네임태그 텍스트 업데이트
        UpdateNameTagText(targetCharacter.slotIndex, nickname, level);

        Debug.Log($"캐릭터 타입 및 레벨 변경: {nickname} -> {newType}, Lv.{level}");
    }

    // 네임태그 텍스트 업데이트 메서드 추가
    void UpdateNameTagText(int slotIndex, string nickname, int level)
    {
        // activeCharacters에서 해당 슬롯 인덱스의 캐릭터 찾기
        int characterIndex = activeCharacters.FindIndex(c => c.slotIndex == slotIndex);
        
        if (characterIndex >= 0 && characterIndex < nameTagUIList.Count)
        {
            GameObject nameTagUI = nameTagUIList[characterIndex];
            if (nameTagUI != null)
            {
                // 네임태그의 Text 컴포넌트 찾기
                Text nameText = nameTagUI.GetComponentInChildren<Text>();
                if (nameText != null)
                {
                    nameText.text = $"{nickname}\nLv.{level}";
                    Debug.Log($"네임태그 텍스트 업데이트: {nickname} Lv.{level}");
                }
            }
        }
    }

    public void SetFirstCharacter(string data)
    {
        string[] parts = data.Split(',');
        if (parts.Length >= 2 && int.TryParse(parts[1], out int level))
        {
            SetFirstCharacter(parts[0], level);
        }
    }

    // 첫 번째 캐릭터 설정 (레벨 포함)
    public void SetFirstCharacter(string nickname, int level)
    {
        ClearAllCharacters(); // 기존 캐릭터 모두 제거
        AddCharacterByNickname(nickname, level);
    }

    // 모든 캐릭터 제거
    public void ClearAllCharacters()
    {
        // 슬롯 비활성화
        for (int i = 0; i < characterSlots.Length; i++)
        {
            if (characterSlots[i] != null)
            {
                characterSlots[i].SetActive(false);
            }
        }

        // 모든 UI 네임태그 제거
        foreach (GameObject nameTag in nameTagUIList)
        {
            if (nameTag != null)
            {
                DestroyImmediate(nameTag);
            }
        }
        nameTagUIList.Clear();

        activeCharacters.Clear();
        currentPlayerCount = 0;

        Debug.Log("모든 캐릭터 제거");
    }

    // 모든 캐릭터 비활성화
    public void DeactivateAllCharacters()
    {
        for (int i = 0; i < characterSlots.Length; i++)
        {
            if (characterSlots[i] != null)
            {
                characterSlots[i].SetActive(false);
            }
        }

        // 모든 UI 네임태그 숨기기
        foreach (GameObject nameTag in nameTagUIList)
        {
            if (nameTag != null)
            {
                nameTag.SetActive(false);
            }
        }

        activeCharacters.Clear();
        currentPlayerCount = 0;
    }
    
    // 특정 캐릭터의 닉네임 변경
    public void ChangeCharacterNickname(string oldNickname, string newNickname)
    {
        // 기존 캐릭터 찾기
        CharacterData targetCharacter = FindCharacterByNickname(oldNickname);
        if (targetCharacter == null)
        {
            Debug.LogWarning($"해당 닉네임의 캐릭터를 찾을 수 없습니다: {oldNickname}");
            return;
        }
        
        // 새 닉네임이 이미 존재하는지 확인
        if (FindCharacterByNickname(newNickname) != null)
        {
            Debug.LogWarning($"이미 존재하는 닉네임입니다: {newNickname}");
            return;
        }
        
        // 닉네임 변경
        targetCharacter.nickname = newNickname;
        
        // 네임태그 텍스트 업데이트
        UpdateNameTagText(targetCharacter.slotIndex, newNickname, targetCharacter.level);
        
        Debug.Log($"캐릭터 닉네임 변경: {oldNickname} -> {newNickname}");
    }

    // 첫 번째 캐릭터의 타입 변경 (버튼용)
    public void ChangeFirstCharacterType()
    {
        if (activeCharacters.Count == 0)
        {
            Debug.LogWarning("변경할 캐릭터가 없습니다.");
            return;
        }

        CharacterData firstCharacter = activeCharacters[0];

        // 다음 타입으로 순환
        CharacterType nextType = (CharacterType)(((int)firstCharacter.currentType + 1) % 3);

        // 타입 변경
        SetCharacterTypeInSlot(firstCharacter.slotIndex, nextType);
        firstCharacter.currentType = nextType;

        Debug.Log($"첫 번째 캐릭터 타입 변경: {firstCharacter.nickname} -> {nextType}");
    }
    #endregion

    #region 이벤트 처리
    // 네임태그 클릭 시 호출되는 메서드
    void OnNameTagClicked(string nickname, int level, int slotIndex)
    {
        Debug.Log($"[네임태그 클릭] 닉네임: {nickname}, 레벨: {level}, 슬롯: {slotIndex + 1}");
                
        // 해당 캐릭터 정보 찾기
        CharacterData clickedCharacter = FindCharacterByNickname(nickname);
        if (clickedCharacter != null)
        {
            Debug.Log($"[캐릭터 정보] 타입: {clickedCharacter.currentType}, 활성 상태: true");
            
            // 안드로이드 함수 호출
            CallAndroidFunction("onNameTagClicked", nickname, clickedCharacter.currentType.ToString());
        }
    }

    // 새로고침 버튼 클릭 시 호출되는 메서드
    void OnRefreshButtonClicked(string characterName)
    {
        Debug.Log($"[새로고침 버튼 클릭] 캐릭터: {characterName}");
        
        // 첫 번째 캐릭터의 타입 변경
        if (activeCharacters.Count > 0)
        {
            CharacterData firstCharacter = activeCharacters[0];
            
            // 다음 타입으로 순환 (HP -> Job -> Heal -> HP ...)
            CharacterType nextType = (CharacterType)(((int)firstCharacter.currentType + 1) % 3);
            
            // 캐릭터 타입 변경 (기존 타입 숨기고 새 타입 보이기)
            SetCharacterTypeInSlot(firstCharacter.slotIndex, nextType);
            firstCharacter.currentType = nextType;
            
            Debug.Log($"첫 번째 캐릭터 타입 변경: {firstCharacter.nickname} -> {nextType}");
            
            // 안드로이드에 변경된 캐릭터 타입만 전달
            CallAndroidFunction("onRefreshButtonClicked", firstCharacter.currentType.ToString());
        }
        else
        {
            Debug.LogWarning("변경할 캐릭터가 없습니다.");
        }
    }

    // 안드로이드 함수 호출 통합 메서드
    void CallAndroidFunction(string functionName, params string[] parameters)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                currentActivity.Call(functionName, parameters);
                Debug.Log($"안드로이드 함수 호출: {functionName}({string.Join(", ", parameters)})");
            }
        }
#else
        Debug.Log($"[에디터/비안드로이드] 안드로이드 함수 호출 시뮬레이션: {functionName}({string.Join(", ", parameters)})");
#endif
    }
    #endregion

    #region 내부 유틸리티
    // 닉네임으로 캐릭터 찾기
    CharacterData FindCharacterByNickname(string nickname)
    {
        return activeCharacters.Find(character => character.nickname == nickname);
    }

    // 캐릭터 슬롯 재정렬 (당기기 방식) - 애니메이션 상태 보존
    void ReorganizeCharacterSlots()
    {
        // 불필요한 슬롯만 비활성화
        for (int i = activeCharacters.Count; i < characterSlots.Length; i++)
        {
            if (characterSlots[i] != null)
            {
                characterSlots[i].SetActive(false);
            }
        }   

        // 활성 슬롯 재설정 (애니메이션 건드리지 않음)
        for (int i = 0; i < activeCharacters.Count; i++)
        {
            if (characterSlots[i] != null)
            {
                characterSlots[i].SetActive(true);
            }
            
            CharacterData charData = activeCharacters[i];
            charData.slotIndex = i; // 새로운 슬롯 인덱스 업데이트
            
            // 모든 타입 숨기기
            HideAllTypesInSlot(i);
            
            // 해당 타입만 보이기 (SetActive 없이)
            if (charactersByType[i, (int)charData.currentType] != null)
            {
                SetCharacterVisible(charactersByType[i, (int)charData.currentType], true);
            }
        }

        Debug.Log($"캐릭터 슬롯 재정렬 완료. 현재 캐릭터 수: {activeCharacters.Count}");
    }
    #endregion

    #region 디버그 및 정보
    public void PrintActiveCharacters()
    {
        Debug.Log("=== 현재 활성 캐릭터 ===");
        for (int i = 0; i < activeCharacters.Count; i++)
        {
            Debug.Log($"{i + 1}. {activeCharacters[i].nickname} Lv.{activeCharacters[i].level} - 타입: {activeCharacters[i].currentType}");
        }
        Debug.Log($"총 {activeCharacters.Count}명");
    }

    public void PrintMemoryInfo()
    {
        Debug.Log($"활성 캐릭터 수: {activeCharacters.Count}");
        Debug.Log($"UI 네임태그 수: {nameTagUIList.Count}");
        Debug.Log($"캐릭터 슬롯 수: {characterSlots.Length}");
    }
    #endregion

    #region 메모리 관리
    public void Cleanup()
    {
        if (this != null && gameObject != null)
        {
            ClearAllCharacters();
        }
        
        activeCharacters?.Clear();
        nameTagUIList?.Clear();
        Debug.Log("CharacterController 리소스 정리 완료");
    }

    void OnDestroy()
    {
        if (activeCharacters != null)
        {
            activeCharacters.Clear();
        }
        
        if (nameTagUIList != null)
        {
            foreach (GameObject nameTag in nameTagUIList)
            {
                if (nameTag != null)
                {
                    DestroyImmediate(nameTag);
                }
            }
            nameTagUIList.Clear();
        }
    }
    #endregion
}