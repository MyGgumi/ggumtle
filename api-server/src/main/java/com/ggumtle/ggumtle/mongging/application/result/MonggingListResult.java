package com.ggumtle.ggumtle.mongging.application.result;

import com.ggumtle.ggumtle.mongging.domain.Mongging;
import lombok.AccessLevel;
import lombok.Builder;

import java.util.List;

@Builder(access = AccessLevel.PRIVATE)
public record MonggingListResult(
        List<ResultMonggingDto> monggings
) {

    public static MonggingListResult from(List<Mongging> monggings) {
        return new MonggingListResult(monggings.stream().map(ResultMonggingDto::from).toList());
    }

    @Builder(access = AccessLevel.PRIVATE)
    public record ResultMonggingDto(
            Long id,
            String monggingClass,
            Integer level
    ) {
        public static ResultMonggingDto from(Mongging mongging) {
            return new ResultMonggingDto(mongging.getId(), mongging.getMonggingClass().getName(), mongging.getLevel());
        }
    }
}
