package com.ggumtle.ggumtle.mongging.presentation.response;

import com.ggumtle.ggumtle.mongging.application.result.MonggingListResult;
import lombok.AccessLevel;
import lombok.Builder;

import java.util.List;

@Builder(access = AccessLevel.PRIVATE)
public record MonggingListResponse(
        List<ResponseMonggingDto> monggings
) {

    public static MonggingListResponse from(MonggingListResult result) {
        return MonggingListResponse.builder()
                .monggings(result.monggings().stream()
                        .map(ResponseMonggingDto::from)
                        .toList())
                .build();
    }

    public record ResponseMonggingDto(
            Long id,
            String monggingClass,
            Integer level
    ) {

        public static ResponseMonggingDto from(MonggingListResult.ResultMonggingDto mongging) {
            return new ResponseMonggingDto(mongging.id(), mongging.monggingClass(), mongging.level());
        }
    }
}
