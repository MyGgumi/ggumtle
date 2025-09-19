using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Features.Map.Services
{
    /// <summary>
    /// Addressable 에셋 로딩을 담당하는 서비스 인터페이스
    /// </summary>
    public interface IAddressableLoadService
    {
        /// <summary>
        /// 로딩 진행률 이벤트 (0.0 ~ 1.0)
        /// </summary>
        event Action<float> OnLoadProgress;

        /// <summary>
        /// 에셋 로딩 완료 이벤트
        /// </summary>
        event Action<string, UnityEngine.Object> OnAssetLoaded;

        /// <summary>
        /// 모든 에셋 로딩 완료 이벤트
        /// </summary>
        event Action OnAllAssetsLoaded;

        /// <summary>
        /// 태그로 에셋들 로딩
        /// </summary>
        /// <param name="tag">로딩할 에셋 태그 (예: "InGame")</param>
        /// <param name="onProgress">진행률 콜백</param>
        /// <returns>로딩 완료까지 대기하는 Task</returns>
        UniTask LoadAssetsWithTagAsync(string tag, Action<float> onProgress = null);

        /// <summary>
        /// 단일 에셋 로딩
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="key">에셋 키 또는 주소</param>
        /// <returns>로딩된 에셋</returns>
        UniTask<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object;

        /// <summary>
        /// AssetReference로 에셋 로딩
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="assetReference">AssetReference</param>
        /// <returns>로딩된 에셋</returns>
        UniTask<T> LoadAssetAsync<T>(AssetReference assetReference) where T : UnityEngine.Object;

        /// <summary>
        /// 프리팹 인스턴스화
        /// </summary>
        /// <param name="key">프리팹 키</param>
        /// <param name="position">생성 위치</param>
        /// <param name="rotation">생성 회전</param>
        /// <param name="parent">부모 Transform</param>
        /// <returns>생성된 GameObject</returns>
        UniTask<GameObject> InstantiateAsync(string key, Vector3 position, Quaternion rotation, Transform parent = null);

        /// <summary>
        /// AssetReference로 프리팹 인스턴스화
        /// </summary>
        /// <param name="assetReference">AssetReference</param>
        /// <param name="position">생성 위치</param>
        /// <param name="rotation">생성 회전</param>
        /// <param name="parent">부모 Transform</param>
        /// <returns>생성된 GameObject</returns>
        UniTask<GameObject> InstantiateAsync(AssetReference assetReference, Vector3 position, Quaternion rotation, Transform parent = null);

        /// <summary>
        /// 특정 에셋 해제
        /// </summary>
        /// <param name="asset">해제할 에셋</param>
        void ReleaseAsset(UnityEngine.Object asset);

        /// <summary>
        /// 인스턴스 해제
        /// </summary>
        /// <param name="instance">해제할 인스턴스</param>
        void ReleaseInstance(GameObject instance);

        /// <summary>
        /// 모든 로딩된 에셋 해제
        /// </summary>
        void ReleaseAll();

        /// <summary>
        /// 특정 태그의 에셋들만 해제
        /// </summary>
        /// <param name="tag">해제할 에셋 태그</param>
        void ReleaseAssetsWithTag(string tag);

        /// <summary>
        /// 현재 로딩된 에셋 개수 가져오기
        /// </summary>
        /// <returns>로딩된 에셋 개수</returns>
        int GetLoadedAssetCount();

        /// <summary>
        /// 메모리 사용량 정보 가져오기
        /// </summary>
        /// <returns>메모리 사용량 (바이트)</returns>
        long GetMemoryUsage();
    }
}