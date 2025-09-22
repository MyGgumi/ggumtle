package com.ggumtle.domain.rest.repository

import com.ggumtle.domain.rest.model.member.request.EditNicknameRequest
import com.ggumtle.domain.rest.model.member.response.EditNicknameResponse
import com.ggumtle.domain.rest.model.member.response.MemberCoinResponse
import com.ggumtle.domain.rest.model.member.response.MemberInfoResponse
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow

interface MemberRepository {
    fun getMemberInfo(): Flow<Resource<MemberInfoResponse>>
    fun editNickname(request: EditNicknameRequest): Flow<Resource<EditNicknameResponse>>
    fun getMemberCoin(): Flow<Resource<MemberCoinResponse>>
}