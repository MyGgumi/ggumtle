using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Features.Map.Services
{
    /// <summary>
    /// Addressable 에셋 로딩 서비스 구현체
    /// </summary>
    public class AddressableLoadServiceImpl : IAddressableLoadService
    {
        private readonly bool _enableDebugLogs = true;
        private readonly List<AsyncOperationHandle> _loadedOperations = new();
        private readonly Dictionary<string, List<UnityEngine.Object>> _loadedAssetsByTag = new();

        public event Action<float> OnLoadProgress;
        public event Action<string, UnityEngine.Object> OnAssetLoaded;
        public event Action OnAllAssetsLoaded;

        public AddressableLoadServiceImpl()
        {
            if (_enableDebugLogs)
                Debug.Log("[AddressableLoadService] 초기화 완료");
        }

        public async UniTask LoadAssetsWithTagAsync(string tag, Action<float> onProgress = null)
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[AddressableLoadService] 태그로 에셋 로딩 시작: {tag}");

                var locations = await Addressables.LoadResourceLocationsAsync(tag);

                if (locations.Count == 0)
                {
                    Debug.LogWarning(
                        $"[AddressableLoadService] 태그에 해당하는 에셋이 없음: {tag}"
                    );
                    OnAllAssetsLoaded?.Invoke();
                    return;
                }

                if (_enableDebugLogs)
                    Debug.Log($"[AddressableLoadService] 로딩할 에셋 개수: {locations.Count}");

                var loadedAssets = new List<UnityEngine.Object>();
                float totalCount = locations.Count;

                for (int i = 0; i < locations.Count; i++)
                {
                    var location = locations[i];

                    try
                    {
                        var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(location);
                        _loadedOperations.Add(handle);

                        var asset = await handle;

                        if (asset != null)
                        {
                            loadedAssets.Add(asset);
                            OnAssetLoaded?.Invoke(location.PrimaryKey, asset);

                            if (_enableDebugLogs)
                                Debug.Log(
                                    $"[AddressableLoadService] 에셋 로딩 완료: {location.PrimaryKey}"
                                );
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"[AddressableLoadService] 에셋 로딩 실패: {location.PrimaryKey}, {e.Message}"
                        );
                    }

                    // 진행률 업데이트
                    float progress = (i + 1) / totalCount;
                    onProgress?.Invoke(progress);
                    OnLoadProgress?.Invoke(progress);

                    // 한 프레임 대기 (UI 업데이트를 위해)
                    await UniTask.Yield();
                }

                // 태그별로 로딩된 에셋 저장
                _loadedAssetsByTag[tag] = loadedAssets;

                if (_enableDebugLogs)
                    Debug.Log(
                        $"[AddressableLoadService] 태그 로딩 완료: {tag}, 로딩된 에셋: {loadedAssets.Count}개"
                    );

                OnAllAssetsLoaded?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableLoadService] 태그 로딩 중 오류: {tag}, {e.Message}");
                throw;
            }
        }

        public async UniTask<T> LoadAssetAsync<T>(string key)
            where T : UnityEngine.Object
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                _loadedOperations.Add(handle);

                var asset = await handle;

                if (_enableDebugLogs && asset != null)
                    Debug.Log($"[AddressableLoadService] 단일 에셋 로딩 완료: {key}");

                return asset;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableLoadService] 단일 에셋 로딩 실패: {key}, {e.Message}");
                throw;
            }
        }

        public async UniTask<T> LoadAssetAsync<T>(AssetReference assetReference)
            where T : UnityEngine.Object
        {
            try
            {
                var handle = assetReference.LoadAssetAsync<T>();
                _loadedOperations.Add(handle);

                var asset = await handle;

                if (_enableDebugLogs && asset != null)
                    Debug.Log(
                        $"[AddressableLoadService] AssetReference 로딩 완료: {assetReference.RuntimeKey}"
                    );

                return asset;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[AddressableLoadService] AssetReference 로딩 실패: {assetReference.RuntimeKey}, {e.Message}"
                );
                throw;
            }
        }

        public async UniTask<GameObject> InstantiateAsync(
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null
        )
        {
            try
            {
                var handle = Addressables.InstantiateAsync(key, position, rotation, parent);
                _loadedOperations.Add(handle);

                var instance = await handle;

                if (_enableDebugLogs && instance != null)
                    Debug.Log($"[AddressableLoadService] 인스턴스 생성 완료: {key}");

                return instance;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableLoadService] 인스턴스 생성 실패: {key}, {e.Message}");
                throw;
            }
        }

        public async UniTask<GameObject> InstantiateAsync(
            AssetReference assetReference,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null
        )
        {
            try
            {
                var handle = assetReference.InstantiateAsync(position, rotation, parent);
                _loadedOperations.Add(handle);

                var instance = await handle;

                if (_enableDebugLogs && instance != null)
                    Debug.Log(
                        $"[AddressableLoadService] AssetReference 인스턴스 생성 완료: {assetReference.RuntimeKey}"
                    );

                return instance;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[AddressableLoadService] AssetReference 인스턴스 생성 실패: {assetReference.RuntimeKey}, {e.Message}"
                );
                throw;
            }
        }

        public void ReleaseAsset(UnityEngine.Object asset)
        {
            if (asset != null)
            {
                Addressables.Release(asset);

                if (_enableDebugLogs)
                    Debug.Log($"[AddressableLoadService] 에셋 해제: {asset.name}");
            }
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance != null)
            {
                Addressables.ReleaseInstance(instance);

                if (_enableDebugLogs)
                    Debug.Log($"[AddressableLoadService] 인스턴스 해제: {instance.name}");
            }
        }

        public void ReleaseAll()
        {
            foreach (var handle in _loadedOperations)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _loadedOperations.Clear();
            _loadedAssetsByTag.Clear();

            if (_enableDebugLogs)
                Debug.Log("[AddressableLoadService] 모든 에셋 해제 완료");
        }

        public void ReleaseAssetsWithTag(string tag)
        {
            if (_loadedAssetsByTag.TryGetValue(tag, out var assets))
            {
                foreach (var asset in assets)
                {
                    ReleaseAsset(asset);
                }

                _loadedAssetsByTag.Remove(tag);

                if (_enableDebugLogs)
                    Debug.Log($"[AddressableLoadService] 태그 에셋 해제 완료: {tag}");
            }
        }

        public int GetLoadedAssetCount()
        {
            return _loadedOperations.Count;
        }

        public long GetMemoryUsage()
        {
            // Addressables의 메모리 사용량을 정확히 계산하기는 어려우므로
            // 근사치 또는 Unity Profiler API 사용
            return GC.GetTotalMemory(false);
        }
    }
}
