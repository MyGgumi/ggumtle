package com.ggumtle.domain.rest.usecase.member

import com.ggumtle.domain.rest.model.member.response.DeleteAccountResponse
import com.ggumtle.domain.rest.repository.MemberRepository
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class DeleteAccountUseCase @Inject constructor(
    private val memberRepository: MemberRepository
) {
    operator fun invoke(): Flow<Resource<DeleteAccountResponse>> {
        return memberRepository.deleteAccount()
    }
}