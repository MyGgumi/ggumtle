using System;
using Cysharp.Threading.Tasks;
using Features.ItemUsage.Messages;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.ItemUsage.Managers
{
    /// <summary>
    /// 맵에 직접 아이템 이펙트를 스폰하는 매니저
    /// 플레이어와 독립적으로 좌표 기반 이펙트 생성
    /// </summary>
    public class ItemEffectManager : MonoBehaviour, IStartable, IDisposable
    {
        #region Constants

        private const string ITEM_EFFECT_PREFAB_KEY = "item_use_flash_effect";
        private const float EFFECT_DURATION = 3.0f; // 3초 후 자동 제거

        #endregion

        #region Dependencies

        private readonly ISubscriber<TaserGunUsedMessage> _taserSubscriber;
        private readonly ISubscriber<FlashBangUsedMessage> _flashBangSubscriber;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor

        [Inject]
        public ItemEffectManager(
            ISubscriber<TaserGunUsedMessage> taserSubscriber,
            ISubscriber<FlashBangUsedMessage> flashBangSubscriber)
        {
            _taserSubscriber = taserSubscriber;
            _flashBangSubscriber = flashBangSubscriber;
        }

        #endregion

        #region IStartable

        public void Start()
        {
            SubscribeToItemMessages();

            if (_enableDebugLogs)
            {
                Debug.Log("[ItemEffectManager] 초기화 완료 - 아이템 이펙트 메시지 구독 시작");
            }
        }

        #endregion

        #region Unity Lifecycle

        void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Private Methods

        private void SubscribeToItemMessages()
        {
            // 테이저건 사용 메시지 구독
            _taserSubscriber
                .Subscribe(OnTaserGunUsed)
                .AddTo(_disposables);

            // 섬광탄 사용 메시지 구독
            _flashBangSubscriber
                .Subscribe(OnFlashBangUsed)
                .AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[ItemEffectManager] 아이템 사용 메시지 구독 완료");
            }
        }

        private async void OnTaserGunUsed(TaserGunUsedMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemEffectManager] 테이저건 이펙트 요청: Position={message.effectPosition}, Success={message.success}");
                }

                await SpawnItemEffectAtPosition(message.effectPosition);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemEffectManager] 테이저건 이펙트 생성 실패: {e.Message}");
            }
        }

        private async void OnFlashBangUsed(FlashBangUsedMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemEffectManager] 섬광탄 이펙트 요청: Position={message.effectPosition}, Success={message.success}");
                }

                await SpawnItemEffectAtPosition(message.effectPosition);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemEffectManager] 섬광탄 이펙트 생성 실패: {e.Message}");
            }
        }

        private async UniTask SpawnItemEffectAtPosition(Vector3 position)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemEffectManager] 이펙트 생성 시작: Position={position}");
                }

                // Resources에서 이펙트 프리팹 로드 및 인스턴스화
                var effectPrefab = Resources.Load<GameObject>(ITEM_EFFECT_PREFAB_KEY);
                if (effectPrefab == null)
                {
                    Debug.LogError($"[ItemEffectManager] 이펙트 프리팹을 찾을 수 없음: {ITEM_EFFECT_PREFAB_KEY}");
                    return;
                }

                var effectInstance = Instantiate(effectPrefab, position, Quaternion.identity);

                if (effectInstance != null)
                {
                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[ItemEffectManager] 이펙트 생성 성공: {effectInstance.name} at {position}");
                    }

                    // ParticleSystem 컴포넌트 찾아서 명시적으로 재생
                    var particleSystem = effectInstance.GetComponent<ParticleSystem>();
                    if (particleSystem != null)
                    {
                        particleSystem.Play();
                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[ItemEffectManager] ParticleSystem.Play() 호출 완료");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ItemEffectManager] ParticleSystem 컴포넌트를 찾을 수 없음: {effectInstance.name}");
                    }

                    // 일정 시간 후 자동 제거
                    _ = AutoDestroyEffect(effectInstance);
                }
                else
                {
                    Debug.LogError($"[ItemEffectManager] 이펙트 생성 실패: null 인스턴스");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemEffectManager] 이펙트 생성 중 오류: {e.Message}");
            }
        }

        private async UniTask AutoDestroyEffect(GameObject effectInstance)
        {
            try
            {
                // 지정된 시간 대기
                await UniTask.Delay(TimeSpan.FromSeconds(EFFECT_DURATION));

                // 인스턴스가 여전히 유효한지 확인
                if (effectInstance != null)
                {
                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[ItemEffectManager] 이펙트 자동 제거: {effectInstance.name}");
                    }

                    // GameObject 제거
                    Destroy(effectInstance);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemEffectManager] 이펙트 자동 제거 실패: {e.Message}");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _disposables?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[ItemEffectManager] Dispose 완료");
            }
        }

        #endregion
    }
}