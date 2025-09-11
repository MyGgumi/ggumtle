package com.ggumtle.ggumtle.friend.application;

import com.ggumtle.ggumtle.friend.domain.MemberState;
import com.ggumtle.ggumtle.friend.persistence.MemberStateRepository;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class MemberStateService {
    public enum State {ONLINE, INGAME, OFFLINE}

    private final MemberStateRepository memberStateRepository;
    private static final long TTL_SECONDS = 300L;

    public void setOnline(Long memberId) {
        long now = Instant.now().toEpochMilli();
        MemberState state = new MemberState(String.valueOf(memberId), State.ONLINE.name() , now,TTL_SECONDS);
        memberStateRepository.save(state);
    }

    public void setInGame(Long memberId) {
        long now = Instant.now().toEpochMilli();
        MemberState state = new MemberState(String.valueOf(memberId), State.INGAME.name(), now,TTL_SECONDS);
        memberStateRepository.save(state);
    }

    public void setOffline(Long memberId) {

        memberStateRepository.deleteById(String.valueOf(memberId));
    }

    public void heartBeat(Long memberId) {
        memberStateRepository.findById(String.valueOf(memberId)).ifPresent(state -> {
            MemberState updatedState = new MemberState(
                    state.getMemberId(),
                    state.getStatus(),
                    Instant.now().toEpochMilli(),
                    TTL_SECONDS
            );
            memberStateRepository.save(updatedState);
        });
    }

    public State getState(Long memberId) {
        return memberStateRepository.findById(String.valueOf(memberId))
                .map(s -> State.INGAME.name().equals(s.getStatus()) ? State.INGAME : State.ONLINE)
                .orElse(State.OFFLINE);
    }

    public Map<Long, State> getStates(List<Long> memberIds) {
        Map<Long, State> result = new HashMap<>();
        for (Long id : memberIds) {
            result.put(id, getState(id));
        }
        return result;
    }

}
