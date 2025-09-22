package com.ggumtle.ggumtle.member.presentation;

import com.ggumtle.ggumtle.common.LoginUser;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.AuthErrorCode;
import com.ggumtle.ggumtle.exception.code.MemberErrorCode;
import com.ggumtle.ggumtle.member.application.MemberService;
import com.ggumtle.ggumtle.member.application.command.GetCoinCommand;
import com.ggumtle.ggumtle.member.application.command.GetMyInfoCommand;
import com.ggumtle.ggumtle.member.application.command.UpdateNicknameCommand;
import com.ggumtle.ggumtle.member.application.command.WithdrawCommand;
import com.ggumtle.ggumtle.member.application.result.GetCoinResult;
import com.ggumtle.ggumtle.member.application.result.GetMyInfoResult;
import com.ggumtle.ggumtle.member.presentation.request.UpdateNicknameRequest;
import com.ggumtle.ggumtle.member.presentation.response.GetCoinResponse;
import com.ggumtle.ggumtle.member.presentation.response.GetMyInfoResponse;
import com.ggumtle.ggumtle.member.presentation.response.UpdateNicknameResponse;
import com.ggumtle.ggumtle.member.presentation.response.WithdrawResponse;
import jakarta.validation.Valid;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.RequestBody;
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

    @GetMapping("/info")
    public ResponseEntity<GetMyInfoResponse>  getMyInfo(@LoginUser Long memberId) {
        GetMyInfoCommand command = new GetMyInfoCommand(memberId);
        GetMyInfoResult result = memberService.getMyInfo(command);

        GetMyInfoResponse response = GetMyInfoResponse.from(result);

        return ResponseEntity.ok(response);
    }

    @PatchMapping("/info")
    public ResponseEntity<UpdateNicknameResponse>  updateNickname(
            @LoginUser Long memberId,
            @Valid @RequestBody UpdateNicknameRequest request) {
        UpdateNicknameCommand command = request.toCommand(memberId);
        memberService.updateNickname(command);

        return ResponseEntity.ok(new UpdateNicknameResponse(true));
    }

    @PatchMapping
    public ResponseEntity<WithdrawResponse> withdraw(@LoginUser Long memberId){
        WithdrawCommand command =  new WithdrawCommand(memberId);
        memberService.withdraw(command);

        return ResponseEntity.ok(new WithdrawResponse(true));
    }
}
