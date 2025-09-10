package com.ggumtle.ggumtle

import android.app.Application
import com.example.datastore.AuthManager
import dagger.hilt.android.HiltAndroidApp
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltAndroidApp
class MyApplication : Application() {
    private val applicationScope = CoroutineScope(SupervisorJob() + Dispatchers.Main)

    @Inject
    lateinit var authManager: AuthManager

    override fun onCreate() {
        super.onCreate()
        applicationScope.launch {
            authManager.checkAutoLogin()
        }

    }
}