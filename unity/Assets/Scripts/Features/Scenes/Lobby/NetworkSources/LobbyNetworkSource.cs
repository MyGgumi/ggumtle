using System;
using Cysharp.Threading.Tasks;
using Networks;
using Networks.Rooms;
using Networks.Sessions;
using UnityEngine;
using VContainer;

namespace Features.Scenes.Lobby.NetworkSources
{
    /// <summary>
    /// 로비 관련 네트워크 통신 구현체
    /// NetworkApi를 통해 서버와 통신하고 결과를 반환
    /// </summary>
    public class LobbyNetworkSource : ILobbyNetworkSource
    {
        private readonly NetworkApi _networkApi;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public LobbyNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            if (_enableDebugLogs)
            {
                Debug.Log("[LobbyNetworkSource] 초기화 완료");
            }
        }

        public async UniTask<VerifyTokenCommand> VerifyTokenAsync(string accessToken)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[LobbyNetworkSource] 토큰 검증 시작: {accessToken}");
                }

                var result = await _networkApi.VerifyToken(accessToken).AsUniTask();

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[LobbyNetworkSource] 토큰 검증 결과: Success={result.Success}, SessionId={result.SessionId}"
                    );
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyNetworkSource] 토큰 검증 실패: {e.Message}");
                throw;
            }
        }

        public async UniTask<RoomJoinCommand> JoinRoomAsync(int roomId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[LobbyNetworkSource] 방 입장 시작: RoomId={roomId}");
                }

                var result = await _networkApi.RoomJoin(roomId).AsUniTask();

                if (_enableDebugLogs)
                {
                    Debug.Log($"[LobbyNetworkSource] 방 입장 결과: Result={result.Result}");
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyNetworkSource] 방 입장 실패: {e.Message}");
                throw;
            }
        }

        public async UniTask InitializeNetworkAsync(string host, int port)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[LobbyNetworkSource] 네트워크 초기화 시작: {host}:{port}");
                }

                await _networkApi.InitializeNetwork(host, port).AsUniTask();

                if (_enableDebugLogs)
                {
                    Debug.Log("[LobbyNetworkSource] 네트워크 초기화 완료");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyNetworkSource] 네트워크 초기화 실패: {e.Message}");
                throw;
            }
        }
    }
}
