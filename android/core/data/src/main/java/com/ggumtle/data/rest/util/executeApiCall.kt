package com.ggumtle.data.rest.util

import android.util.Log
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
            Log.d("executeApiCall", "result : ${result.data}")
            emit(Resource.Success(mapper(result.data)))
        }
        is NetworkResult.Error -> {
            Log.e("executeApiCall", "API Error: ${result.message}, code: ${result.code}")
            emit(Resource.Failure(
                errorMessage = result.message ?: defaultErrorMessage,
                code = result.code
            ))
        }
        is NetworkResult.Exception -> {
            Log.e("executeApiCall", "API Exception: ${result.e.message}", result.e)
            emit(Resource.Failure(
                errorMessage = result.e.message ?: "네트워크 오류가 발생했습니다",
                code = HttpStatus.INTERNAL_SERVER_ERROR
            ))
        }
    }
}