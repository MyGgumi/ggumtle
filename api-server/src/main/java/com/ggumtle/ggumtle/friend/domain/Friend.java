package com.ggumtle.ggumtle.friend.domain;

import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.FriendErrorCode;
import com.ggumtle.ggumtle.member.domain.Member;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.util.Objects;


@Entity
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Getter
public class Friend {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "follower_id", nullable = false)
    private Member follower;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "followee_id", nullable = false)
    private Member followee;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private Status status;

    private Friend(Member follower, Member followee, Status status){
        if (Objects.equals(follower.getId(), followee.getId())){
            throw new GgumtleException(FriendErrorCode.CANNOT_REQUEST_SELF);
        }
        this.follower = follower;
        this.followee = followee;
        this.status = Status.PENDING;
    }

    public static Friend request(Member follower, Member followee) {
        return new Friend(follower, followee, Status.PENDING);
    }

    public void accept() {
        switch (this.status) {
            case PENDING -> this.status = Status.ACCEPTED;
            case ACCEPTED -> throw new GgumtleException(FriendErrorCode.ALREADY_FRIEND);
        }
    }
}
