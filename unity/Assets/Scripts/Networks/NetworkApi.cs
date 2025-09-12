using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Network;
using Networks.Packets;
using Networks.Rooms;
using Networks.Scenes;
using Networks.Sessions;
using UnityEngine;

namespace Networks
{
    public class NetworkApi : MonoBehaviour
    {
        [Header("Client")]
        [SerializeField] private Client client;
        
        // 비동기 응답 처리를 위한 TaskCompletionSource 딕셔너리
        private readonly ConcurrentDictionary<PacketType, TaskCompletionSource<object>> _pendingRequests = new();
        
        // 싱글톤 인스턴스
        public static NetworkApi Instance { get; private set; }

        private void Awake()
        {
            Debug.Log("NetworkApi Awake");

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            Debug.Log("NetworkApi Start");

            if (!client.IsConnected)
            {
                Debug.Log("Client 연결 안 되어 있음");
            }
        }

        public async Task InitializeNetwork(string host, int port)
        {
            Debug.Log("NetworkApi InitializeNetwork");

            await client.ConnectAsync(host, port);
        }

        public async Task<RoomJoinCommand> RoomJoin(long roomId)
        {
            try
            {
                Debug.Log($"RoomJoin 시작: {roomId}");
                
                // Client 연결 상태 확인
                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("Client가 연결되지 않았습니다.");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }
                
                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.RoomJoinResponse] = tcs;
                
                Debug.Log($"TaskCompletionSource 등록됨: {PacketType.RoomJoinResponse}");
                Debug.Log($"현재 대기 중인 요청 수: {_pendingRequests.Count}");

                var roomJoinRequest = new RoomJoinSend(roomId);
                client.Send(roomJoinRequest);
                
                Debug.Log($"RoomJoin 요청 전송: {roomId}");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() => {
                    Debug.LogWarning("RoomJoin 타임아웃 발생");
                    tcs.TrySetCanceled();
                });
                
                Debug.Log("RoomJoin 응답 대기 시작...");
                var response = await tcs.Task;
                Debug.Log("RoomJoin 응답 수신됨");

                if (response is RoomJoinCommand roomJoinResponse)
                {
                    Debug.Log($"RoomJoin 응답 수신: Result = {roomJoinResponse.Result}, Success = {roomJoinResponse.Success}");
                    return roomJoinResponse;
                }

                throw new InvalidOperationException("RoomJoin 예상하지 못한 응답 타입입니다.");
            }
            catch (TimeoutException)
            {
                Debug.LogError("RoomJoin 요청이 타임아웃되었습니다.");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"RoomJoin 요청 중 오류 발생: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.RoomJoinResponse, out _);
            }
        }
        
        /// <summary>
        /// 토큰 검증을 비동기로 수행합니다.
        /// </summary>
        /// <param name="accessToken">검증할 액세스 토큰</param>
        /// <returns>검증 성공 시 세션 ID를 반환합니다.</returns>
        public async Task<VerifyTokenCommand> VerifyToken(string accessToken)
        {
            try
            {
                Debug.Log($"VerifyToken 시작: {accessToken}");
                
                // TaskCompletionSource 생성
                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.VerifyTokenResponse] = tcs;
                
                Debug.Log($"TaskCompletionSource 등록됨: {PacketType.VerifyTokenResponse}");
                Debug.Log($"현재 대기 중인 요청 수: {_pendingRequests.Count}");
                
                // Client 연결 상태 확인
                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("Client가 연결되지 않았습니다.");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }
                
                // 요청 전송
                var verifyTokenRequest = new VerifyTokenSend(accessToken);
                client.Send(verifyTokenRequest);
                
                Debug.Log($"VerifyToken 요청 전송: {accessToken}");
                
                // 응답 대기 (타임아웃 10초)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() => {
                    Debug.LogWarning("VerifyToken 타임아웃 발생");
                    tcs.TrySetCanceled();
                });
                
                Debug.Log("응답 대기 시작...");
                var response = await tcs.Task;
                Debug.Log("응답 수신됨");
                
                if (response is VerifyTokenCommand verifyTokenResponse)
                {
                    Debug.Log($"VerifyToken 응답 수신: SessionId = {verifyTokenResponse.SessionId}");
                    return verifyTokenResponse;
                }
                
                throw new InvalidOperationException("VerifyToken 예상하지 못한 응답 타입입니다.");
            }
            catch (TimeoutException)
            {
                Debug.LogError("VerifyToken 요청이 타임아웃되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"VerifyToken 요청 중 오류 발생: {ex.Message}");
                throw;
            }
            finally
            {
                // TaskCompletionSource 정리
                _pendingRequests.TryRemove(PacketType.VerifyTokenResponse, out _);
            }
        }
        
        /// <summary>
        /// 씬 전환 완료 알림
        /// </summary>
        /// <returns>씬 전환 응답</returns>
        public async Task<SceneChangeCommand> SceneChange()
        {
            try
            {
                Debug.Log("SceneChange 시작");
                
                // Client 연결 상태 확인
                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("Client가 연결되지 않았습니다.");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }
                
                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.SceneChangeResponse] = tcs;
                
                Debug.Log($"TaskCompletionSource 등록됨: {PacketType.SceneChangeResponse}");
                Debug.Log($"현재 대기 중인 요청 수: {_pendingRequests.Count}");

                var sceneChangeRequest = new SceneChangeSend();
                client.Send(sceneChangeRequest);
                
                Debug.Log("SceneChange 요청 전송");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() => {
                    Debug.LogWarning("SceneChange 타임아웃 발생");
                    tcs.TrySetCanceled();
                });
                
                Debug.Log("SceneChange 응답 대기 시작...");
                var response = await tcs.Task;
                Debug.Log("SceneChange 응답 수신됨");

                if (response is SceneChangeCommand sceneChangeResponse)
                {
                    Debug.Log($"SceneChange 응답 수신: Result = {sceneChangeResponse.Result}, Success = {sceneChangeResponse.Success}");
                    return sceneChangeResponse;
                }

                throw new InvalidOperationException("SceneChange 예상하지 못한 응답 타입입니다.");
            }
            catch (TimeoutException)
            {
                Debug.LogError("SceneChange 요청이 타임아웃되었습니다.");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"SceneChange 요청 중 오류 발생: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.SceneChangeResponse, out _);
            }
        }
        
        /// <summary>
        /// CommandDispatcher에서 호출되는 응답 처리 메서드
        /// </summary>
        /// <param name="command">수신된 명령어</param>
        public void HandleResponse(Command command)
        {
            Debug.Log($"HandleResponse 호출됨: {command.Type}");
            Debug.Log($"대기 중인 요청 수: {_pendingRequests.Count}");
            
            if (_pendingRequests.TryGetValue(command.Type, out var tcs))
            {
                Debug.Log($"TaskCompletionSource 찾음: {command.Type}");
                tcs.SetResult(command);
                Debug.Log($"응답 처리 완료: {command.Type}");
            }
            else
            {
                Debug.LogWarning($"대기 중인 요청이 없습니다: {command.Type}");
                Debug.LogWarning($"현재 대기 중인 요청들:");
                foreach (var kvp in _pendingRequests)
                {
                    Debug.LogWarning($"  - {kvp.Key}");
                }
            }
        }
    }
}