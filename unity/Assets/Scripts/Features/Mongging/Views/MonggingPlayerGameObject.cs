using System;
using Features.Mongging.Messages;
using Features.Mongging.Models;
using Features.Mongging.Services;
using MessagePipe;
using Networks.Rooms.Domains;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Mongging.Views
{
    /// <summary>
    /// 몽깅이 플레이어 GameObject 컴포넌트
    /// 애니메이션 처리 및 시각적 피드백 담당
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class MonggingPlayerGameObject : MonoBehaviour
    {
        [Header("몽깅이 설정")]
        [SerializeField]
        private MonggingPlayerType playerType = MonggingPlayerType.Worker;

        [Header("Debug 설정")]
        [SerializeField]
        private bool enableDebugLogs = true;

        // 의존성 주입
        private IMonggingTeamService _teamService;
        private ISubscriber<MonggingPlayerAnimationMessage> _animationSubscriber;
        private ISubscriber<MonggingPlayerStateChangedMessage> _stateChangedSubscriber;
        private ISubscriber<MonggingPlayerHitMessage> _hitSubscriber;
        private ISubscriber<MonggingPlayerStatusEffectMessage> _statusEffectSubscriber;

        // 컴포넌트
        private Animator _animator;
        private Collider[] _colliders;
        private CompositeDisposable _disposables = new CompositeDisposable();

        // 플레이어 정보
        public long PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public bool IsLocal { get; private set; }

        // 애니메이터 파라미터 해시
        private int _takeDamageHash;
        private int _downHash;
        private int _reviveHash;
        private int _stunHash;
        private int _frightenHash;
        private int _stateHash;
        private int _isAliveHash;
        private int _isFaintedHash;

        // 현재 상태
        private MonggingPlayerState _currentState = MonggingPlayerState.Normal;
        private bool _isPlayingAnimation = false;

        [Inject]
        public void Construct(
            IMonggingTeamService teamService,
            ISubscriber<MonggingPlayerAnimationMessage> animationSubscriber,
            ISubscriber<MonggingPlayerStateChangedMessage> stateChangedSubscriber,
            ISubscriber<MonggingPlayerHitMessage> hitSubscriber,
            ISubscriber<MonggingPlayerStatusEffectMessage> statusEffectSubscriber
        )
        {
            _teamService = teamService ?? throw new ArgumentNullException(nameof(teamService));
            _animationSubscriber = animationSubscriber;
            _stateChangedSubscriber = stateChangedSubscriber;
            _hitSubscriber = hitSubscriber;
            _statusEffectSubscriber = statusEffectSubscriber;

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[MonggingPlayerGameObject] VContainer 의존성 주입 완료: {gameObject.name}"
                );
            }
        }

        private void Start()
        {
            InitializeComponents();
            InitializeAnimatorParameters();
            InitializePlayerIdFromPlayerGameObject();
            SubscribeToMessages();

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[MonggingPlayerGameObject] 초기화 완료: {gameObject.name}, PlayerId: {PlayerId}"
                );
            }
        }

        private void Update()
        {
            // 상태이상 시간 업데이트는 MonggingPlayerService에서 처리
            // GameObject는 시각적 표현만 담당
        }

        /// <summary>
        /// PlayerPacket 데이터로 몽깅이 초기화
        /// </summary>
        public void InitializeFromPacket(PlayerPacket packet)
        {
            if (!packet.IsMongging)
            {
                Debug.LogWarning(
                    "[MonggingPlayerGameObject] 몽둥이 플레이어에게 MonggingPlayerGameObject가 적용되었습니다!"
                );
                return;
            }

            PlayerId = packet.Id;
            PlayerName = packet.NickName ?? $"Player_{packet.Id}";
            IsLocal = packet.IsMine; // 패킷에서 로컬 여부 확인

            // 팀 서비스에 플레이어 등록
            if (_teamService != null)
            {
                _teamService.RegisterPlayer(PlayerId, PlayerName, playerType, IsLocal);
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[MonggingPlayerGameObject] 패킷으로 초기화: PlayerId={PlayerId}, Name={PlayerName}, Local={IsLocal}"
                );
            }
        }

        private void InitializeComponents()
        {
            // 하위 오브젝트들에서 Controller가 있는 Animator 찾기
            var animators = GetComponentsInChildren<Animator>();

            foreach (var animator in animators)
            {
                if (animator.runtimeAnimatorController != null)
                {
                    _animator = animator;
                    break;
                }
            }

            if (_animator == null)
            {
                Debug.LogError(
                    $"[MonggingPlayerGameObject] Controller가 할당된 Animator를 찾을 수 없습니다: {gameObject.name}"
                );
                if (enableDebugLogs)
                {
                    Debug.Log($"[MonggingPlayerGameObject] 발견된 Animator 목록:");
                    for (int i = 0; i < animators.Length; i++)
                    {
                        Debug.Log(
                            $"  [{i}] {animators[i].gameObject.name} - Controller: {animators[i].runtimeAnimatorController?.name ?? "None"}"
                        );
                    }
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[MonggingPlayerGameObject] Controller가 있는 Animator 발견: {_animator.gameObject.name} (Controller: {_animator.runtimeAnimatorController.name})"
                    );
                }
            }

            // 렌더러 및 콜라이더 컴포넌트 캐시
            _colliders = GetComponentsInChildren<Collider>();

            if (enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerGameObject] {_colliders.Length}개의 Collider 캐시 완료");
            }
        }

        private void InitializeAnimatorParameters()
        {
            if (_animator != null)
            {
                _takeDamageHash = Animator.StringToHash("TakeDamage");
                _downHash = Animator.StringToHash("Down");
                _reviveHash = Animator.StringToHash("Revive");
                _stunHash = Animator.StringToHash("Stun");
                _frightenHash = Animator.StringToHash("Frighten");
                _stateHash = Animator.StringToHash("State");
                _isAliveHash = Animator.StringToHash("IsAlive");
                _isFaintedHash = Animator.StringToHash("IsFainted");

                if (enableDebugLogs)
                {
                    Debug.Log($"[MonggingPlayerGameObject] 애니메이터 파라미터 초기화 완료");
                }
            }
        }

        /// <summary>
        /// PlayerGameObject 또는 RemotePlayerGameObject에서 PlayerId를 가져와서 초기화
        /// </summary>
        private void InitializePlayerIdFromPlayerGameObject()
        {
            // 로컬 플레이어: PlayerGameObject에서 PlayerId 가져오기
            var playerGameObject = GetComponent<Features.Player.Views.PlayerGameObject>();
            if (playerGameObject != null && playerGameObject.PlayerId != -1)
            {
                PlayerId = playerGameObject.PlayerId;
                PlayerName = $"Player_{PlayerId}";
                IsLocal = true;

                // 팀 서비스에 플레이어 등록
                if (_teamService != null)
                {
                    _teamService.RegisterPlayer(PlayerId, PlayerName, playerType, IsLocal);
                }

                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[MonggingPlayerGameObject] PlayerGameObject에서 PlayerId 설정: {PlayerId} (Local)"
                    );
                }
                return;
            }

            // 원격 플레이어: RemotePlayerGameObject에서 PlayerId 가져오기
            var remotePlayerGameObject =
                GetComponent<Features.Player.Views.RemotePlayerGameObject>();
            if (remotePlayerGameObject != null && remotePlayerGameObject.PlayerId != -1)
            {
                PlayerId = remotePlayerGameObject.PlayerId;
                PlayerName = $"Player_{PlayerId}";
                IsLocal = false;

                // 팀 서비스에 플레이어 등록
                if (_teamService != null)
                {
                    _teamService.RegisterPlayer(PlayerId, PlayerName, playerType, IsLocal);
                }

                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[MonggingPlayerGameObject] RemotePlayerGameObject에서 PlayerId 설정: {PlayerId} (Remote)"
                    );
                }
                return;
            }

            if (enableDebugLogs)
            {
                Debug.LogWarning(
                    $"[MonggingPlayerGameObject] PlayerGameObject 또는 RemotePlayerGameObject를 찾을 수 없거나 PlayerId가 -1입니다: {gameObject.name}"
                );
            }
        }

        private void SubscribeToMessages()
        {
            if (
                _animationSubscriber == null
                || _stateChangedSubscriber == null
                || _hitSubscriber == null
                || _statusEffectSubscriber == null
            )
            {
                Debug.LogError(
                    $"[MonggingPlayerGameObject] Message Subscriber가 null입니다: {gameObject.name}"
                );
                return;
            }

            // 애니메이션 메시지 구독
            _animationSubscriber
                .Subscribe(msg =>
                {
                    if (msg.PlayerId == PlayerId)
                        OnAnimationMessage(msg);
                })
                .AddTo(_disposables);

            // 상태 변경 메시지 구독
            _stateChangedSubscriber
                .Subscribe(msg =>
                {
                    if (msg.PlayerId == PlayerId)
                        OnStateChanged(msg);
                })
                .AddTo(_disposables);

            // 피격 메시지 구독
            _hitSubscriber
                .Subscribe(msg =>
                {
                    if (msg.PlayerId == PlayerId)
                        OnHit(msg);
                })
                .AddTo(_disposables);

            // 상태이상 메시지 구독
            _statusEffectSubscriber
                .Subscribe(msg =>
                {
                    if (msg.PlayerId == PlayerId)
                        OnStatusEffect(msg);
                })
                .AddTo(_disposables);

            if (enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerGameObject] 메시지 구독 완료: PlayerId={PlayerId}");
            }
        }

        /// <summary>
        /// 애니메이션 메시지 처리
        /// </summary>
        private void OnAnimationMessage(MonggingPlayerAnimationMessage message)
        {
            try
            {
                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[MonggingPlayerGameObject] 애니메이션 메시지 수신: PlayerId={message.PlayerId}, Trigger={message.AnimationTrigger}, Duration={message.Duration}"
                    );
                }

                TriggerAnimation(message.AnimationTrigger, message.Duration);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[MonggingPlayerGameObject] 애니메이션 메시지 처리 실패: {e.Message}"
                );
            }
        }

        /// <summary>
        /// 상태 변경 메시지 처리
        /// </summary>
        private void OnStateChanged(MonggingPlayerStateChangedMessage message)
        {
            try
            {
                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[MonggingPlayerGameObject] 상태 변경: PlayerId={message.PlayerId}, {message.PreviousState} → {message.NewState}, HP={message.CurrentHp}"
                    );
                }

                _currentState = message.NewState;
                UpdateAnimatorState();

                // 상태 전환 애니메이션
                switch (message.NewState)
                {
                    case MonggingPlayerState.Fainted:
                    case MonggingPlayerState.Dead:
                        TriggerAnimation("Down");
                        break;
                    case MonggingPlayerState.Normal:
                        if (message.PreviousState == MonggingPlayerState.Fainted)
                        {
                            TriggerAnimation("Revive");
                        }
                        break;
                    case MonggingPlayerState.Escaped:
                        ApplyEscapedVisual();
                        SetInteractionEnabled(false);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingPlayerGameObject] 상태 변경 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 피격 메시지 처리
        /// </summary>
        private void OnHit(MonggingPlayerHitMessage message)
        {
            try
            {
                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[MonggingPlayerGameObject] 피격: PlayerId={message.PlayerId}, Damage={message.Damage}, HP={message.PreviousHp}→{message.CurrentHp}"
                    );
                }

                // 피격 애니메이션
                TriggerAnimation("TakeDamage", 0.5f);

                // 피격 이펙트 (추후 구현)
                // ShowHitEffect(message.HitPosition);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingPlayerGameObject] 피격 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 상태이상 메시지 처리
        /// </summary>
        private void OnStatusEffect(MonggingPlayerStatusEffectMessage message)
        {
            try
            {
                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[MonggingPlayerGameObject] 상태이상: PlayerId={message.PlayerId}, Effect={message.StatusEffect}, Applied={message.IsApplied}, Duration={message.Duration}"
                    );
                }

                if (message.IsApplied)
                {
                    switch (message.StatusEffect)
                    {
                        case MonggingPlayerState.Stunned:
                            TriggerAnimation("Stun", message.Duration);
                            // 감전 이펙트 (추후 구현)
                            break;

                        case MonggingPlayerState.Frightened:
                            TriggerAnimation("Frighten", message.Duration);
                            // 공포 이펙트 - 시야 어둡게 (추후 구현)
                            break;
                    }
                }
                else
                {
                    // 상태이상 해제 처리
                    if (enableDebugLogs)
                    {
                        Debug.Log(
                            $"[MonggingPlayerGameObject] 상태이상 해제: {message.StatusEffect}"
                        );
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingPlayerGameObject] 상태이상 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 애니메이션 트리거
        /// </summary>
        private void TriggerAnimation(string triggerName, float duration = 0f)
        {
            if (_animator == null)
            {
                Debug.LogError(
                    $"[MonggingPlayerGameObject] Animator가 null입니다! 애니메이션 트리거 실패: {triggerName}"
                );
                return;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogError(
                    $"[MonggingPlayerGameObject] AnimatorController가 null입니다! 애니메이션 트리거 실패: {triggerName}"
                );
                return;
            }

            int triggerHash = Animator.StringToHash(triggerName);
            _animator.SetTrigger(triggerHash);

            if (duration > 0f)
            {
                _isPlayingAnimation = true;
                // 애니메이션 지속 시간 후 해제 (추후 구현)
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[MonggingPlayerGameObject] 애니메이션 트리거: {triggerName} (Hash: {triggerHash}), Duration: {duration}"
                );
            }
        }

        /// <summary>
        /// 애니메이터 상태 업데이트
        /// </summary>
        private void UpdateAnimatorState()
        {
            if (_animator == null)
                return;

            // 상태 값 설정
            _animator.SetInteger(_stateHash, (int)_currentState);
            _animator.SetBool(_isAliveHash, _currentState != MonggingPlayerState.Dead);
            _animator.SetBool(_isFaintedHash, _currentState == MonggingPlayerState.Fainted);
        }

        /// <summary>
        /// 특정 애니메이션이 재생 중인지 확인
        /// </summary>
        public bool IsPlayingAnimation(string animationName)
        {
            if (_animator == null)
                return false;

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(animationName);
        }

        /// <summary>
        /// 현재 상태 가져오기
        /// </summary>
        public MonggingPlayerState GetCurrentState()
        {
            return _currentState;
        }

        /// <summary>
        /// 탈출 시각 효과 적용 (SkinnedMeshRenderer 비활성화)
        /// </summary>
        private void ApplyEscapedVisual()
        {
            // SkinnedMeshRenderer를 찾아서 비활성화 (투명 효과)
            var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            int disabledCount = 0;

            foreach (var renderer in skinnedRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                    disabledCount++;

                    if (enableDebugLogs)
                    {
                        Debug.Log(
                            $"[MonggingPlayerGameObject] SkinnedMeshRenderer 비활성화: {renderer.gameObject.name}"
                        );
                    }
                }
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[MonggingPlayerGameObject] 탈출 시각 효과 적용 완료: {disabledCount}개의 SkinnedMeshRenderer 비활성화"
                );
            }
        }

        /// <summary>
        /// 상호작용 활성화/비활성화 (이동은 유지)
        /// </summary>
        private void SetInteractionEnabled(bool enabled)
        {
            // CharacterController를 제외한 Collider만 비활성화 (이동 유지)
            if (_colliders != null)
            {
                foreach (var collider in _colliders)
                {
                    if (collider != null && !(collider is CharacterController))
                    {
                        collider.enabled = enabled;
                    }
                }
            }

            // InteractionTriggerDetector 비활성화
            var interactionDetector =
                GetComponentInChildren<Interaction.InteractionTriggerDetector>();
            if (interactionDetector != null)
            {
                interactionDetector.enabled = enabled;
            }

            // FaintedMonggingInteractable 비활성화
            var faintedInteractable =
                GetComponentInChildren<Features.Revival.Views.FaintedMonggingInteractable>();
            if (faintedInteractable != null)
            {
                faintedInteractable.SetInteractionEnabled(enabled);
            }

            if (enableDebugLogs)
            {
                string action = enabled ? "활성화" : "비활성화";
                Debug.Log($"[MonggingPlayerGameObject] 상호작용 {action} 완료 (이동은 유지)");
            }
        }

        private void OnDestroy()
        {
            // R3 구독 해제
            _disposables?.Dispose();

            // 팀 서비스에서 플레이어 해제
            if (_teamService != null && PlayerId > 0)
            {
                _teamService.UnregisterPlayer(PlayerId);
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[MonggingPlayerGameObject] 리소스 정리 완료: {gameObject.name}, PlayerId: {PlayerId}"
                );
            }
        }

        /// <summary>
        /// 디버그용 현재 상태 출력
        /// </summary>
        [ContextMenu("Log Current Status")]
        public void LogCurrentStatus()
        {
            var playerData = _teamService?.GetPlayer(PlayerId);
            if (playerData != null)
            {
                Debug.Log(
                    $"[MonggingPlayerGameObject] 상태 리포트:\n"
                        + $"  PlayerId: {PlayerId}\n"
                        + $"  PlayerName: {PlayerName}\n"
                        + $"  IsLocal: {IsLocal}\n"
                        + $"  CurrentState: {_currentState}\n"
                        + $"  HP: {playerData.currentHp}/{playerData.maxHp}\n"
                        + $"  FaintCount: {playerData.faintCount}\n"
                        + $"  IsStunned: {playerData.isStunned} ({playerData.stunRemainingTime:F1}s)\n"
                        + $"  IsFrightened: {playerData.isFrightened} ({playerData.frightenRemainingTime:F1}s)"
                );
            }
            else
            {
                Debug.Log(
                    $"[MonggingPlayerGameObject] 플레이어 데이터를 찾을 수 없음: PlayerId={PlayerId}"
                );
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 몽깅이 상태 시각화 (추후 구현)
            Gizmos.color = _currentState switch
            {
                MonggingPlayerState.Normal => Color.green,
                MonggingPlayerState.Stunned => Color.yellow,
                MonggingPlayerState.Frightened => Color.blue,
                MonggingPlayerState.Fainted => Color.red,
                MonggingPlayerState.Dead => Color.black,
                MonggingPlayerState.Escaped => Color.white,
                _ => Color.gray,
            };

            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
        }
    }
}
