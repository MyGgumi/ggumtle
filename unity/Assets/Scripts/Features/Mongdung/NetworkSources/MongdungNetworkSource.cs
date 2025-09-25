using System;
using Features.Mongdung.Models;
using Cysharp.Threading.Tasks;
using Networks;
using UnityEngine;
using VContainer;

namespace Features.Mongdung.NetworkSources
{
    /// <summary>
    /// 몽둥이 네트워크 소스 구현체
    /// NetworkApi를 통해 서버와 통신하고 결과를 Service 레이어에 전달
    /// </summary>
    public class MongdungNetworkSource : IMongdungNetworkSource
    {
        private readonly NetworkApi _networkApi;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public MongdungNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungNetworkSource] 초기화 완료");
            }
        }

        public async UniTask<bool> SendAttackActionAsync(Vector3 position, Vector3 direction)
        {
            return await SendMongdungActionAsync(MongdungActionType.Attack, position, direction);
        }

        public async UniTask<bool> SendTrapSettingActionAsync(Vector3 position, Vector3 direction)
        {
            return await SendMongdungActionAsync(MongdungActionType.TrapSetting, position, direction);
        }

        public async UniTask<bool> SendFrightenActionAsync(Vector3 position, Vector3 direction)
        {
            return await SendMongdungActionAsync(MongdungActionType.Frighten, position, direction);
        }

        public async UniTask<bool> SendMongdungActionAsync(MongdungActionType actionType, Vector3 position, Vector3 direction)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[MongdungNetworkSource] 몽둥이 액션 전송: ActionType={actionType}, Position={position}, Direction={direction}"
                    );
                }

                // TODO: NetworkApi에 몽둥이 액션 메서드 추가 필요
                // 현재는 임시로 성공 반환
                await UniTask.Delay(100); // 네트워크 지연 시뮬레이션

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungNetworkSource] 몽둥이 액션 전송 완료: ActionType={actionType}");
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MongdungNetworkSource] 몽둥이 액션 전송 실패: ActionType={actionType}, Error={e.Message}");
                return false;
            }
        }

        public async UniTask<bool> CancelMongdungActionAsync(MongdungActionType actionType)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungNetworkSource] 몽둥이 액션 취소 요청: ActionType={actionType}");
                }

                // TODO: NetworkApi에 액션 취소 메서드 추가 필요
                await UniTask.Delay(50);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungNetworkSource] 몽둥이 액션 취소 완료: ActionType={actionType}");
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MongdungNetworkSource] 몽둥이 액션 취소 실패: ActionType={actionType}, Error={e.Message}");
                return false;
            }
        }

        public async UniTask<bool> UpdateMongdungStateAsync(MongdungState state)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungNetworkSource] 몽둥이 상태 업데이트: State={state}");
                }

                // TODO: NetworkApi에 상태 업데이트 메서드 추가 필요
                await UniTask.Delay(50);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungNetworkSource] 몽둥이 상태 업데이트 완료: State={state}");
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MongdungNetworkSource] 몽둥이 상태 업데이트 실패: State={state}, Error={e.Message}");
                return false;
            }
        }
    }
}