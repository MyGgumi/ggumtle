package com.example.datastore

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.stringPreferencesKey
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthDataStore @Inject constructor(
    private val dataStore: DataStore<Preferences>
) {

    companion object {
        private val ACCESS_TOKEN = stringPreferencesKey("access_token")
        private val MEMBER_ID = stringPreferencesKey("member_id")
//        private val REFRESH_TOKEN = stringPreferencesKey("refresh_token")
//        private val USER_EMAIL = stringPreferencesKey("user_email")
    }

    val accessTokenFlow: Flow<String?> = ACCESS_TOKEN.flowIn(dataStore)
    val memberIdFlow: Flow<String?> = MEMBER_ID.flowIn(dataStore)
//    val refreshTokenFlow: Flow<String?> = REFRESH_TOKEN.flowIn(dataStore)
//    val userEmailFlow: Flow<String?> = USER_EMAIL.flowIn(dataStore)
    suspend fun saveMemberId(memberId: Long) = MEMBER_ID.saveTo(dataStore, memberId.toString())
    suspend fun deleteMemberId() = MEMBER_ID.deleteFrom(dataStore)
    suspend fun saveAccessToken(token: String) = ACCESS_TOKEN.saveTo(dataStore, token)
    suspend fun deleteAccessToken() = ACCESS_TOKEN.deleteFrom(dataStore)
//    suspend fun saveRefreshToken(token: String) = REFRESH_TOKEN.saveTo(dataStore, token)
//    suspend fun deleteRefreshToken() = REFRESH_TOKEN.deleteFrom(dataStore)
//    suspend fun saveUserEmail(email: String) = USER_EMAIL.saveTo(dataStore, email)
//    suspend fun deleteUserEmail() = USER_EMAIL.deleteFrom(dataStore)

    suspend fun clearAll() {
        deleteAccessToken()
        deleteMemberId()
//        deleteRefreshToken()
//        deleteUserEmail()
    }
}