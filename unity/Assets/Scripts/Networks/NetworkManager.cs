using System;
using System.Threading;
using System.Threading.Tasks;
using Network;
using Networks.Sessions;
using UnityEngine;

namespace Networks
{
    public class NetworkManager : MonoBehaviour
    {
        [Header("서버 셋팅")]
        [SerializeField]
        private string host = "";

        [SerializeField]
        private int port = 0;

        [SerializeField]
        private string accessToken = "";

        private Client _client;
        private Session _session;
        private NetworkApi _networkApi;

        public bool IsVerified => _session != null;
        public long? SessionId => _session?.GetSessionId();

        private async void Start()
        {
            try
            {
                DontDestroyOnLoad(gameObject);

                _client = new Client(host, port);
                await _client.ConnectAsync();

                // NetworkApi 초기화
                _networkApi = GetComponent<NetworkApi>();
                if (_networkApi == null)
                {
                    _networkApi = gameObject.AddComponent<NetworkApi>();
                }

                // 비동기 Verify 실행
                await VerifyAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"NetworkManager 초기화 에러 : {e.Message}");
            }
        }

        public void Disconnect()
        {
            try
            {
                _client?.Disconnect();
                _session = null;
                Debug.Log("NetworkManager 연결이 정상적으로 종료되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"NetworkManager 연결 종료 중 예외 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 비동기 토큰 검증 메서드
        /// </summary>
        /// <param name="maxRetries">최대 재시도 횟수 (기본값: 3)</param>
        public async Task<bool> VerifyAsync(int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.Log($"토큰 검증 시작... (시도 {attempt}/{maxRetries})");

                    // NetworkApi를 통해 비동기로 토큰 검증
                    long sessionId = await _networkApi.VerifyToken(accessToken);

                    // 세션 생성 및 저장
                    _session = new Session();
                    _session.SetSessionId(sessionId);

                    Debug.Log($"토큰 검증 성공! 세션 ID: {sessionId}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"토큰 검증 실패 (시도 {attempt}/{maxRetries}): {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        Debug.LogError("최대 재시도 횟수에 도달했습니다. 토큰 검증을 포기합니다.");
                        _session = null;
                        return false;
                    }

                    // 재시도 전 잠시 대기
                    await Task.Delay(1000 * attempt); // 1초, 2초, 3초...
                }
            }

            return false;
        }

        /// <summary>
        /// 수동으로 토큰 검증을 다시 시도합니다.
        /// </summary>
        /// <param name="newAccessToken">새로운 액세스 토큰 (null이면 기존 토큰 사용)</param>
        public async Task<bool> RetryVerifyAsync(string newAccessToken = null)
        {
            if (!string.IsNullOrEmpty(newAccessToken))
            {
                accessToken = newAccessToken;
                Debug.Log("새로운 액세스 토큰으로 업데이트되었습니다.");
            }

            return await VerifyAsync();
        }

        /// <summary>
        /// NetworkApi에서 사용할 수 있는 Send 메서드
        /// </summary>
        /// <param name="sendable">전송할 패킷</param>
        public void Send(Sendable sendable)
        {
            _client.Send(sendable);
        }

        private void OnDestroy()
        {
            try
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"NetworkManager OnDestroy 중 예외 발생: {ex.Message}");
            }
        }

        private void OnApplicationQuit()
        {
            try
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"NetworkManager OnApplicationQuit 중 예외 발생: {ex.Message}");
            }
        }
    }
}
