/**
 * 애플리케이션 유틸리티 및 이벤트 처리
 * - 권한 요청 결과 이벤트 데이터 클래스
 * - equals() 및 hashCode() 올바른 구현
 */
package com.ggumtle.mission.ar.util

data class PermissionResultEvent(val requestCode: Int, val grantResults: IntArray) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as PermissionResultEvent

        if (requestCode != other.requestCode) return false
        if (!grantResults.contentEquals(other.grantResults)) return false

        return true
    }

    override fun hashCode(): Int {
        var result = requestCode
        result = 31 * result + grantResults.contentHashCode()
        return result
    }
}
