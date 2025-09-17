using Messages;
using UnityEngine;
using VContainer;
using ViewModels;

namespace TestDebug
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
    }
}
