using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Network;
using Networks.Packets;
using Networks.Sessions;
using UnityEngine;

namespace Networks
{
    public class NetworkApi : MonoBehaviour
    {
        private NetworkManager _networkManager;
        
        // 비동기 응답 처리를 위한 TaskCompletionSource 딕셔너리
        private readonly ConcurrentDictionary<PacketType, TaskCompletionSource<object>> _pendingRequests = new();
        
        // 싱글톤 인스턴스
        private static NetworkApi _instance;
        public static NetworkApi Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<NetworkApi>();
                    if (_instance == null)
                    {
                        Debug.LogError("NetworkApi 인스턴스를 찾을 수 없습니다.");
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _networkManager = GetComponent<NetworkManager>();
        }
        
        /// <summary>
        /// 토큰 검증을 비동기로 수행합니다.
        /// </summary>
        /// <param name="accessToken">검증할 액세스 토큰</param>
        /// <returns>검증 성공 시 세션 ID를 반환합니다.</returns>
        public async Task<long> VerifyToken(string accessToken)
        {
            try
            {
                // TaskCompletionSource 생성
                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.VerifyTokenResponse] = tcs;
                
                // 요청 전송
                var verifyTokenRequest = new VerifyTokenSend(accessToken);
                _networkManager.Send(verifyTokenRequest);
                
                Debug.Log($"VerifyToken 요청 전송: {accessToken}");
                
                // 응답 대기 (타임아웃 10초)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() => tcs.TrySetCanceled());
                
                var response = await tcs.Task;
                
                if (response is VerifyTokenCommand verifyTokenResponse)
                {
                    Debug.Log($"VerifyToken 응답 수신: SessionId = {verifyTokenResponse.SessionId}");
                    return verifyTokenResponse.SessionId;
                }
                
                throw new InvalidOperationException("예상하지 못한 응답 타입입니다.");
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
        /// CommandDispatcher에서 호출되는 응답 처리 메서드
        /// </summary>
        /// <param name="command">수신된 명령어</param>
        public void HandleResponse(Command command)
        {
            if (_pendingRequests.TryGetValue(command.Type, out var tcs))
            {
                tcs.SetResult(command);
                Debug.Log($"응답 처리 완료: {command.Type}");
            }
            else
            {
                Debug.LogWarning($"대기 중인 요청이 없습니다: {command.Type}");
            }
        }
    }
}