using UnityEngine;

namespace Features.Mongdung.Models
{
    /// <summary>
    /// 몽둥이 액션 타입
    /// </summary>
    public enum MongdungActionType
    {
        Attack, // 공격
        TrapSetting, // 함정 설치
        Frighten, // 위협
    }

    /// <summary>
    /// 몽둥이 상태
    /// </summary>
    public enum MongdungState
    {
        Idle, // 기본 상태 (이동 가능)
        ExecutingAction, // 액션 실행 중 (이동 불가)
        Cooldown, // 쿨다운 중 (이동 가능하지만 액션 불가)
        GameEnd, // 게임 종료
    }

    /// <summary>
    /// 몽둥이 액션 데이터
    /// </summary>
    [System.Serializable]
    public class MongdungActionData
    {
        [Header("액션 설정")]
        public MongdungActionType actionType;

        [Header("시간 설정 (초)")]
        public float executionDuration = 2.0f; // 액션 실행 시간
        public float cooldownDuration = 5.0f; // 쿨다운 시간

        [Header("애니메이션 설정")]
        public string animatorParameter; // 애니메이터 파라미터명

        [Header("효과 설정")]
        public float range = 5.0f; // 액션 범위
        public bool blockMovement = true; // 이동 제한 여부

        public MongdungActionData(MongdungActionType type, string parameter)
        {
            actionType = type;
            animatorParameter = parameter;
        }
    }

    /// <summary>
    /// 몽둥이 액션 상태
    /// </summary>
    public class MongdungActionState
    {
        public MongdungActionType ActionType { get; set; }
        public bool IsExecuting { get; set; }
        public bool IsOnCooldown { get; set; }
        public float RemainingExecutionTime { get; set; }
        public float RemainingCooldownTime { get; set; }

        public bool CanExecute => !IsExecuting && !IsOnCooldown;

        public MongdungActionState(MongdungActionType actionType)
        {
            ActionType = actionType;
            IsExecuting = false;
            IsOnCooldown = false;
            RemainingExecutionTime = 0f;
            RemainingCooldownTime = 0f;
        }
    }

    /// <summary>
    /// 몽둥이 전체 데이터
    /// </summary>
    [System.Serializable]
    public class MongdungData
    {
        [Header("기본 정보")]
        public long playerId;
        public string playerName;

        [Header("상태")]
        public MongdungState currentState = MongdungState.Idle;

        [Header("액션 데이터")]
        public MongdungActionData[] actionDataArray = new MongdungActionData[]
        {
            new MongdungActionData(MongdungActionType.Attack, "Attack")
            {
                executionDuration = 1.0f,
                cooldownDuration = 0.5f,
                range = 3.0f,
            },
            new MongdungActionData(MongdungActionType.TrapSetting, "TrapSetting")
            {
                executionDuration = 3.0f,
                cooldownDuration = 12.0f,
                range = 2.0f,
            },
            new MongdungActionData(MongdungActionType.Frighten, "Frighten")
            {
                executionDuration = 1.0f,
                cooldownDuration = 6.0f,
                range = 4.0f,
            },
        };

        /// <summary>
        /// 특정 액션 타입의 데이터 가져오기
        /// </summary>
        public MongdungActionData GetActionData(MongdungActionType actionType)
        {
            foreach (var data in actionDataArray)
            {
                if (data.actionType == actionType)
                    return data;
            }

            // 기본값 반환
            return new MongdungActionData(actionType, actionType.ToString());
        }
    }
}
