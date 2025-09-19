package com.ggumtle.ggumtle.mongging.presentation;

import com.ggumtle.ggumtle.common.LoginUser;
import com.ggumtle.ggumtle.mongging.application.MonggingService;
import com.ggumtle.ggumtle.mongging.application.result.EnhanceMonggingResult;
import com.ggumtle.ggumtle.mongging.presentation.response.EnhanceMonggingResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequiredArgsConstructor
@RequestMapping("/mongging")
public class MonggingController {

    private final MonggingService monggingService;

    @PatchMapping("/{mongging-id}/enhance")
    public ResponseEntity<EnhanceMonggingResponse> enhanceMongging(
            @LoginUser Long memberId,
            @PathVariable("mongging-id") Long monggingId
    ) {
        EnhanceMonggingResult result = monggingService.enhanceMongging(memberId, monggingId);
        return ResponseEntity.ok(EnhanceMonggingResponse.from(result));
    }
}
