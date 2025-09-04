package com.ggumtle.ggumtle.messaging;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.ggumtle.ggumtle.common.property.GameServerProperty;
import com.ggumtle.ggumtle.messaging.message.CreatedRoomMessage;
import com.ggumtle.ggumtle.messaging.message.RequestRoomMessage;
import com.ggumtle.ggumtle.room.application.RoomManager;
import com.ggumtle.ggumtle.room.domain.Room;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.redis.connection.Message;
import org.springframework.data.redis.connection.MessageListener;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Component;

@Component
@Slf4j
public class RoomMessageListener implements MessageListener {
    private static final String CREATED_ROOM_CHANNEL = "created_room";

    private final String gameServerId;
    private final ObjectMapper objectMapper;
    private final RedisTemplate<String, String> redisTemplate;
    private final RoomManager roomManager;

    @Autowired
    protected RoomMessageListener(
            GameServerProperty gameServerProperty,
            ObjectMapper objectMapper,
            RedisTemplate<String, String> redisTemplate,
            RoomManager roomManager) {
        this.gameServerId = gameServerProperty.getId();
        this.objectMapper = objectMapper;
        this.redisTemplate = redisTemplate;
        this.roomManager = roomManager;
    }

    @Override
    public void onMessage(Message message, byte[] pattern) {
        try {
            RequestRoomMessage requestRoom = objectMapper.readValue(message.getBody(), RequestRoomMessage.class);

            Room room = roomManager.createRoom(requestRoom.playerIds());

            CreatedRoomMessage createdRoomMessage = new CreatedRoomMessage(room.getRoomId(), requestRoom.requestId(), this.gameServerId);
            String json =  objectMapper.writeValueAsString(createdRoomMessage);

            redisTemplate.convertAndSend(CREATED_ROOM_CHANNEL, json);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
