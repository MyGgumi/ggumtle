package com.ggumtle.ggumtle.member.application;

import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.MemberErrorCode;
import com.ggumtle.ggumtle.member.application.command.GetCoinCommand;
import com.ggumtle.ggumtle.member.application.result.GetCoinResult;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.member.persistence.MemberRepository;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class MemberService {
    private final MemberRepository memberRepository;

    public GetCoinResult getCoin(GetCoinCommand command) {
        Long memberId = command.memberId();

        Member member = memberRepository.findById(memberId)
                .orElseThrow(()-> new GgumtleException(MemberErrorCode.NOT_FOUND));
        int coin = member.getCoin();

        return new GetCoinResult(memberId,coin);
    }
}
