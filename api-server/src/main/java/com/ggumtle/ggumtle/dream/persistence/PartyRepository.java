package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.domain.Party;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface PartyRepository extends CrudRepository<Party, Long> {
}
