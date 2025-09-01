package com.ggumtle.ggumtle.room.domain;

import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.session.Session;
import lombok.Getter;
import lombok.RequiredArgsConstructor;

import java.util.List;
import java.util.concurrent.CopyOnWriteArrayList;

@RequiredArgsConstructor
@Getter
public class Room {

    private static final int MAX_MEMBER_COUNT = 6;

    private final long roomId;
    private final List<Long> playerIds;

    // 멀티 쓰레드 환경에서 동시성을 보장하기 위해 CopyOnWriteArrayList 사용
    private final CopyOnWriteArrayList<Session> sessions = new CopyOnWriteArrayList<>();

    public int getSessionCount() {
        return sessions.size();
    }

    public boolean isFull() {
        return getSessionCount() >= MAX_MEMBER_COUNT;
    }

    public boolean isAvailable(int partyMemberCount) {
        return getSessionCount() + partyMemberCount <= MAX_MEMBER_COUNT;
    }

    public void addSession(Session session) {
        sessions.add(session);
    }

    public void removeSession(Session session) {
        sessions.remove(session);
    }

    public void broadcast(Packet packet) {
        sessions.forEach(session -> session.sendPacket(packet));
    }

}
