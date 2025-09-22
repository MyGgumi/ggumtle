using Features.PlayerList.Models;

namespace Features.Player.Messages
{
    public readonly struct PlayerRoleChangedMessage
    {
        public readonly PlayerRole previousRole;
        public readonly PlayerRole newRole;
        public readonly bool canInteract;

        public PlayerRoleChangedMessage(PlayerRole previousRole, PlayerRole newRole, bool canInteract)
        {
            this.previousRole = previousRole;
            this.newRole = newRole;
            this.canInteract = canInteract;
        }
    }
}