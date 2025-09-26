package com.ggumtle.domain.websocket.model


enum class UnityMonggingClass (val type: String){
    HP("HpMongging"), HEAL("JobMongging"), JOB("HealMongging")
}

fun String.toUnityMonggingClass(): UnityMonggingClass {
    return when (this.lowercase()) {
        "heal" -> UnityMonggingClass.HEAL
        "physical" -> UnityMonggingClass.HP
        "work" -> UnityMonggingClass.JOB
        else -> UnityMonggingClass.HEAL
    }
}