package com.ggumtle.data.rest.remote.datasource

import com.ggumtle.data.rest.model.member.request.EditNicknameRequestDto
import com.ggumtle.data.rest.model.member.response.EditNicknameResponseDto
import com.ggumtle.data.rest.model.member.response.MemberCoinResponseDto
import com.ggumtle.data.rest.model.member.response.MemberInfoResponseDto
import com.ggumtle.data.rest.model.member.response.DeleteAccountResponseDto
import com.ggumtle.data.rest.remote.service.MemberService
import com.ggumtle.network.rest.model.NetworkResult
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class MemberRemoteDataSource @Inject constructor(
    private val memberService: MemberService
) {
    suspend fun getMemberInfo(): NetworkResult<MemberInfoResponseDto> =
        memberService.getMemberInfo()

    suspend fun editNickname(request: EditNicknameRequestDto): NetworkResult<EditNicknameResponseDto> =
        memberService.editNickname(request)

    suspend fun getMemberCoin(): NetworkResult<MemberCoinResponseDto> =
        memberService.getMemberCoin()

    suspend fun deleteAccount(): NetworkResult<DeleteAccountResponseDto> =
        memberService.deleteAccount()
}