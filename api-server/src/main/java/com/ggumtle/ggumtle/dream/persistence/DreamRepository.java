package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.domain.Dream;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface DreamRepository extends CrudRepository<Dream, Long> {
    Optional<Dream> findByRoomRequestId(String id);
}
