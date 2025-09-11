package com.ggumtle.ggumtle.friend.persistence;

import com.ggumtle.ggumtle.friend.domain.MemberState;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface MemberStateRepository extends CrudRepository<MemberState,String> {

}
