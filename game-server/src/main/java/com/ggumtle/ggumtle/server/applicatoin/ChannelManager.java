package com.ggumtle.ggumtle.server.applicatoin;

import com.ggumtle.ggumtle.auth.JwtService;
import io.netty.channel.Channel;
import io.netty.channel.group.ChannelGroup;
import io.netty.channel.group.DefaultChannelGroup;
import io.netty.util.AttributeKey;
import io.netty.util.concurrent.GlobalEventExecutor;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

@Slf4j
@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class ChannelManager {

    private static final AttributeKey<Boolean> authAttributeKey = AttributeKey.valueOf("authenticated");
    private static final AttributeKey<Long> memberIdAttributeKey = AttributeKey.valueOf("memberId");

    private final ChannelGroup channels = new DefaultChannelGroup(GlobalEventExecutor.INSTANCE);
    private final JwtService jwtService;

    public void addChannel(Channel channel) {
        channels.add(channel);
    }

    public void removeChannel(Channel channel) {
        channels.remove(channel);
    }

    public int getChannelCount() {
        return channels.size();
    }

    public boolean authorizeChannel(Channel channel, String accessToken) {
        if (channel.attr(authAttributeKey).get() != null) {
            log.warn("[{}] 채널 인증: 본 채널은 이미 {}번 사용자로 인증되었습니다", channel.id(), channel.attr(memberIdAttributeKey).get());

            return true;
        }

        if (!jwtService.verifyToken(accessToken)) {
            log.info("[{}] 채널 인증: 토큰이 유효하지 않습니다", channel.id());

            return false;
        }

        long memberId = jwtService.parseId(accessToken);
        channel.attr(authAttributeKey).set(true);
        channel.attr(memberIdAttributeKey).set(memberId);

        log.info("[{}] 채널 인증: 본 채널을 {}번 사용자로 인증하였습니다", channel.id(), channel.attr(memberIdAttributeKey).get());

        return true;
    }

    public static boolean isAuthenticated(Channel channel) {
        return channel.attr(authAttributeKey).get();
    }

    public static Long getMemberId(Channel channel) {
        return channel.attr(memberIdAttributeKey).get();
    }

    public long getPendingWriteCount() {
        long totalPending = 0;
        for (Channel ch : channels) {
            if (!ch.isWritable()) {
                totalPending += 1;
            }
        }
        return totalPending;
    }
}
