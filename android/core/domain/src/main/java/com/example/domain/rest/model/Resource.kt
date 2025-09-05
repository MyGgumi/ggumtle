package com.example.domain.rest.model

import com.example.common.constant.HttpStatus

sealed class Resource<out R> {
    data object Loading : Resource<Nothing>()

    data class Success<out T>(
        val data: T
    ) : Resource<T>()

    data class Failure(
        val errorMessage: String,
        val code: Int,
    ) : Resource<Nothing>() {
        fun printError(): String {
            return "code : $code, msg : $errorMessage"
        }

        companion object {
            val NullData = Failure(
                errorMessage = "data is null",
                code = HttpStatus.INTERNAL_SERVER_ERROR
            )
        }
    }
}
