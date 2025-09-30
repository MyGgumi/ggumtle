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
        private readonly Features.Player.Services.PlayerManagerService _playerManagerService;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public MongdungNetworkSource(
            NetworkApi networkApi,
            Features.Player.Services.PlayerManagerService playerManagerService)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));
            _playerManagerService = playerManagerService ?? throw new ArgumentNullException(nameof(playerManagerService));

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungNetworkSource] 초기화 완료");
            }
        }

        public async UniTask<bool> SendAttackActionAsync(Vector3 position, Vector3 direction, long targetId = -1)
        {
            return await SendMongdungActionAsync(MongdungActionType.Attack, position, direction, targetId);
        }

        public async UniTask<bool> SendTrapSettingActionAsync(Vector3 position, Vector3 direction)
        {
            return await SendMongdungActionAsync(MongdungActionType.TrapSetting, position, direction);
        }

        public async UniTask<bool> SendFrightenActionAsync(Vector3 position, Vector3 direction)
        {
            return await SendMongdungActionAsync(MongdungActionType.Frighten, position, direction);
        }

        public async UniTask<bool> SendMongdungActionAsync(MongdungActionType actionType, Vector3 position, Vector3 direction, long targetId = -1)
        {
            try
            {
                if (_networkApi == null)
                {
                    Debug.LogError("[MongdungNetworkSource] NetworkApi가 null입니다");
                    return false;
                }

                // 액션 타입에 따라 적절한 NetworkApi 메서드 호출
                switch (actionType)
                {
                    case MongdungActionType.Attack:
                        // Attack은 direction과 targetId 필요
                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[MongdungNetworkSource] 몽둥이 공격 전송: Direction={direction}, TargetId={targetId}");
                        }
                        _networkApi.MongdungAttack(direction, targetId);
                        break;

                    case MongdungActionType.TrapSetting:
                        // TrapSetting: skillType = 2, 플레이어 전방 1미터 지점에 스폰
                        var playerGO = _playerManagerService?.GetLocalPlayer()?.GameObject;
                        Vector3 spawnPosition = playerGO != null
                            ? playerGO.transform.position + playerGO.transform.forward * 1f
                            : Vector3.zero;
                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[MongdungNetworkSource] 꿈틀이 설치 전송 (SkillType=2): SpawnPosition={spawnPosition}");
                        }
                        _networkApi.MongdungSkill(2, spawnPosition);
                        break;

                    case MongdungActionType.Frighten:
                        // Frighten: skillType = 1, 고정 좌표 (-1,-1,-1)
                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[MongdungNetworkSource] 위협 전송 (SkillType=1): Position=(-1,-1,-1)");
                        }
                        _networkApi.MongdungSkill(1, new Vector3(-1, -1, -1));
                        break;

                    default:
                        Debug.LogWarning($"[MongdungNetworkSource] 알 수 없는 액션 타입: {actionType}");
                        return false;
                }

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