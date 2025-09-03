package com.ggumtle.ggumtle.friend.application;


import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.errorCode.FriendErrorCode;
import com.ggumtle.ggumtle.friend.application.command.FriendRequestCommand;
import com.ggumtle.ggumtle.friend.application.result.FriendRequestResult;
import com.ggumtle.ggumtle.friend.domain.Friend;
import com.ggumtle.ggumtle.friend.domain.Status;
import com.ggumtle.ggumtle.friend.persistence.FriendRepository;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.member.persistence.MemberRepository;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;


@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class FriendService {
    private final FriendRepository friendRepository;
    private final MemberRepository memberRepository;

    @Transactional
    public FriendRequestResult requestFriend(FriendRequestCommand command) {
        Long requesterId = command.requesterId();
        Long targetMemgerId = command.targetMemberId();

        if (requesterId.equals(targetMemgerId)) {
            throw new GgumtleException(FriendErrorCode.CANNOT_REQUEST_SELF);
        }

        Member requester = memberRepository.findById(requesterId)
                .orElseThrow(() -> new GgumtleException(FriendErrorCode.TARGET_NOT_FOUND));
        Member target = memberRepository.findById(targetMemgerId)
                .orElseThrow(() -> new GgumtleException(FriendErrorCode.TARGET_NOT_FOUND));

        if (friendRepository.areFriends(requesterId, targetMemgerId)) {
            throw new GgumtleException(FriendErrorCode.ALREADY_FRIEND);
        }

        boolean pendingAB = friendRepository.existsByFollower_IdAndFollowee_IdAndStatus(requesterId, targetMemgerId, Status.PENDING);
        boolean pendingBA = friendRepository.existsByFollower_IdAndFollowee_IdAndStatus(targetMemgerId, requesterId, Status.PENDING);

        if (pendingAB || pendingBA) {
            throw new GgumtleException(FriendErrorCode.ALREADY_REQUEST);
        }

        Friend friend = Friend.request(requester,target);
        friendRepository.save(friend);

        return FriendRequestResult.of(requesterId,targetMemgerId);
    }
}
