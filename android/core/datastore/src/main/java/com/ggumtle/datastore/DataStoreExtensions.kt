package com.ggumtle.datastore

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

/**
 * DataStore Preferences Key Extensions
 * 재사용 가능한 확장 함수들
 */

fun <T> Preferences.Key<T>.flowIn(store: DataStore<Preferences>): Flow<T?> =
    store.data.map { it[this] }

suspend fun <T> Preferences.Key<T>.saveTo(store: DataStore<Preferences>, value: T) =
    store.edit { it[this] = value }

suspend fun <T> Preferences.Key<T>.deleteFrom(store: DataStore<Preferences>) =
    store.edit { it.remove(this) }

suspend fun <T> Preferences.Key<T>.updateIn(
    store: DataStore<Preferences>,
    transform: (currentValue: T?) -> T
) = store.edit { preferences ->
    val currentValue = preferences[this]
    preferences[this] = transform(currentValue)
}

val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "auth_preferences")
