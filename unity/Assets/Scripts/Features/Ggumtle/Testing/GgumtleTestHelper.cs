using Features.Ggumtle.Messages;
using Features.Ggumtle.Services;
using Features.Ggumtle.ViewModels;
using UnityEngine;
using VContainer;

namespace Features.Ggumtle.Testing
{
    /// <summary>
    /// 꿈틀이 시스템 테스트를 위한 헬퍼 클래스
    /// 에디터에서 꿈틀이 상태를 확인하고 테스트할 수 있는 기능 제공
    /// </summary>
    public class GgumtleTestHelper : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField]
        private bool enableDebugUI = true;

        [SerializeField]
        private bool showViewModelInfo = true;

        [Inject]
        private GgumtleViewModel _viewModel;

        [Inject]
        private IGgumtleService _ggumtleService;

        private void OnGUI()
        {
            if (!enableDebugUI)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label(
                "🔍 Ggumtle Debug Info",
                new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold }
            );

            if (_viewModel != null && showViewModelInfo)
            {
                ShowViewModelInfo();
            }
            else
            {
                GUILayout.Label("❌ GgumtleViewModel not injected");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void ShowViewModelInfo()
        {
            GUILayout.Space(10);

            // 기본 정보
            GUILayout.Label("📋 Basic Info", EditorGUIStyle());
            GUILayout.Label($"Current ID: {_viewModel.CurrentGgumtleId.Value}");
            GUILayout.Label($"In Range: {_viewModel.IsInRange.Value}");
            GUILayout.Label($"Distance: {_viewModel.Distance.Value:F2}m");

            GUILayout.Space(5);

            // 상태 정보
            GUILayout.Label("🎯 State Info", EditorGUIStyle());
            GUILayout.Label($"State: {_viewModel.State.Value}");
            GUILayout.Label($"Can Interact: {_viewModel.CanInteract.Value}");
            GUILayout.Label($"Interaction Text: {_viewModel.InteractionText.Value}");

            GUILayout.Space(5);

            // 홀드 정보
            GUILayout.Label("⏳ Hold Info", EditorGUIStyle());
            GUILayout.Label($"Is Holding: {_viewModel.IsHolding.Value}");
            GUILayout.Label($"Hold Progress: {_viewModel.HoldProgress.Value:P1}");
            GUILayout.Label($"Hold Duration: {_viewModel.HoldDuration.Value:F1}s");

            GUILayout.Space(5);

            // 먹이 정보
            GUILayout.Label("🍯 Food Info", EditorGUIStyle());
            GUILayout.Label($"Food: {_viewModel.CurrentFood.Value}/{_viewModel.MaxFood.Value}");
            GUILayout.Label($"Food Progress: {_viewModel.FoodProgress.Value:P1}");

            GUILayout.Space(10);

            // 테스트 버튼들
            GUILayout.Label("🎮 Test Controls", EditorGUIStyle());

            if (GUILayout.Button("Start Hold"))
            {
                StartHoldAsync();
            }

            if (GUILayout.Button("Cancel Hold"))
            {
                _viewModel.CancelHold();
            }

            if (GUILayout.Button("Reset ViewModel"))
            {
                // ViewModel 상태 리셋 (테스트용)
                _viewModel.IsInRange.Value = false;
                _viewModel.CurrentGgumtleId.Value = string.Empty;
            }

            GUILayout.Space(5);

            // 꿈틀이 관리
            GUILayout.Label("🔧 Ggumtle Management", EditorGUIStyle());

            if (GUILayout.Button("Print All Ggumtles"))
            {
                PrintAllGgumtles();
            }
        }

        private GUIStyle EditorGUIStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow },
            };
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogViewModelState()
        {
            if (_viewModel == null)
            {
                UnityEngine.Debug.Log("[GgumtleTestHelper] ViewModel is null");
                return;
            }

            UnityEngine.Debug.Log(
                $"[GgumtleTestHelper] ViewModel State:\n"
                    + $"  - ID: {_viewModel.CurrentGgumtleId.Value}\n"
                    + $"  - InRange: {_viewModel.IsInRange.Value}\n"
                    + $"  - State: {_viewModel.State.Value}\n"
                    + $"  - IsHolding: {_viewModel.IsHolding.Value}\n"
                    + $"  - Food: {_viewModel.CurrentFood.Value}/{_viewModel.MaxFood.Value}"
            );
        }

        // 에디터에서 호출할 수 있는 컨텍스트 메뉴
        [ContextMenu("Log ViewModel State")]
        public void LogState()
        {
            LogViewModelState();
        }

        [ContextMenu("Test Hold")]
        public void TestHold()
        {
            if (_viewModel != null)
            {
                StartHoldAsync();
            }
        }

        private async void StartHoldAsync()
        {
            if (_viewModel != null)
            {
                await _viewModel.StartHold();
            }
        }

        // 꿈틀이 관리 메서드들

        [ContextMenu("현재 등록된 꿈틀이 목록 출력")]
        public void PrintAllGgumtles()
        {
            if (_ggumtleService == null)
            {
                Debug.LogError("[GgumtleTestHelper] GgumtleService가 주입되지 않음");
                return;
            }

            var allData = _ggumtleService.GetAllGgumtleData();
            Debug.Log($"[GgumtleTestHelper] 등록된 꿈틀이 총 {allData.Count}개:");

            foreach (var kvp in allData)
            {
                var data = kvp.Value;
                Debug.Log($"  - ID: {kvp.Key}, Name: {data.ggumtleName}, Position: {data.position}, State: {data.currentState}");
            }
        }

    }
}
