package com.chidc.affectionwear.data

import android.net.Uri
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL

sealed interface ConnectionState {
    data object Checking : ConnectionState
    data object Online : ConnectionState
    data object Offline : ConnectionState
    data object Unknown : ConnectionState
    data object Unreachable : ConnectionState
    data class Failed(val message: String) : ConnectionState
}

sealed interface CommandResult {
    data class Success(val message: String) : CommandResult
    data class Failure(val message: String) : CommandResult
    data object Offline : CommandResult
    data object Unknown : CommandResult
    data object Unreachable : CommandResult
}

object BridgeApi {
    suspend fun checkStatus(baseUrl: String, deviceId: String): ConnectionState =
        withContext(Dispatchers.IO) {
            if (deviceId.isBlank()) return@withContext ConnectionState.Unknown

            try {
                val response = request(
                    baseUrl = baseUrl,
                    path = "/api/devices/${Uri.encode(deviceId)}/status",
                    method = "GET",
                    body = null
                )
                when (response.code) {
                    200 -> {
                        val connected = JSONObject(response.body).optBoolean("connected")
                        if (connected) ConnectionState.Online else ConnectionState.Offline
                    }
                    404 -> ConnectionState.Unknown
                    else -> ConnectionState.Failed("Bridge HTTP ${response.code}")
                }
            } catch (_: Exception) {
                ConnectionState.Unreachable
            }
        }

    suspend fun sendCommand(
        baseUrl: String,
        deviceId: String,
        command: String,
        name: String? = null,
        adjective: String? = null
    ): CommandResult = withContext(Dispatchers.IO) {
        if (deviceId.isBlank()) return@withContext CommandResult.Unknown

        try {
            val payload = JSONObject().apply {
                put("command", command)
                if (!name.isNullOrBlank()) put("name", name)
                if (!adjective.isNullOrBlank()) put("adjective", adjective)
            }.toString()

            val response = request(
                baseUrl = baseUrl,
                path = "/api/devices/${Uri.encode(deviceId)}/commands",
                method = "POST",
                body = payload
            )

            when (response.code) {
                200, 202 -> CommandResult.Success("Sent")
                409 -> CommandResult.Offline
                404 -> CommandResult.Unknown
                else -> CommandResult.Failure("Bridge HTTP ${response.code}")
            }
        } catch (_: Exception) {
            CommandResult.Unreachable
        }
    }

    private fun request(
        baseUrl: String,
        path: String,
        method: String,
        body: String?
    ): Response {
        val base = baseUrl.trim().trimEnd('/').ifBlank { PresetStore.DEFAULT_URL }
        val connection = URL(base + path).openConnection() as HttpURLConnection
        try {
            connection.requestMethod = method
            connection.connectTimeout = 5000
            connection.readTimeout = 5000
            connection.setRequestProperty("Accept", "application/json")

            if (body != null) {
                connection.doOutput = true
                connection.setRequestProperty("Content-Type", "application/json; charset=utf-8")
                connection.outputStream.use { output ->
                    output.write(body.toByteArray(Charsets.UTF_8))
                }
            }

            val code = connection.responseCode
            val stream = if (code in 200..399) {
                connection.inputStream
            } else {
                connection.errorStream
            }
            val text = stream?.use { input ->
                BufferedReader(InputStreamReader(input, Charsets.UTF_8)).readText()
            }.orEmpty()

            return Response(code, text)
        } finally {
            connection.disconnect()
        }
    }

    private data class Response(val code: Int, val body: String)
}
