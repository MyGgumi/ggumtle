package com.ggumtle.ggumtle.member.application.command;

public record UpdateNicknameCommand(
        Long memberId,
        String nickname
) {
}
