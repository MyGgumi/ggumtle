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
import lombok.NoArgsConstructor;

@Entity
@NoArgsConstructor(access = AccessLevel.PROTECTED)
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

    @Column(nullable = false)
    private Boolean isDeleted;

    public MemberMission(Member member, Mission mission) {
        this.member = member;
        this.mission = mission;
        this.state = MissionState.BEFORE_SUCCESS;
        this.doneCount = 0;
        this.isDeleted = false;
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

    public void getReward() {
        this.member.increaseCoinCappedToMax(this.mission.getRewardCoinAmount());
        this.state = MissionState.AFTER_REWARD;
    }

    public void deleteMemberMission(){
        this.isDeleted = true;
    }
}
