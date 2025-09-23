using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

namespace Features.Map.Services
{
    /// <summary>
    /// Addressable 에셋 로딩 서비스 구현체
    /// </summary>
    public class AddressableLoadServiceImpl : IAddressableLoadService
    {
        private readonly bool _enableDebugLogs = false;
        private readonly List<AsyncOperationHandle> _loadedOperations = new();
        private readonly Dictionary<string, List<UnityEngine.Object>> _loadedAssetsByTag = new();
        private readonly IObjectResolver _resolver;

        public event Action<float> OnLoadProgress;
        public event Action<string, UnityEngine.Object> OnAssetLoaded;
        public event Action OnAllAssetsLoaded;

        [Inject]
        public AddressableLoadServiceImpl(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

            if (_enableDebugLogs)
                Debug.Log("[AddressableLoadService] VContainer 연동 초기화 완료");
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

        /// <summary>
        /// VContainer 의존성 주입을 포함한 Addressable 인스턴스 생성
        /// </summary>
        public async UniTask<T> SpawnWithInjectionAsync<T>(
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null
        ) where T : MonoBehaviour
        {
            try
            {
                var instance = await InstantiateAsync(key, position, rotation, parent);

                // VContainer 의존성 주입
                try
                {
                    _resolver.InjectGameObject(instance);
                    if (_enableDebugLogs)
                        Debug.Log($"[AddressableLoadService] VContainer 의존성 주입 성공: {key}");
                }
                catch (System.Exception injectionEx)
                {
                    Debug.LogWarning($"[AddressableLoadService] VContainer 의존성 주입 실패 (계속 진행): {key}, 오류: {injectionEx.Message}");
                }

                var component = instance.GetComponent<T>();
                if (component == null)
                {
                    Debug.LogError($"[AddressableLoadService] 컴포넌트 {typeof(T).Name}를 찾을 수 없음: {key}");
                    ReleaseInstance(instance);
                    return null;
                }

                if (_enableDebugLogs)
                    Debug.Log($"[AddressableLoadService] VContainer 주입 완료: {key} -> {typeof(T).Name}");

                return component;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableLoadService] VContainer 주입 실패: {key}, {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 여러 인스턴스를 한번에 생성하고 VContainer 의존성 주입
        /// </summary>
        public async UniTask<List<T>> SpawnMultipleAsync<T>(
            string key,
            List<Vector3> positions,
            Transform parent = null
        ) where T : MonoBehaviour
        {
            var results = new List<T>();

            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[AddressableLoadService] 대량 생성 시작: {key}, 수량: {positions.Count}");

                for (int i = 0; i < positions.Count; i++)
                {
                    var component = await SpawnWithInjectionAsync<T>(key, positions[i], Quaternion.identity, parent);
                    if (component != null)
                    {
                        results.Add(component);
                    }

                    // 한 프레임 대기 (성능 분산)
                    if (i % 5 == 4) // 5개마다 대기
                    {
                        await UniTask.Yield();
                    }
                }

                if (_enableDebugLogs)
                    Debug.Log($"[AddressableLoadService] 대량 생성 완료: {key}, 성공: {results.Count}/{positions.Count}");

                return results;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableLoadService] 대량 생성 실패: {key}, {e.Message}");

                // 실패시 생성된 인스턴스들 정리
                foreach (var result in results)
                {
                    if (result != null)
                        ReleaseInstance(result.gameObject);
                }

                throw;
            }
        }

        /// <summary>
        /// AssetReference를 사용한 VContainer 의존성 주입 생성
        /// </summary>
        public async UniTask<T> SpawnWithInjectionAsync<T>(
            AssetReference assetReference,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null
        ) where T : MonoBehaviour
        {
            try
            {
                var instance = await InstantiateAsync(assetReference, position, rotation, parent);

                // VContainer 의존성 주입
                try
                {
                    _resolver.InjectGameObject(instance);
                    if (_enableDebugLogs)
                        Debug.Log($"[AddressableLoadService] VContainer 의존성 주입 성공: {assetReference.RuntimeKey}");
                }
                catch (System.Exception injectionEx)
                {
                    Debug.LogWarning($"[AddressableLoadService] VContainer 의존성 주입 실패 (계속 진행): {assetReference.RuntimeKey}, 오류: {injectionEx.Message}");
                }

                var component = instance.GetComponent<T>();
                if (component == null)
                {
                    Debug.LogError($"[AddressableLoadService] 컴포넌트 {typeof(T).Name}를 찾을 수 없음: {assetReference.RuntimeKey}");
                    ReleaseInstance(instance);
                    return null;
                }

                if (_enableDebugLogs)
                    Debug.Log($"[AddressableLoadService] AssetReference VContainer 주입 완료: {assetReference.RuntimeKey} -> {typeof(T).Name}");

                return component;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableLoadService] AssetReference VContainer 주입 실패: {assetReference.RuntimeKey}, {e.Message}");
                throw;
            }
        }
    }
}
