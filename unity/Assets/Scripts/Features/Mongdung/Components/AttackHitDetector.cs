using Features.Player.Views;
using UnityEngine;

namespace Features.Mongdung.Components
{
    /// <summary>
    /// 몽둥이 공격 범위 판정 컴포넌트
    /// 공격 시 범위 내 몽깅이 플레이어를 검출하여 적중 여부 판별
    /// </summary>
    public class AttackHitDetector : MonoBehaviour
    {
        [Header("공격 범위 설정")]
        [SerializeField] private float attackRange = 3f;
        [SerializeField] private float attackAngle = 60f; // 공격 각도 (도)
        [SerializeField] private bool enableDebugVisualization = true;

        [Header("레이어 설정")]
        [SerializeField] private LayerMask monggingLayerMask = -1; // Player_Mongging 레이어

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;

        /// <summary>
        /// 공격 방향으로 적중 대상 검출
        /// </summary>
        /// <param name="attackDirection">공격 방향 벡터</param>
        /// <returns>적중된 플레이어 ID, 미적중 시 -1</returns>
        public long DetectHitTarget(Vector3 attackDirection)
        {
            if (attackDirection == Vector3.zero)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[AttackHitDetector] 공격 방향이 Zero Vector입니다.");
                return -1;
            }

            // 공격 중심점 계산 (플레이어 앞쪽으로 약간 이동)
            Vector3 attackCenter = transform.position + attackDirection.normalized * (attackRange * 0.3f);

            // 구체 범위로 1차 검출 (Player_Mongging 레이어만)
            var hits = Physics.OverlapSphere(attackCenter, attackRange, monggingLayerMask);

            if (enableDebugLogs && hits.Length > 0)
            {
                Debug.Log($"[AttackHitDetector] 1차 검출된 오브젝트 수: {hits.Length}");
            }

            long closestTargetId = -1;
            float closestDistance = float.MaxValue;
            GameObject closestTarget = null;

            // 자기 자신의 Player ID 가져오기 (제외용)
            long myPlayerId = GetPlayerIdFromGameObject(gameObject);

            foreach (var hit in hits)
            {
                // 자기 자신은 제외
                if (hit.gameObject == gameObject)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[AttackHitDetector] 자기 자신 제외: {hit.name}");
                    }
                    continue;
                }

                // Player ID 추출 및 자기 자신과 같은 ID면 제외
                long targetPlayerId = GetPlayerIdFromGameObject(hit.gameObject);
                if (targetPlayerId == -1)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[AttackHitDetector] Player ID를 찾을 수 없어 제외: {hit.name}");
                    }
                    continue;
                }

                if (targetPlayerId == myPlayerId)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[AttackHitDetector] 같은 Player ID로 제외: {hit.name}, ID: {targetPlayerId}");
                    }
                    continue;
                }

                // 방향 체크 (공격 각도 내에 있는지)
                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(attackDirection.normalized, dirToTarget);

                if (enableDebugLogs)
                {
                    Debug.Log($"[AttackHitDetector] 대상: {hit.name}, ID: {targetPlayerId}, 각도: {angle:F1}°, 허용각도: {attackAngle * 0.5f:F1}°");
                }

                // 공격 각도 내에 있는지 체크
                if (angle <= attackAngle * 0.5f)
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);

                    // 가장 가까운 대상 찾기
                    if (distance < closestDistance)
                    {
                        closestTargetId = targetPlayerId;
                        closestDistance = distance;
                        closestTarget = hit.gameObject;

                        if (enableDebugLogs)
                        {
                            Debug.Log($"[AttackHitDetector] 새로운 가장 가까운 대상: {hit.name}, ID: {targetPlayerId}, 거리: {distance:F2}m");
                        }
                    }
                }
            }

            // 최종 결과 로그
            if (enableDebugLogs)
            {
                if (closestTargetId != -1)
                {
                    Debug.Log($"[AttackHitDetector] ✅ 공격 적중! 대상: {closestTarget.name}, PlayerId: {closestTargetId}, 거리: {closestDistance:F2}m");
                }
                else
                {
                    Debug.Log($"[AttackHitDetector] ❌ 공격 미적중 - 범위 내 유효 대상 없음");
                }
            }

            return closestTargetId;
        }

        /// <summary>
        /// GameObject에서 플레이어 ID 추출
        /// </summary>
        /// <param name="obj">대상 GameObject</param>
        /// <returns>플레이어 ID, 찾을 수 없으면 -1</returns>
        private long GetPlayerIdFromGameObject(GameObject obj)
        {
            // Local Player 체크
            var playerGameObject = obj.GetComponent<PlayerGameObject>();
            if (playerGameObject != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[AttackHitDetector] Local Player 발견: {obj.name}, ID: {playerGameObject.PlayerId}");
                return playerGameObject.PlayerId;
            }

            // Remote Player 체크
            var remotePlayerGameObject = obj.GetComponent<RemotePlayerGameObject>();
            if (remotePlayerGameObject != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[AttackHitDetector] Remote Player 발견: {obj.name}, ID: {remotePlayerGameObject.PlayerId}");
                return remotePlayerGameObject.PlayerId;
            }

            if (enableDebugLogs)
                Debug.LogWarning($"[AttackHitDetector] PlayerGameObject 컴포넌트를 찾을 수 없음: {obj.name}");

            return -1;
        }

        /// <summary>
        /// 공격 범위 시각화 (Scene View용)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!enableDebugVisualization) return;

            // 공격 범위 구체 그리기 (반투명 빨간색)
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Vector3 attackCenter = transform.position + transform.forward * (attackRange * 0.3f);
            Gizmos.DrawSphere(attackCenter, attackRange);

            // 공격 방향 화살표 그리기
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * attackRange);

            // 공격 각도 범위 그리기
            Gizmos.color = Color.yellow;
            float halfAngle = attackAngle * 0.5f * Mathf.Deg2Rad;

            // 왼쪽 경계선
            Vector3 leftBoundary = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * transform.forward * attackRange;
            Gizmos.DrawRay(transform.position, leftBoundary);

            // 오른쪽 경계선
            Vector3 rightBoundary = Quaternion.Euler(0, attackAngle * 0.5f, 0) * transform.forward * attackRange;
            Gizmos.DrawRay(transform.position, rightBoundary);
        }

        /// <summary>
        /// Inspector에서 레이어 마스크 자동 설정 (Editor 전용)
        /// </summary>
        private void Reset()
        {
            // Player_Mongging 레이어가 있다면 자동으로 설정
            int monggingLayer = LayerMask.NameToLayer("Player_Mongging");
            if (monggingLayer != -1)
            {
                monggingLayerMask = 1 << monggingLayer;
                Debug.Log($"[AttackHitDetector] Player_Mongging 레이어 자동 설정: {monggingLayerMask}");
            }
            else
            {
                Debug.LogWarning("[AttackHitDetector] Player_Mongging 레이어를 찾을 수 없습니다. 수동으로 설정해주세요.");
            }
        }
    }
}