package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.domain.PartyInvitation;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface PartyInvitationRepository extends CrudRepository<PartyInvitation, String> {
    List<PartyInvitation> findAllByInviteeId(Long inviteeId);

    boolean existsByPartyIdAndInviteeId(String partyId, Long inviteeId);

    void deleteAllByInviterId(Long inviterId);
}
