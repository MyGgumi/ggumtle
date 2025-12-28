package com.ggumtle.loadtest.player

/**
 * Represents the current state of a virtual player
 */
enum class PlayerState {
    DISCONNECTED,
    CONNECTED,
    AUTHENTICATING,
    AUTHENTICATED,
    CREATING_ROOM,
    ROOM_CREATED,
    JOINING_ROOM,
    IN_ROOM,
    LOADING_SCENE,
    WAITING_FOR_START,
    IN_GAME,
    GAME_ENDED,
    ERROR
}
