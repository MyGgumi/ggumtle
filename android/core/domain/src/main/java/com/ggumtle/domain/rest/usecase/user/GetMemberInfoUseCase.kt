package com.ggumtle.domain.rest.usecase.user

import com.ggumtle.domain.rest.model.member.response.MemberInfoResponse
import com.ggumtle.domain.rest.repository.MemberRepository
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class GetMemberInfoUseCase @Inject constructor(
    private val memberRepository: MemberRepository
) {
    operator fun invoke(): Flow<Resource<MemberInfoResponse>> {
        return memberRepository.getMemberInfo()
    }
}