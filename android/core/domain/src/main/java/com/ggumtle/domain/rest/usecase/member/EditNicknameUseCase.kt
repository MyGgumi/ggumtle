package com.ggumtle.domain.rest.usecase.member

import com.ggumtle.domain.rest.model.member.request.EditNicknameRequest
import com.ggumtle.domain.rest.model.member.response.EditNicknameResponse
import com.ggumtle.domain.rest.repository.MemberRepository
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class EditNicknameUseCase @Inject constructor(
    private val memberRepository: MemberRepository
) {
    operator fun invoke(nickname: String): Flow<Resource<EditNicknameResponse>> {
        return memberRepository.editNickname(EditNicknameRequest(nickname))
    }
}