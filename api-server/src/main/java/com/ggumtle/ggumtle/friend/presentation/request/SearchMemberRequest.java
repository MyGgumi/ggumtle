package com.ggumtle.ggumtle.friend.presentation.request;

import com.ggumtle.ggumtle.friend.application.command.SearchMemberCommand;

public record SearchMemberRequest(
        String keyword,
        int page,
        int size
) {
    public SearchMemberCommand toCommand(Long requesterId){
        int page0 = Math.max(0, this.page() - 1);
        return new SearchMemberCommand(requesterId, this.keyword(), page0, this.size());
    }
}
