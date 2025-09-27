using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Network;
using Networks.chests;
using Networks.Chests;
using Networks.Game;
using Networks.Ggumtle;
using Networks.Packets;
using Networks.Players;
using Networks.Rooms;
using Networks.Scenes;
using Networks.Sessions;
using UnityEngine;

namespace Networks
{
    public class NetworkApi : MonoBehaviour
    {
        [Header("Client")]
        [SerializeField]
        private Client client;

        // 비동기 응답 처리를 위한 TaskCompletionSource 딕셔너리
        private readonly ConcurrentDictionary<
            PacketType,
            TaskCompletionSource<object>
        > _pendingRequests = new();

        // 싱글톤 인스턴스
        public static NetworkApi Instance { get; private set; }

        private void Awake()
        {
            Debug.Log("[NetworkApi] 초기화 시작");

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            Debug.Log("[NetworkApi] 시작");

            if (client == null)
            {
                Debug.LogWarning("[NetworkApi] Client가 설정되지 않았습니다. Inspector에서 Client를 설정해주세요.");
                return;
            }

            if (!client.IsConnected)
            {
                Debug.LogWarning("[NetworkApi] 클라이언트 연결 상태 확인 필요");
            }
        }

        public async Task InitializeNetwork(string host, int port)
        {
            Debug.Log("[NetworkApi] 네트워크 초기화 시작");

            await client.ConnectAsync(host, port);
        }

        public async Task<DiggingStartCommand> DiggingStart(int ggumtleId)
        {
            try
            {
                Debug.Log($"[NetworkApi] 꿈틀이 파기 시작: GgumtleId={ggumtleId}");

                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.DiggingStartResponse] = tcs;

                var diggingStartRequest = new DiggingStartSend(ggumtleId);
                client.Send(diggingStartRequest);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogError("[NetworkApi] 꿈틀이 파기 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                var response = await tcs.Task;

                if (response is DiggingStartCommand command)
                {
                    return command;
                }

                throw new InvalidOperationException("DiggingStart에서 예상치 못 한 응답 타입니다.");
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 꿈틀이 파기 요청 타임아웃");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 꿈틀이 파기 패킷 전송 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.DiggingStartResponse, out _);
            }
        }

        public async Task<JellyStartCommand> JellyStart(int ggumtleId)
        {
            try
            {
                Debug.Log($"[NetworkApi] 젤리 시작: GgumtleId={ggumtleId}");

                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.JellyStartResponse] = tcs;

                var jellyStartRequest = new JellyStartSend(ggumtleId);
                client.Send(jellyStartRequest);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogError("[NetworkApi] 젤리 시작 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                var response = await tcs.Task;

                if (response is JellyStartCommand command)
                {
                    return command;
                }

                throw new InvalidOperationException("JellyStart에서 예상치 못한 응답 타입입니다.");
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 젤리 시작 요청 타임아웃");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 젤리 시작 패킷 전송 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.JellyStartResponse, out _);
            }
        }

        public void JellyQuit()
        {
            try
            {
                Debug.Log("[NetworkApi] 젤리 종료 시작");

                var jellyQuitRequest = new JellyQuitSend();
                client.Send(jellyQuitRequest);

            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 젤리 종료 패킷 전송 실패: {e.Message}");
                throw;
            }
        }

        public async Task<DiggingQuitCommand> DiggingQuit()
        {
            try
            {
                Debug.Log("[NetworkApi] 꿈틀이 파기 종료 시작");

                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.DiggingQuitResponse] = tcs;

                var diggingQuitRequest = new DiggingQuitSend();
                client.Send(diggingQuitRequest);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogError("[NetworkApi] 꿈틀이 파기 종료 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                var response = await tcs.Task;

                if (response is DiggingQuitCommand command)
                {
                    return command;
                }

                throw new InvalidOperationException("DiggingQuit에서 예상치 못 한 응답 타입니다.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 꿈틀이 파기 종료 패킷 전송 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.DiggingQuitResponse, out _);
            }
        }

        public async Task<RoomJoinCommand> RoomJoin(long roomId)
        {
            try
            {
                Debug.Log($"[NetworkApi] 방 조인 시작: RoomId={roomId}");

                // Client 연결 상태 확인
                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.RoomJoinResponse] = tcs;

                Debug.Log($"[NetworkApi] 응답 대기 등록: {PacketType.RoomJoinResponse}");
                Debug.Log($"[NetworkApi] 대기 중인 요청 수: {_pendingRequests.Count}");

                var roomJoinRequest = new RoomJoinSend(roomId);
                client.Send(roomJoinRequest);

                Debug.Log($"[NetworkApi] 방 조인 요청 전송: RoomId={roomId}");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogWarning("[NetworkApi] 방 조인 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                Debug.Log("[NetworkApi] 방 조인 응답 대기");
                var response = await tcs.Task;
                Debug.Log("[NetworkApi] 방 조인 응답 수신");

                if (response is RoomJoinCommand roomJoinResponse)
                {
                    Debug.Log(
                        $"[NetworkApi] 방 조인 응답 처리: Result={roomJoinResponse.Result}, Success={roomJoinResponse.Success}"
                    );
                    return roomJoinResponse;
                }

                throw new InvalidOperationException("RoomJoin 예상하지 못한 응답 타입입니다.");
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 방 조인 요청 타임아웃");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 방 조인 요청 실패: {e.Message}");
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
                Debug.Log($"[NetworkApi] 토큰 검증 시작: {accessToken}");

                // TaskCompletionSource 생성
                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.VerifyTokenResponse] = tcs;

                Debug.Log($"[NetworkApi] 응답 대기 등록: {PacketType.VerifyTokenResponse}");
                Debug.Log($"[NetworkApi] 대기 중인 요청 수: {_pendingRequests.Count}");

                // Client 연결 상태 확인
                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                // 요청 전송
                var verifyTokenRequest = new VerifyTokenSend(accessToken);
                client.Send(verifyTokenRequest);

                Debug.Log($"[NetworkApi] 토큰 검증 요청 전송: {accessToken}");

                // 응답 대기 (타임아웃 10초)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogWarning("[NetworkApi] 토큰 검증 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                Debug.Log("[NetworkApi] 토큰 검증 응답 대기");
                var response = await tcs.Task;
                Debug.Log("[NetworkApi] 토큰 검증 응답 수신");

                if (response is VerifyTokenCommand verifyTokenResponse)
                {
                    Debug.Log(
                        $"[NetworkApi] 토큰 검증 응답 처리: SessionId={verifyTokenResponse.SessionId}"
                    );
                    return verifyTokenResponse;
                }

                throw new InvalidOperationException("VerifyToken 예상하지 못한 응답 타입입니다.");
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 토큰 검증 요청 타임아웃");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkApi] 토큰 검증 요청 실패: {ex.Message}");
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
                Debug.Log("[NetworkApi] 씬 전환 시작");

                // Client 연결 상태 확인
                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.SceneChangeResponse] = tcs;

                Debug.Log($"[NetworkApi] 응답 대기 등록: {PacketType.SceneChangeResponse}");
                Debug.Log($"[NetworkApi] 대기 중인 요청 수: {_pendingRequests.Count}");

                var sceneChangeRequest = new SceneChangeSend();
                client.Send(sceneChangeRequest);

                Debug.Log("[NetworkApi] 씬 전환 요청 전송");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogWarning("[NetworkApi] 씬 전환 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                Debug.Log("[NetworkApi] 씬 전환 응답 대기");
                var response = await tcs.Task;
                Debug.Log("[NetworkApi] 씬 전환 응답 수신");

                if (response is SceneChangeCommand sceneChangeResponse)
                {
                    Debug.Log(
                        $"[NetworkApi] 씬 전환 응답 처리: Result={sceneChangeResponse.Result}, Success={sceneChangeResponse.Success}"
                    );
                    return sceneChangeResponse;
                }

                throw new InvalidOperationException("SceneChange 예상하지 못한 응답 타입입니다.");
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 씬 전환 요청 타임아웃");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 씬 전환 요청 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.SceneChangeResponse, out _);
            }
        }

        public async Task<ChestOpenCommand> ChestOpen(int chestId)
        {
            try
            {
                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.ChestOpenResponse] = tcs;

                var send = new ChestOpenSend(chestId);
                client.Send(send);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    tcs.TrySetCanceled();
                });

                var response = await tcs.Task;

                if (response is ChestOpenCommand chestOpenResponse)
                {
                    return chestOpenResponse;
                }

                throw new InvalidOperationException("상자 열기에 실패했습니다.");
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 상자 열기 요청 타임아웃");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 상자 열기 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.ChestOpenResponse, out _);
            }
        }

        public async Task<ChestCloseCommand> ChestClose(int chestId)
        {
            try
            {
                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.ChestCloseResponse] = tcs;

                var send = new ChestCloseSend(chestId);
                client.Send(send);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    tcs.TrySetCanceled();
                });

                var response = await tcs.Task;

                if (response is ChestCloseCommand chestCloseCommand)
                {
                    return chestCloseCommand;
                }

                throw new InvalidOperationException("상자 닫기에 실패했습니다.");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[NetworkApi] 상자 닫기 요청 타임아웃 (ID: {chestId})");
                return new ChestCloseCommand(0); // 기본 실패 응답 반환 (Fail = 0)
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 상자 닫기 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.ChestCloseResponse, out _);
            }
        }

        /// <summary>
        /// CommandDispatcher에서 호출되는 응답 처리 메서드
        /// </summary>
        /// <param name="command">수신된 명령어</param>
        public void MongdungAttack(Vector3 direction, long targetId)
        {
            try
            {
                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    return;
                }

                var attackRequest = new MongdungAttackSend(direction, targetId);
                client.Send(attackRequest);

                Debug.Log(
                    $"[NetworkApi] 몽둥이 공격 전송: Direction={direction}, TargetId={targetId}"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 몽둥이 공격 패킷 전송 실패: {e.Message}");
            }
        }

        public void GetItem(int chestId, int index)
        {
            try
            {
                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    return;
                }

                var getItemRequest = new GetItemSend(chestId, index);
                client.Send(getItemRequest);

            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 아이템 획득 패킷 전송 실패: {e.Message}");
            }
        }

        public void PutItem(int itemId)
        {
            try
            {
                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    return;
                }

                var putItemRequest = new PutItemSend(itemId);
                client.Send(putItemRequest);

                Debug.Log($"[NetworkApi] 아이템 넣기 요청 전송: ItemId={itemId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 아이템 넣기 패킷 전송 실패: {e.Message}");
            }
        }

        public async Task<MonggingRevivalStartCommand> MonggingRevivalStart(long targetMonggingId)
        {
            try
            {
                Debug.Log($"[NetworkApi] 몽깅이 부활 시작: TargetMonggingId={targetMonggingId}");

                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.MonggingRevivalStartResponse] = tcs;

                var monggingRevivalStartRequest = new MonggingRevivalStartSend(targetMonggingId);
                client.Send(monggingRevivalStartRequest);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogError("[NetworkApi] 몽깅이 부활 시작 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                var response = await tcs.Task;

                if (response is MonggingRevivalStartCommand command)
                {
                    return command;
                }

                throw new InvalidOperationException(
                    "MonggingRevivalStart에서 예상치 못한 응답 타입입니다."
                );
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 몽깅이 부활 시작 요청 타임아웃");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 몽깅이 부활 시작 패킷 전송 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.MonggingRevivalStartResponse, out _);
            }
        }

        public async Task<MonggingRevivalStopCommand> MonggingRevivalStop()
        {
            try
            {
                Debug.Log("[NetworkApi] 몽깅이 부활 중지 시작");

                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.MonggingRevivalStopResponse] = tcs;

                var monggingRevivalStopRequest = new MonggingRevivalStopSend();
                client.Send(monggingRevivalStopRequest);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogError("[NetworkApi] 몽깅이 부활 중지 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                var response = await tcs.Task;

                if (response is MonggingRevivalStopCommand command)
                {
                    return command;
                }

                throw new InvalidOperationException(
                    "MonggingRevivalStop에서 예상치 못한 응답 타입입니다."
                );
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 몽깅이 부활 중지 요청 타임아웃");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 몽깅이 부활 중지 패킷 전송 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.MonggingRevivalStopResponse, out _);
            }
        }

        public async Task<MonggingItemUseCommand> MonggingItemUse(Vector3 direction, int itemId)
        {
            try
            {
                Debug.Log(
                    $"[NetworkApi] 몽깅이 아이템 사용 시작: Direction={direction}, ItemId={itemId}"
                );

                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.MonggingItemUseResponse] = tcs;

                var monggingItemUseRequest = new MonggingItemUseSend(direction, itemId);
                client.Send(monggingItemUseRequest);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogError("[NetworkApi] 몽깅이 아이템 사용 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                var response = await tcs.Task;

                if (response is MonggingItemUseCommand command)
                {
                    return command;
                }

                throw new InvalidOperationException(
                    "MonggingItemUse에서 예상치 못한 응답 타입입니다."
                );
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 몽깅이 아이템 사용 요청 타임아웃");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 몽깅이 아이템 사용 패킷 전송 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.MonggingItemUseResponse, out _);
            }
        }

        public async Task<MonggingFieldItemUseCommand> MonggingFieldItemUse(int fieldItemId)
        {
            try
            {
                Debug.Log($"[NetworkApi] 몽깅이 필드 아이템 사용 시작: FieldItemId={fieldItemId}");

                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.MonggingFieldItemUseResponse] = tcs;

                var monggingFieldItemUseRequest = new MonggingFieldItemUseSend(fieldItemId);
                client.Send(monggingFieldItemUseRequest);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogError("[NetworkApi] 몽깅이 필드 아이템 사용 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                var response = await tcs.Task;

                if (response is MonggingFieldItemUseCommand command)
                {
                    return command;
                }

                throw new InvalidOperationException(
                    "MonggingFieldItemUse에서 예상치 못한 응답 타입입니다."
                );
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 몽깅이 필드 아이템 사용 요청 타임아웃");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 몽깅이 필드 아이템 사용 패킷 전송 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.MonggingFieldItemUseResponse, out _);
            }
        }

        public void MongdungSkill(int skillType)
        {
            try
            {
                Debug.Log($"[NetworkApi] 몽둥이 스킬 시작: SkillType={skillType}");

                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    return;
                }

                var mongdungSkillRequest = new MongdungSkillSend(skillType);
                client.Send(mongdungSkillRequest);

                Debug.Log($"[NetworkApi] 몽둥이 스킬 요청 전송 완료: SkillType={skillType}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 몽둥이 스킬 패킷 전송 실패: {e.Message}");
            }
        }

        public async Task<ExitAttemptCommand> ExitAttempt(int exitId)
        {
            try
            {
                Debug.Log($"[NetworkApi] 탈출 시도 시작: ExitId={exitId}");

                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[NetworkApi] 클라이언트 연결 실패");
                    throw new Exception("Client가 연결되지 않았습니다.");
                }

                var tcs = new TaskCompletionSource<object>();
                _pendingRequests[PacketType.ExitAttemptResponse] = tcs;

                var exitAttemptRequest = new ExitAttemptSend(exitId);
                client.Send(exitAttemptRequest);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() =>
                {
                    Debug.LogError("[NetworkApi] 탈출 시도 요청 타임아웃");
                    tcs.TrySetCanceled();
                });

                var response = await tcs.Task;

                if (response is ExitAttemptCommand command)
                {
                    return command;
                }

                throw new InvalidOperationException("ExitAttempt에서 예상치 못한 응답 타입입니다.");
            }
            catch (TimeoutException)
            {
                Debug.LogError("[NetworkApi] 탈출 시도 요청 타임아웃");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkApi] 탈출 시도 패킷 전송 실패: {e.Message}");
                throw;
            }
            finally
            {
                _pendingRequests.TryRemove(PacketType.ExitAttemptResponse, out _);
            }
        }

        public void HandleResponse(Command command)
        {
            if (_pendingRequests.TryGetValue(command.Type, out var tcs))
            {
                Debug.Log($"[NetworkApi] 응답 처리: {command.Type} (대기중: {_pendingRequests.Count}개)");
                tcs.SetResult(command);
            }
            else
            {
                Debug.LogWarning($"[NetworkApi] 대기 중인 요청 없음: {command.Type}");
                Debug.LogWarning($"[NetworkApi] 현재 대기 중인 요청들:");
                foreach (var kvp in _pendingRequests)
                {
                    Debug.LogWarning($"[NetworkApi]   - {kvp.Key}");
                }
            }
        }
    }
}
