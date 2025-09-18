package com.ggumtle.ggumtle.member.presentation;

import com.ggumtle.ggumtle.common.LoginUser;
import com.ggumtle.ggumtle.member.application.MemberService;
import com.ggumtle.ggumtle.member.application.command.GetCoinCommand;
import com.ggumtle.ggumtle.member.application.result.GetCoinResult;
import com.ggumtle.ggumtle.member.persistence.MemberRepository;
import com.ggumtle.ggumtle.member.presentation.response.GetCoinResponse;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;

@Controller
@RequestMapping("/member")
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class MemberController {
    private final MemberService memberService;

    @GetMapping("/coin")
    public ResponseEntity<GetCoinResponse> getCoin(@LoginUser Long memberId) {
        GetCoinCommand command = new GetCoinCommand(memberId);
        GetCoinResult result = memberService.getCoin(command);

        GetCoinResponse response = new GetCoinResponse(result.memberId(), result.coin());

        return ResponseEntity.ok(response);
    }
}
