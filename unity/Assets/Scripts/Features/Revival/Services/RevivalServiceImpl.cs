using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Features.ItemUsage.Messages;
using Features.Player.Services;
using Features.Revival.Messages;
using Features.Revival.Models;
using Features.Revival.NetworkSources;
using MessagePipe;
using Networks.Players;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Revival.Services
{
    /// <summary>
    /// 부활 서비스 구현체
    /// 직접 부활 및 자가제세동기 부활 로직 담당
    /// </summary>
    public class RevivalServiceImpl : IRevivalService, IDisposable
    {
        #region Constants

        private const float DIRECT_REVIVAL_DURATION = 3f; // 직접 부활 소요 시간 (초)
        private const float SELF_DEFIB_REVIVAL_DURATION = 3f; // 자가제세동기 부활 소요 시간 (초)
        private const float REVIVAL_RANGE = 2f; // 부활 가능 범위 (미터)
        #endregion

        #region Observable Properties

        public ReadOnlyReactiveProperty<RevivalProgressData> CurrentDirectRevival =>
            _currentDirectRevival;
        public ReadOnlyReactiveProperty<RevivalProgressData> CurrentSelfDefibRevival =>
            _currentSelfDefibRevival;
        public ReadOnlyReactiveProperty<RevivalInteractionData> InteractionState =>
            _interactionState;
        public ReadOnlyReactiveProperty<bool> IsDirectReviving => _isDirectReviving;
        public ReadOnlyReactiveProperty<bool> IsSelfDefibReviving => _isSelfDefibReviving;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<RevivalProgressData> _currentDirectRevival = new(
            new RevivalProgressData()
        );
        private readonly ReactiveProperty<RevivalProgressData> _currentSelfDefibRevival = new(
            new RevivalProgressData()
        );
        private readonly ReactiveProperty<RevivalInteractionData> _interactionState = new(
            new RevivalInteractionData()
        );
        private readonly ReactiveProperty<bool> _isDirectReviving = new(false);
        private readonly ReactiveProperty<bool> _isSelfDefibReviving = new(false);

        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = true;

        // Unity Coroutine 관리용
        private Coroutine _directRevivalCoroutine;
        private Coroutine _selfDefibRevivalCoroutine;

        #endregion

        #region Dependencies

        private readonly IRevivalNetworkSource _networkSource;
        private readonly IPublisher<RevivalProgressMessage> _revivalProgressPublisher;
        private readonly IPublisher<RevivalCompletedMessage> _revivalCompletedPublisher;
        private readonly IPublisher<SelfDefibRevivalStartMessage> _selfDefibStartPublisher;
        private readonly IPublisher<SelfDefibRevivalProgressMessage> _selfDefibProgressPublisher;
        private readonly PlayerManagerService _playerManagerService;
        private readonly ISubscriber<SelfDefibrillatorUsedMessage> _selfDefibUsedSubscriber;
        private readonly ISubscriber<FaintedMonggingDetectedMessage> _faintedDetectedSubscriber;
        private readonly ISubscriber<FaintedMonggingLeftMessage> _faintedLeftSubscriber;

        // Unity MonoBehaviour 참조 (Coroutine 실행용)
        private MonoBehaviour _coroutineRunner;

        #endregion

        #region Constructor

        [Inject]
        public RevivalServiceImpl(
            IRevivalNetworkSource networkSource,
            IPublisher<RevivalProgressMessage> revivalProgressPublisher,
            IPublisher<RevivalCompletedMessage> revivalCompletedPublisher,
            IPublisher<SelfDefibRevivalStartMessage> selfDefibStartPublisher,
            IPublisher<SelfDefibRevivalProgressMessage> selfDefibProgressPublisher,
            PlayerManagerService playerManagerService,
            ISubscriber<SelfDefibrillatorUsedMessage> selfDefibUsedSubscriber,
            ISubscriber<FaintedMonggingDetectedMessage> faintedDetectedSubscriber,
            ISubscriber<FaintedMonggingLeftMessage> faintedLeftSubscriber
        )
        {
            _networkSource = networkSource;
            _revivalProgressPublisher = revivalProgressPublisher;
            _revivalCompletedPublisher = revivalCompletedPublisher;
            _selfDefibStartPublisher = selfDefibStartPublisher;
            _selfDefibProgressPublisher = selfDefibProgressPublisher;
            _playerManagerService = playerManagerService;
            _selfDefibUsedSubscriber = selfDefibUsedSubscriber;
            _faintedDetectedSubscriber = faintedDetectedSubscriber;
            _faintedLeftSubscriber = faintedLeftSubscriber;

            // Coroutine 실행을 위한 MonoBehaviour 찾기
            _coroutineRunner = GameObject.FindObjectOfType<MonoBehaviour>();
            if (_coroutineRunner == null)
            {
                Debug.LogError(
                    "[RevivalServiceImpl] MonoBehaviour를 찾을 수 없어 Coroutine을 실행할 수 없습니다!"
                );
            }

            SubscribeToMessages();

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region Message Subscriptions

        private void SubscribeToMessages()
        {
            // 자가제세동기 사용 메시지 구독
            _selfDefibUsedSubscriber.Subscribe(OnSelfDefibrillatorUsed).AddTo(_disposables);

            // 기절한 몽깅이 감지 메시지 구독
            _faintedDetectedSubscriber.Subscribe(OnFaintedMonggingDetected).AddTo(_disposables);

            // 기절한 몽깅이 벗어남 메시지 구독
            _faintedLeftSubscriber.Subscribe(OnFaintedMonggingLeft).AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalServiceImpl] 메시지 구독 완료");
            }
        }

        private void OnSelfDefibrillatorUsed(SelfDefibrillatorUsedMessage message)
        {
            if (message.success)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[RevivalServiceImpl] 자가제세동기 사용됨: PlayerId={message.playerId}, ReviveHP={message.reviveHp}"
                    );
                }

                StartSelfDefibrillatorRevival(message.playerId, message.reviveHp);
            }
        }

        private void OnFaintedMonggingDetected(FaintedMonggingDetectedMessage message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log(
                    $"[RevivalServiceImpl] 기절한 몽깅이 감지: PlayerId={message.PlayerId}, Name={message.PlayerName}, Distance={message.Distance:F2}m"
                );
            }

            // 상호작용 상태 업데이트 (기절한 플레이어는 항상 부활 가능)
            UpdateInteractionState(
                message.PlayerId,
                true, // isInRange
                true, // canRevive (기절한 플레이어는 부활 가능)
                message.Distance,
                message.Transform.position
            );
        }

        private void OnFaintedMonggingLeft(FaintedMonggingLeftMessage message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log(
                    $"[RevivalServiceImpl] 기절한 몽깅이 벗어남: PlayerId={message.PlayerId}"
                );
            }

            // 상호작용 상태 클리어
            ClearInteractionState();
        }

        #endregion

        #region Direct Revival Methods

        public async UniTask<bool> StartDirectRevivalAsync(
            long revivingPlayerId,
            long targetPlayerId
        )
        {
            try
            {
                if (!CanStartDirectRevival(revivingPlayerId, targetPlayerId))
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning(
                            $"[RevivalServiceImpl] 직접 부활 시작 불가: RevivingId={revivingPlayerId}, TargetId={targetPlayerId}"
                        );
                    }
                    return false;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[RevivalServiceImpl] 직접 부활 시작: RevivingId={revivingPlayerId}, TargetId={targetPlayerId}"
                    );
                }

                // 서버에 부활 시작 요청
                var response = await _networkSource.StartDirectRevivalAsync(targetPlayerId);
                if (!response.Success)
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning(
                            $"[RevivalServiceImpl] 서버에서 직접 부활 시작 거부: {response.Result}"
                        );
                    }
                    return false;
                }

                // 부활 데이터 설정
                var revivalData = new RevivalProgressData(
                    revivingPlayerId,
                    targetPlayerId,
                    RevivalType.DirectRevival,
                    DIRECT_REVIVAL_DURATION
                );
                revivalData.StartRevival();

                _currentDirectRevival.Value = revivalData;
                _isDirectReviving.Value = true;

                // 부활 진행 Coroutine 시작
                if (_coroutineRunner != null)
                {
                    _directRevivalCoroutine = _coroutineRunner.StartCoroutine(
                        DirectRevivalCoroutine(revivalData)
                    );
                }

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[RevivalServiceImpl] 직접 부활 시작 완료: Duration={DIRECT_REVIVAL_DURATION}초"
                    );
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RevivalServiceImpl] 직접 부활 시작 실패: {e.Message}");
                return false;
            }
        }

        public async UniTask<bool> CancelDirectRevivalAsync(long revivingPlayerId)
        {
            try
            {
                if (
                    !_isDirectReviving.Value
                    || _currentDirectRevival.Value.revivingPlayerId != revivingPlayerId
                )
                {
                    return false;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[RevivalServiceImpl] 직접 부활 취소: RevivingId={revivingPlayerId}"
                    );
                }

                // 서버에 부활 중지 요청
                var response = await _networkSource.StopDirectRevivalAsync();

                // Coroutine 중지
                if (_directRevivalCoroutine != null && _coroutineRunner != null)
                {
                    _coroutineRunner.StopCoroutine(_directRevivalCoroutine);
                    _directRevivalCoroutine = null;
                }

                // 상태 초기화
                _currentDirectRevival.Value.Reset();
                _isDirectReviving.Value = false;

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[RevivalServiceImpl] 직접 부활 취소 완료: Success={response.Success}"
                    );
                }

                return response.Success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RevivalServiceImpl] 직접 부활 취소 실패: {e.Message}");
                return false;
            }
        }

        public bool CanStartDirectRevival(long revivingPlayerId, long targetPlayerId)
        {
            // 이미 부활 진행 중인지 확인
            if (IsAnyRevivalInProgress())
            {
                return false;
            }

            // 로컬 플레이어 확인
            if (_playerManagerService != null)
            {
                var localPlayerId = _playerManagerService.GetLocalPlayer().Id;
                if (revivingPlayerId != localPlayerId)
                {
                    return false;
                }
            }

            // 상호작용 상태 확인
            var interaction = _interactionState.Value;
            if (interaction.targetPlayerId != targetPlayerId || !interaction.canRevive)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Self Defibrillator Methods

        public bool StartSelfDefibrillatorRevival(long playerId, int reviveHp = -1)
        {
            try
            {
                if (!CanStartSelfDefibRevival(playerId))
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning(
                            $"[RevivalServiceImpl] 자가제세동기 부활 시작 불가: PlayerId={playerId}"
                        );
                    }
                    return false;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[RevivalServiceImpl] 자가제세동기 부활 시작: PlayerId={playerId}, ReviveHP={reviveHp}"
                    );
                }

                // 부활 데이터 설정 (HP 정보 포함)
                var revivalData = new RevivalProgressData(
                    playerId,
                    playerId,
                    RevivalType.SelfDefibrillator,
                    SELF_DEFIB_REVIVAL_DURATION,
                    reviveHp
                );
                revivalData.StartRevival();

                _currentSelfDefibRevival.Value = revivalData;
                _isSelfDefibReviving.Value = true;

                // 시작 메시지 발행
                _selfDefibStartPublisher.Publish(
                    new SelfDefibRevivalStartMessage(playerId, SELF_DEFIB_REVIVAL_DURATION)
                );

                // 부활 진행 Coroutine 시작
                if (_coroutineRunner != null)
                {
                    _selfDefibRevivalCoroutine = _coroutineRunner.StartCoroutine(
                        SelfDefibRevivalCoroutine(revivalData)
                    );
                }

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[RevivalServiceImpl] 자가제세동기 부활 시작 완료: Duration={SELF_DEFIB_REVIVAL_DURATION}초"
                    );
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RevivalServiceImpl] 자가제세동기 부활 시작 실패: {e.Message}");
                return false;
            }
        }

        public bool CanStartSelfDefibRevival(long playerId)
        {
            // 이미 부활 진행 중인지 확인
            if (IsAnyRevivalInProgress())
            {
                return false;
            }

            // 로컬 플레이어 확인
            if (_playerManagerService != null)
            {
                var localPlayerId = _playerManagerService.GetLocalPlayer().Id;
                if (playerId != localPlayerId)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Interaction Methods

        public void UpdateInteractionState(
            long targetPlayerId,
            bool isInRange,
            bool canRevive,
            float distance,
            Vector3 targetPosition
        )
        {
            var interaction = _interactionState.Value;
            string interactionText = "";

            if (isInRange && canRevive)
            {
                interactionText = "부활시키기";
            }
            else if (isInRange && !canRevive)
            {
                interactionText = "부활할 수 없음";
            }

            interaction.UpdateInteraction(
                targetPlayerId,
                isInRange,
                canRevive,
                distance,
                targetPosition,
                interactionText
            );
            _interactionState.ForceNotify();

            if (_enableDebugLogs)
            {
                Debug.Log(
                    $"[RevivalServiceImpl] 상호작용 상태 업데이트: TargetId={targetPlayerId}, InRange={isInRange}, CanRevive={canRevive}"
                );
            }
        }

        public void ClearInteractionState()
        {
            _interactionState.Value.Reset();
            _interactionState.ForceNotify();

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalServiceImpl] 상호작용 상태 초기화");
            }
        }

        #endregion

        #region Utility Methods

        public bool IsAnyRevivalInProgress()
        {
            return _isDirectReviving.Value || _isSelfDefibReviving.Value;
        }

        public void ResetAllRevivalStates()
        {
            // Coroutine 중지
            if (_directRevivalCoroutine != null && _coroutineRunner != null)
            {
                _coroutineRunner.StopCoroutine(_directRevivalCoroutine);
                _directRevivalCoroutine = null;
            }

            if (_selfDefibRevivalCoroutine != null && _coroutineRunner != null)
            {
                _coroutineRunner.StopCoroutine(_selfDefibRevivalCoroutine);
                _selfDefibRevivalCoroutine = null;
            }

            // 상태 초기화
            _currentDirectRevival.Value.Reset();
            _currentSelfDefibRevival.Value.Reset();
            _interactionState.Value.Reset();
            _isDirectReviving.Value = false;
            _isSelfDefibReviving.Value = false;

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalServiceImpl] 모든 부활 상태 초기화");
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator DirectRevivalCoroutine(RevivalProgressData revivalData)
        {
            while (!revivalData.IsCompleted())
            {
                // 진행률 업데이트
                float progress = revivalData.UpdateProgress();

                // 진행률 메시지 발행
                _revivalProgressPublisher.Publish(
                    new RevivalProgressMessage(
                        revivalData.revivingPlayerId,
                        revivalData.targetPlayerId,
                        progress,
                        revivalData.IsCompleted()
                    )
                );

                // Observable 업데이트
                _currentDirectRevival.ForceNotify();

                if (revivalData.IsCompleted())
                {
                    break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            // 완료 처리는 서버에서 MonggingRevivalComplete 메시지로 처리됨
            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalServiceImpl] 직접 부활 진행 완료, 서버 응답 대기");
            }
        }

        private IEnumerator SelfDefibRevivalCoroutine(RevivalProgressData revivalData)
        {
            while (!revivalData.IsCompleted())
            {
                // 진행률 업데이트
                float progress = revivalData.UpdateProgress();
                float remainingTime = revivalData.GetRemainingTime();

                // 진행률 메시지 발행
                _selfDefibProgressPublisher.Publish(
                    new SelfDefibRevivalProgressMessage(
                        revivalData.revivingPlayerId,
                        progress,
                        remainingTime
                    )
                );

                // Observable 업데이트
                _currentSelfDefibRevival.ForceNotify();

                if (revivalData.IsCompleted())
                {
                    break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            // 자가제세동기 부활 완료 (자동 완료)
            CompleteRevival(revivalData.revivingPlayerId, RevivalType.SelfDefibrillator);

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalServiceImpl] 자가제세동기 부활 완료");
            }
        }

        #endregion

        #region Public Methods for External Completion

        /// <summary>
        /// 서버에서 부활 완료 메시지를 받았을 때 호출
        /// </summary>
        public void CompleteRevival(long revivedPlayerId, RevivalType revivalType)
        {
            if (revivalType == RevivalType.DirectRevival && _isDirectReviving.Value)
            {
                var revivalData = _currentDirectRevival.Value;
                if (revivalData.targetPlayerId == revivedPlayerId)
                {
                    revivalData.CompleteRevival();
                    _currentDirectRevival.ForceNotify();

                    // Coroutine 중지
                    if (_directRevivalCoroutine != null && _coroutineRunner != null)
                    {
                        _coroutineRunner.StopCoroutine(_directRevivalCoroutine);
                        _directRevivalCoroutine = null;
                    }

                    _isDirectReviving.Value = false;

                    // 완료 메시지 발행 (실제 HP 값 사용)
                    int finalHp = revivalData.reviveHp > 0 ? revivalData.reviveHp : 50; // 기본값 50
                    _revivalCompletedPublisher.Publish(
                        new RevivalCompletedMessage(
                            revivedPlayerId,
                            revivalData.revivingPlayerId,
                            finalHp,
                            false
                        )
                    );
                }
            }
            else if (revivalType == RevivalType.SelfDefibrillator && _isSelfDefibReviving.Value)
            {
                var revivalData = _currentSelfDefibRevival.Value;
                if (revivalData.targetPlayerId == revivedPlayerId)
                {
                    revivalData.CompleteRevival();
                    _currentSelfDefibRevival.ForceNotify();

                    // Coroutine 중지
                    if (_selfDefibRevivalCoroutine != null && _coroutineRunner != null)
                    {
                        _coroutineRunner.StopCoroutine(_selfDefibRevivalCoroutine);
                        _selfDefibRevivalCoroutine = null;
                    }

                    _isSelfDefibReviving.Value = false;

                    // 완료 메시지 발행 (실제 HP 값 사용)
                    int finalHp = revivalData.reviveHp > 0 ? revivalData.reviveHp : 50; // 기본값 50
                    _revivalCompletedPublisher.Publish(
                        new RevivalCompletedMessage(revivedPlayerId, -1, finalHp, true)
                    );
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            // Coroutine 중지
            ResetAllRevivalStates();

            // Reactive Properties 해제
            _disposables?.Dispose();
            _currentDirectRevival?.Dispose();
            _currentSelfDefibRevival?.Dispose();
            _interactionState?.Dispose();
            _isDirectReviving?.Dispose();
            _isSelfDefibReviving?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalServiceImpl] Dispose 완료");
            }
        }

        #endregion
    }
}
