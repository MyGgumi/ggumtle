using Features.PlayerList.Models;
using R3;

namespace Features.Player.Services
{
    public interface IPlayerRoleService
    {
        ReadOnlyReactiveProperty<PlayerRole> CurrentRole { get; }
        void ChangeRole(PlayerRole newRole);
        bool CanInteract();
    }
}