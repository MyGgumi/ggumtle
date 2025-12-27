package com.ggumtle.loadtest.player

/**
 * Represents the current state of a virtual player
 */
enum class PlayerState {
    DISCONNECTED,
    CONNECTED,
    AUTHENTICATING,
    AUTHENTICATED,
    JOINING_ROOM,
    IN_ROOM,
    LOADING_SCENE,
    WAITING_FOR_START,
    IN_GAME,
    GAME_ENDED,
    ERROR
}
