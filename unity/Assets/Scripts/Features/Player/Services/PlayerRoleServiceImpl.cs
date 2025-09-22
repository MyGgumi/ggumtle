using Features.PlayerList.Models;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Player.Services
{
    public class PlayerRoleServiceImpl : IPlayerRoleService
    {
        private readonly ReactiveProperty<PlayerRole> _currentRole = new(PlayerRole.Mongging);
        public ReadOnlyReactiveProperty<PlayerRole> CurrentRole => _currentRole;

        private readonly bool _enableDebugLogs = true;

        [Inject]
        public PlayerRoleServiceImpl()
        {
            DebugLog("PlayerRole Service 초기화 완료");
        }

        public void ChangeRole(PlayerRole newRole)
        {
            if (_currentRole.Value != newRole)
            {
                var oldRole = _currentRole.Value;
                _currentRole.Value = newRole;
                DebugLog($"플레이어 역할 변경: {oldRole} -> {newRole}");
            }
        }

        public bool CanInteract()
        {
            return _currentRole.Value == PlayerRole.Mongging;
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[PlayerRoleService] {message}");
        }
    }
}