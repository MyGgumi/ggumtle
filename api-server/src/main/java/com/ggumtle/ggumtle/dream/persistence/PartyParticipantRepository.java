package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface PartyParticipantRepository extends CrudRepository<PartyParticipant, Long> {
    List<PartyParticipant> findAllByPartyId(String partyId);
}
