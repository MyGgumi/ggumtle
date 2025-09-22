package com.ggumtle.data.rest.remote.service

import com.ggumtle.data.rest.model.member.request.EditNicknameRequestDto
import com.ggumtle.data.rest.model.member.response.EditNicknameResponseDto
import com.ggumtle.data.rest.model.member.response.MemberCoinResponseDto
import com.ggumtle.data.rest.model.member.response.MemberInfoResponseDto
import com.ggumtle.network.rest.model.NetworkResult
import retrofit2.http.*

interface MemberService {

    @GET("member/info")
    suspend fun getMemberInfo(): NetworkResult<MemberInfoResponseDto>

    @PATCH("member/info")
    suspend fun editNickname(
        @Body request: EditNicknameRequestDto
    ): NetworkResult<EditNicknameResponseDto>

    @GET("member/coin")
    suspend fun getMemberCoin(): NetworkResult<MemberCoinResponseDto>

}