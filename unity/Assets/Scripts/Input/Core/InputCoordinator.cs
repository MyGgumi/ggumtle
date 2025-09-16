using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace InputSystem.Core
{
    /// <summary>
    /// 모든 입력 레이어를 중앙에서 관리하는 코디네이터
    /// </summary>
    public class InputCoordinator : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        private readonly List<IInputLayer> _inputLayers = new List<IInputLayer>();
        private readonly Dictionary<int, IInputLayer> _activePointers = new Dictionary<int, IInputLayer>();

        private VisualElement _root;
        private InputMode _currentMode = InputMode.Normal;

        public InputMode CurrentMode => _currentMode;

        /// <summary>
        /// 입력 코디네이터 초기화
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root;
            DebugLog("InputCoordinator 초기화");
        }

        /// <summary>
        /// 입력 레이어 등록
        /// </summary>
        public void RegisterLayer(IInputLayer layer)
        {
            if (layer == null) return;

            if (!_inputLayers.Contains(layer))
            {
                _inputLayers.Add(layer);
                layer.Initialize(_root);

                // 우선순위 순으로 정렬
                _inputLayers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                DebugLog($"입력 레이어 등록: {layer.LayerName} (Priority: {layer.Priority})");
            }
        }

        /// <summary>
        /// 입력 레이어 등록 해제
        /// </summary>
        public void UnregisterLayer(IInputLayer layer)
        {
            if (layer == null) return;

            if (_inputLayers.Remove(layer))
            {
                layer.Cleanup();
                DebugLog($"입력 레이어 해제: {layer.LayerName}");
            }
        }

        /// <summary>
        /// 특정 타입의 레이어 가져오기
        /// </summary>
        public T GetLayer<T>() where T : class, IInputLayer
        {
            return _inputLayers.FirstOrDefault(l => l is T) as T;
        }

        /// <summary>
        /// 입력 모드 변경
        /// </summary>
        public void SetInputMode(InputMode mode)
        {
            if (_currentMode == mode) return;

            var previousMode = _currentMode;
            _currentMode = mode;

            UpdateLayersForMode();

            DebugLog($"입력 모드 변경: {previousMode} → {mode}");
        }

        /// <summary>
        /// 포인터 점유 등록 (멀티터치 충돌 방지)
        /// </summary>
        public bool TryAcquirePointer(int pointerId, IInputLayer layer)
        {
            if (_activePointers.ContainsKey(pointerId))
            {
                // 이미 다른 레이어가 사용 중
                return _activePointers[pointerId] == layer;
            }

            _activePointers[pointerId] = layer;
            DebugLog($"포인터 {pointerId} 획득: {layer.LayerName}");
            return true;
        }

        /// <summary>
        /// 포인터 점유 해제
        /// </summary>
        public void ReleasePointer(int pointerId, IInputLayer layer)
        {
            if (_activePointers.TryGetValue(pointerId, out var currentLayer))
            {
                if (currentLayer == layer)
                {
                    _activePointers.Remove(pointerId);
                    DebugLog($"포인터 {pointerId} 해제: {layer.LayerName}");
                }
            }
        }

        /// <summary>
        /// 특정 포인터가 사용 가능한지 확인
        /// </summary>
        public bool IsPointerAvailable(int pointerId)
        {
            return !_activePointers.ContainsKey(pointerId);
        }

        /// <summary>
        /// 모든 입력 리셋
        /// </summary>
        public void ResetAllInput()
        {
            foreach (var layer in _inputLayers)
            {
                layer.ResetInput();
            }

            _activePointers.Clear();
            DebugLog("모든 입력 리셋");
        }

        /// <summary>
        /// 특정 레이어만 활성화
        /// </summary>
        public void EnableOnlyLayer<T>() where T : class, IInputLayer
        {
            foreach (var layer in _inputLayers)
            {
                if (layer is T)
                    layer.Enable();
                else
                    layer.Disable();
            }
        }

        /// <summary>
        /// 모든 레이어 활성화
        /// </summary>
        public void EnableAllLayers()
        {
            foreach (var layer in _inputLayers)
            {
                layer.Enable();
            }
        }

        /// <summary>
        /// 모든 레이어 비활성화
        /// </summary>
        public void DisableAllLayers()
        {
            foreach (var layer in _inputLayers)
            {
                layer.Disable();
            }
        }

        private void UpdateLayersForMode()
        {
            switch (_currentMode)
            {
                case InputMode.Normal:
                    // 모든 레이어 활성화
                    EnableAllLayers();
                    break;

                case InputMode.Chatting:
                    // 이동/액션 비활성화, UI만 활성
                    foreach (var layer in _inputLayers)
                    {
                        if (layer.LayerName.Contains("UI") || layer.LayerName.Contains("Chat"))
                            layer.Enable();
                        else
                            layer.Disable();
                    }
                    break;

                case InputMode.UIInteraction:
                    // 카메라 회전 비활성화
                    foreach (var layer in _inputLayers)
                    {
                        if (layer.LayerName.Contains("Camera"))
                            layer.Disable();
                        else
                            layer.Enable();
                    }
                    break;

                case InputMode.Inventory:
                    // 이동 비활성화
                    foreach (var layer in _inputLayers)
                    {
                        if (layer.LayerName.Contains("Joystick") || layer.LayerName.Contains("Movement"))
                            layer.Disable();
                        else
                            layer.Enable();
                    }
                    break;

                case InputMode.Cutscene:
                case InputMode.Paused:
                    // 모든 게임플레이 입력 비활성화
                    DisableAllLayers();
                    break;
            }
        }

        private void OnDestroy()
        {
            // 정리
            foreach (var layer in _inputLayers)
            {
                layer.Cleanup();
            }

            _inputLayers.Clear();
            _activePointers.Clear();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // 앱이 백그라운드로 갈 때 모든 입력 리셋
                ResetAllInput();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // 포커스를 잃을 때 모든 입력 리셋
                ResetAllInput();
            }
        }

        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                UnityEngine.Debug.Log($"[InputCoordinator] {message}");
            }
        }
    }
}