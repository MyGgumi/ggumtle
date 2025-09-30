using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AnimationPrefab
{
    public string name;
    public GameObject prefab;
}

public class PreloadAni : MonoBehaviour
{
    public List<AnimationPrefab> animationPrefabs = new List<AnimationPrefab>();
    private List<GameObject> preloadedObjects = new List<GameObject>();
    
    void Awake()
    {
        // 씬 전환해도 파괴되지 않게
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        PreloadAllPrefabs();
    }
    
    void PreloadAllPrefabs()
    {
        foreach (var item in animationPrefabs)
        {
            if (item.prefab != null)
            {
                GameObject obj = Instantiate(item.prefab);
                obj.SetActive(false);
                
                // 프리로드된 오브젝트도 씬 전환 시 유지
                DontDestroyOnLoad(obj);
                
                preloadedObjects.Add(obj);
                
                Debug.Log($"Preloaded: {item.name}");
            }
            else
            {
                Debug.Log($"Prefab '{item.name}' is null, skipping...");
            }
        }
        
        Debug.Log($"Total {preloadedObjects.Count} prefabs preloaded!");
    }
    
    // OnDestroy 삭제 - 게임 종료될 때까지 유지
}