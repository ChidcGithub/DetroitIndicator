package com.chidc.affectionwear.data

import android.content.Context
import org.json.JSONArray
import org.json.JSONObject
import java.util.UUID

data class Preset(
    val id: String,
    val name: String,
    val adjective: String,
    val command: String,
    val label: String
)

data class StateOption(
    val label: String,
    val command: String
)

val STATE_OPTIONS = listOf(
    StateOption("Small +", "inc"),
    StateOption("Small \u2212", "dec"),
    StateOption("Big +", "inc+"),
    StateOption("Big \u2212", "dec+"),
    StateOption("Unlock", "unlock"),
    StateOption("Lock", "lock"),
    StateOption("Name only", "neutral")
)

class PresetStore(context: Context) {
    private val prefs = context.applicationContext
        .getSharedPreferences(PREFS, Context.MODE_PRIVATE)

    val bridgeUrl: String
        get() = prefs.getString(KEY_URL, DEFAULT_URL)
            ?.trim()
            ?.trimEnd('/')
            ?.ifBlank { DEFAULT_URL }
            ?: DEFAULT_URL

    val deviceId: String
        get() = prefs.getString(KEY_ID, "")?.trim()?.uppercase() ?: ""

    fun saveConnection(url: String, id: String) {
        prefs.edit()
            .putString(KEY_URL, url.trim().trimEnd('/').ifBlank { DEFAULT_URL })
            .putString(KEY_ID, id.trim().uppercase())
            .apply()
    }

    fun loadPresets(): List<Preset> {
        val raw = prefs.getString(KEY_PRESETS, null)
        if (raw != null) {
            return parsePresets(raw)
        }

        val legacyName = prefs.getString(LEGACY_NAME, "")?.trim().orEmpty()
        val legacyAdjective = prefs.getString(LEGACY_ADJECTIVE, "")?.trim().orEmpty()
        if (legacyName.isEmpty() || legacyAdjective.isEmpty()) {
            return emptyList()
        }

        val legacy = Preset(
            id = UUID.randomUUID().toString(),
            name = legacyName.uppercase(),
            adjective = legacyAdjective.uppercase(),
            command = prefs.getString(LEGACY_COMMAND, "inc") ?: "inc",
            label = prefs.getString(LEGACY_LABEL, "Small +") ?: "Small +"
        )
        val migrated = listOf(legacy)
        savePresets(migrated)
        prefs.edit()
            .remove(LEGACY_NAME)
            .remove(LEGACY_ADJECTIVE)
            .remove(LEGACY_COMMAND)
            .remove(LEGACY_LABEL)
            .remove(LEGACY_STATE)
            .apply()
        return migrated
    }

    fun savePresets(presets: List<Preset>) {
        val array = JSONArray()
        presets.forEach { preset ->
            array.put(JSONObject().apply {
                put("id", preset.id)
                put("name", preset.name)
                put("adjective", preset.adjective)
                put("command", preset.command)
                put("label", preset.label)
            })
        }
        prefs.edit().putString(KEY_PRESETS, array.toString()).apply()
    }

    private fun parsePresets(raw: String): List<Preset> {
        return try {
            val array = JSONArray(raw)
            buildList {
                for (index in 0 until array.length()) {
                    val item = array.optJSONObject(index) ?: continue
                    val name = item.optString("name").trim()
                    val adjective = item.optString("adjective").trim()
                    val command = item.optString("command").trim()
                    if (name.isEmpty() || adjective.isEmpty() || command.isEmpty()) {
                        continue
                    }
                    add(
                        Preset(
                            id = item.optString("id").ifBlank { UUID.randomUUID().toString() },
                            name = name.uppercase(),
                            adjective = adjective.uppercase(),
                            command = command,
                            label = item.optString("label").ifBlank { command }
                        )
                    )
                }
            }
        } catch (_: Exception) {
            emptyList()
        }
    }

    companion object {
        const val DEFAULT_URL = "http://10.0.2.2:5210"

        private const val PREFS = "affection_wear"
        private const val KEY_ID = "device_id"
        private const val KEY_URL = "bridge_url"
        private const val KEY_PRESETS = "presets_json"

        private const val LEGACY_NAME = "preset_name"
        private const val LEGACY_ADJECTIVE = "preset_adjective"
        private const val LEGACY_COMMAND = "preset_command"
        private const val LEGACY_LABEL = "preset_label"
        private const val LEGACY_STATE = "preset_state"
    }
}
