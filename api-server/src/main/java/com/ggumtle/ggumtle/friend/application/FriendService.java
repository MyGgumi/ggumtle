package com.ggumtle.ggumtle.friend.application;


import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.FriendErrorCode;
import com.ggumtle.ggumtle.friend.application.command.AcceptFriendRequestCommand;
import com.ggumtle.ggumtle.friend.application.command.GetFriendRequestsCommand;
import com.ggumtle.ggumtle.friend.application.command.GetFriendsCommand;
import com.ggumtle.ggumtle.friend.application.command.RejectFriendRequestCommand;
import com.ggumtle.ggumtle.friend.application.command.RequestFriendCommand;
import com.ggumtle.ggumtle.friend.application.command.SearchMemberCommand;
import com.ggumtle.ggumtle.friend.application.result.AcceptFriendRequestResult;
import com.ggumtle.ggumtle.friend.application.result.GetFriendRequestsResult;
import com.ggumtle.ggumtle.friend.application.result.GetFriendsResult;
import com.ggumtle.ggumtle.friend.application.result.RejectFriendRequestResult;
import com.ggumtle.ggumtle.friend.application.result.RequestFriendsResult;
import com.ggumtle.ggumtle.friend.application.result.SearchMemberResult;
import com.ggumtle.ggumtle.friend.domain.Friend;
import com.ggumtle.ggumtle.friend.domain.Status;
import com.ggumtle.ggumtle.friend.persistence.FriendRepository;
import com.ggumtle.ggumtle.friend.persistence.po.MemberPo;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.member.persistence.MemberRepository;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Slice;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;


@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class FriendService {
    private final FriendRepository friendRepository;
    private final MemberRepository memberRepository;

    @Transactional
    public RequestFriendsResult requestFriend(RequestFriendCommand command) {
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

        return RequestFriendsResult.of(requesterId,targetMemgerId);
    }

    @Transactional(readOnly = true)
    public GetFriendsResult getFriends(GetFriendsCommand command) {
        Long memberId = command.memberId();

        List<Friend> asFollower = friendRepository.findAllByFollower_IdAndStatus(memberId, Status.ACCEPTED);
        List<Friend> asFollowee = friendRepository.findAllByFollowee_IdAndStatus(memberId, Status.ACCEPTED);

        List<Member> friends = new ArrayList<>();

        asFollower.forEach(f -> friends.add(f.getFollowee()));
        asFollowee.forEach(f -> friends.add(f.getFollower()));

        return GetFriendsResult.of(friends);
    }

    @Transactional(readOnly = true)
    public GetFriendRequestsResult getFriendRequests(GetFriendRequestsCommand command) {
        Long memberId = command.memberId();

        List<Friend> asFollowee = friendRepository.findAllByFollowee_IdAndStatus(memberId, Status.PENDING);

        return  GetFriendRequestsResult.of(asFollowee);
    }

    @Transactional
    public AcceptFriendRequestResult acceptFriendRequest(AcceptFriendRequestCommand command) {
        Long friendId = command.friendId();
        Long loginMemberId = command.loginMemberId();
        Friend friend = friendRepository.findByIdAndFollowee_Id(friendId, loginMemberId)
                .orElseThrow(() -> new GgumtleException(FriendErrorCode.REQUEST_NOT_FOUND));

        friend.accept();
        Long followerId = friend.getFollower().getId();
        String nickname = friend.getFollower().getNickname();

        return AcceptFriendRequestResult.of(followerId,nickname);
    }

    @Transactional
    public RejectFriendRequestResult rejectFriendRequest(RejectFriendRequestCommand command) {
        Long friendId = command.friendId();
        Long loginMemberId = command.loginMemberId();
        Friend friend = friendRepository.findByIdAndFollowee_Id(friendId, loginMemberId)
                .orElseThrow(() -> new GgumtleException(FriendErrorCode.REQUEST_NOT_FOUND));

        Long followerId = friend.getFollower().getId();
        String nickname = friend.getFollower().getNickname();

        friendRepository.delete(friend);
        return RejectFriendRequestResult.of(followerId,nickname);
    }

    @Transactional(readOnly = true)
    public SearchMemberResult searchMember(SearchMemberCommand command) {
        Slice<MemberPo> poSlice = friendRepository.searchMembers(
                command.requesterId(),
                command.keyword(),
                PageRequest.of(command.page(), command.size())
        );

        return SearchMemberResult.fromPoSlice(poSlice);
    }
}
