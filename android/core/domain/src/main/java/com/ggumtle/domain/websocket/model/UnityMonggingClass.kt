package com.ggumtle.domain.websocket.model


enum class UnityMonggingClass (val type: String, val classId: Long){
    HEAL("HealMongging",1), HP("HpMongging",2), JOB("JobMongging",3);

    companion object {
        fun fromClassId(classId: Long): UnityMonggingClass {
            return entries.find { it.classId == classId } ?: HEAL
        }
    }
}

fun String.toUnityMonggingClass(): UnityMonggingClass {
    return when (this.lowercase()) {
        "heal" -> UnityMonggingClass.HEAL
        "physical" -> UnityMonggingClass.HP
        "work" -> UnityMonggingClass.JOB
        else -> UnityMonggingClass.HEAL
    }
}