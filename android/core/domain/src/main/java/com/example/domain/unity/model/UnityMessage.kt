package com.example.domain.unity.model

data class UnityMessage(
    val target: String,
    val methodName: String,
    val params: List<Any>
)