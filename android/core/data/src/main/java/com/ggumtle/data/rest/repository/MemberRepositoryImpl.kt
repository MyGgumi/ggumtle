package com.ggumtle.data.rest.repository

import com.ggumtle.data.rest.model.member.request.toDto
import com.ggumtle.data.rest.model.member.response.toDomain
import com.ggumtle.data.rest.remote.datasource.MemberRemoteDataSource
import com.ggumtle.data.rest.util.executeApiCall
import com.ggumtle.domain.rest.model.member.request.EditNicknameRequest
import com.ggumtle.domain.rest.model.member.response.EditNicknameResponse
import com.ggumtle.domain.rest.model.member.response.MemberCoinResponse
import com.ggumtle.domain.rest.model.member.response.MemberInfoResponse
import com.ggumtle.domain.rest.repository.MemberRepository
import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.domain.rest.model.member.response.DeleteAccountResponse
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class MemberRepositoryImpl @Inject constructor(
    private val remoteDataSource: MemberRemoteDataSource
) : MemberRepository {

    override fun getMemberInfo(): Flow<Resource<MemberInfoResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.getMemberInfo() },
            mapper = { it.toDomain() },
            defaultErrorMessage = "사용자 정보 조회에 실패했습니다"
        )

    override fun editNickname(request: EditNicknameRequest): Flow<Resource<EditNicknameResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.editNickname(request.toDto()) },
            mapper = { it.toDomain() },
            defaultErrorMessage = "닉네임 수정에 실패했습니다"
        )

    override fun getMemberCoin(): Flow<Resource<MemberCoinResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.getMemberCoin() },
            mapper = { it.toDomain() },
            defaultErrorMessage = "코인 조회에 실패했습니다"
        )

    override fun deleteAccount(): Flow<Resource<DeleteAccountResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.deleteAccount() },
            mapper = { it.toDomain() },
            defaultErrorMessage = "회원탈퇴에 실패했습니다"
        )
}