package com.ggumtle.ggumtle.mission.domain;

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
import lombok.RequiredArgsConstructor;

@Entity
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Getter
public class MemberMission {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "member_id")
    private Member member;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "mission_id")
    private Mission mission;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private MissionState state;

    @Column(nullable = false)
    private Integer doneCount;

    public static MemberMission createMemberMission(Member member, Mission mission) {
        MemberMission missionComplete = new MemberMission();
        missionComplete.member = member;
        missionComplete.mission = mission;
        missionComplete.state = MissionState.BEFORE_SUCCESS;
        missionComplete.doneCount = 0;
        return missionComplete;
    }

    public boolean doMission() {
        if (this.doneCount >= mission.getRequiredCount()) {
            return false;
        }

        doneCount++;
        if (this.doneCount >= mission.getRequiredCount()) {
            this.state = MissionState.SUCCESS;
        }
        return true;
    }
}
