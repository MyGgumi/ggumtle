package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.domain.PartyInvitation;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface PartyInvitationRepository extends CrudRepository<PartyInvitation, String> {
}
