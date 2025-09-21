package com.ggumtle.ggumtle.member.application;

import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.MemberErrorCode;
import com.ggumtle.ggumtle.member.application.command.GetCoinCommand;
import com.ggumtle.ggumtle.member.application.command.GetMyInfoCommand;
import com.ggumtle.ggumtle.member.application.command.UpdateNicknameCommand;
import com.ggumtle.ggumtle.member.application.result.GetCoinResult;
import com.ggumtle.ggumtle.member.application.result.GetMyInfoResult;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.member.persistence.MemberRepository;
import com.ggumtle.ggumtle.member.presentation.response.UpdateNicknameResponse;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class MemberService {
    private final MemberRepository memberRepository;

    @Transactional(readOnly = true)
    public GetCoinResult getCoin(GetCoinCommand command) {
        Long memberId = command.memberId();

        Member member = memberRepository.findById(memberId)
                .orElseThrow(()-> new GgumtleException(MemberErrorCode.NOT_FOUND));
        int coin = member.getCoin();

        return new GetCoinResult(memberId,coin);
    }

    @Transactional(readOnly = true)
    public GetMyInfoResult getMyInfo(GetMyInfoCommand command) {
        Long memberId = command.memberId();

        Member member = memberRepository.findById(memberId)
                .orElseThrow(()-> new GgumtleException(MemberErrorCode.NOT_FOUND));

        return GetMyInfoResult.from(member);
    }

    @Transactional
    public void updateNickname(UpdateNicknameCommand command) {
        Long memberId = command.memberId();
        String newNickname = command.nickname();

        Member myInfo = memberRepository.findById(memberId)
                .orElseThrow(()-> new GgumtleException(MemberErrorCode.NOT_FOUND));

        memberRepository.findByNickname(newNickname)
                .ifPresent(member -> {
                    if(!member.getId().equals(memberId)){
                        throw new GgumtleException(MemberErrorCode.DUPLICATE_NICKNAME);
                    }
                });

        myInfo.updateNickname(newNickname);
    }
}
