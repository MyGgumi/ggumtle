package com.example.datastore.di

import android.content.Context
import androidx.datastore.core.DataStore
import com.example.common.network.di.ApplicationScope
import com.example.datastore.AuthDataStore
import com.example.datastore.AuthManager
import com.example.datastore.GoogleAuthManager
import com.example.datastore.dataStore
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import kotlinx.coroutines.CoroutineScope
import javax.inject.Singleton


@Module
@InstallIn(SingletonComponent::class)
object DataStoreModule {

    @Provides
    @Singleton
    fun provideGoogleAuthManager(
        @ApplicationContext context: Context
    ): GoogleAuthManager = GoogleAuthManager(context)

    @Provides
    @Singleton
    fun provideAuthManger(
        authDataStore: AuthDataStore,
        googleAuthManager: GoogleAuthManager,
        @ApplicationScope applicationScope: CoroutineScope,
    ): AuthManager = AuthManager(authDataStore, googleAuthManager, applicationScope)

    @Provides
    @Singleton
    fun provideAuthDataStore(
        dataStore: DataStore<androidx.datastore.preferences.core.Preferences>
    ): AuthDataStore = AuthDataStore(dataStore)

    @Provides
    @Singleton
    fun provideAuthPreferencesDataStore(
        @ApplicationContext context: Context
    ): DataStore<androidx.datastore.preferences.core.Preferences> {
        return context.dataStore
    }
}