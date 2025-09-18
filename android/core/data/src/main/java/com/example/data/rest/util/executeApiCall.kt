package com.example.data.rest.util

import com.ggumtle.common.constant.HttpStatus
import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.network.rest.model.NetworkResult
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow

fun <T : Any, R : Any> executeApiCall(
    apiCall: suspend () -> NetworkResult<T>,
    mapper: (T) -> R,
    defaultErrorMessage: String
): Flow<Resource<R>> = flow {
    emit(Resource.Loading)

    when (val result = apiCall()) {
        is NetworkResult.Success -> {
            emit(Resource.Success(mapper(result.data)))
        }
        is NetworkResult.Error -> {
            emit(Resource.Failure(
                errorMessage = result.message ?: defaultErrorMessage,
                code = result.code
            ))
        }
        is NetworkResult.Exception -> {
            emit(Resource.Failure(
                errorMessage = result.e.message ?: "네트워크 오류가 발생했습니다",
                code = HttpStatus.INTERNAL_SERVER_ERROR
            ))
        }
    }
}