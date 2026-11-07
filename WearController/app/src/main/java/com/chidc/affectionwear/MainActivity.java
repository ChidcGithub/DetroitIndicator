package com.chidc.affectionwear;

import android.app.Activity;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Bundle;
import android.widget.EditText;
import android.widget.TextView;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.Locale;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class MainActivity extends Activity {
    private static final String PREFS = "affection_wear";
    private static final String KEY_ID = "device_id";
    private static final String KEY_URL = "bridge_url";
    private static final String DEFAULT_URL = "http://10.0.2.2:5210";

    private final ExecutorService executor = Executors.newSingleThreadExecutor();

    private EditText deviceId;
    private EditText bridgeUrl;
    private TextView statusText;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        deviceId = findViewById(R.id.device_id);
        bridgeUrl = findViewById(R.id.bridge_url);
        statusText = findViewById(R.id.status_text);

        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        deviceId.setText(prefs.getString(KEY_ID, ""));
        bridgeUrl.setText(prefs.getString(KEY_URL, DEFAULT_URL));

        findViewById(R.id.btn_save).setOnClickListener(v -> {
            savePrefs();
            checkStatus();
        });

        bind(R.id.btn_inc, "inc", "small increase");
        bind(R.id.btn_dec, "dec", "small decrease");
        bind(R.id.btn_inc_big, "inc+", "big increase");
        bind(R.id.btn_dec_big, "dec+", "big decrease");
        bind(R.id.btn_unlock, "unlock", "path unlocked");
        bind(R.id.btn_lock, "lock", "path locked");
        bind(R.id.btn_neutral, "neutral", "name only");
        bind(R.id.btn_show, "show", "show");
        bind(R.id.btn_hide, "hide", "hide");

        if (deviceId.getText().toString().trim().isEmpty()) {
            setStatus("Enter the desktop ID from Settings \u2192 Bridge");
        }
    }

    @Override
    protected void onResume() {
        super.onResume();
        if (deviceId != null && !deviceId.getText().toString().trim().isEmpty()) {
            checkStatus();
        }
    }

    private void bind(int viewId, String command, String label) {
        findViewById(viewId).setOnClickListener(v -> {
            savePrefs();
            sendCommand(command, label);
        });
    }

    private void savePrefs() {
        String id = currentId();
        String url = currentBaseUrl();
        deviceId.setText(id);
        bridgeUrl.setText(url);
        getSharedPreferences(PREFS, MODE_PRIVATE)
                .edit()
                .putString(KEY_ID, id)
                .putString(KEY_URL, url)
                .apply();
    }

    private String currentId() {
        return deviceId.getText().toString().trim().toUpperCase(Locale.ROOT);
    }

    private String currentBaseUrl() {
        String value = bridgeUrl.getText().toString().trim();
        if (value.isEmpty()) {
            value = DEFAULT_URL;
        }
        while (value.endsWith("/")) {
            value = value.substring(0, value.length() - 1);
        }
        return value;
    }

    private void checkStatus() {
        String id = currentId();
        if (id.isEmpty()) {
            setStatus("Enter the desktop ID first");
            return;
        }

        setStatus("Checking\u2026");
        executor.execute(() -> {
            try {
                Result result = request("GET",
                        "/api/devices/" + Uri.encode(id) + "/status", null);
                if (result.code == 200) {
                    boolean online = result.body.replace(" ", "").contains("\"connected\":true");
                    setStatus(online ? "Desktop online" : "Desktop offline");
                } else if (result.code == 404) {
                    setStatus("Unknown device ID");
                } else {
                    setStatus("Bridge error " + result.code);
                }
            } catch (Exception e) {
                setStatus("Bridge unreachable");
            }
        });
    }

    private void sendCommand(String command, String label) {
        String id = currentId();
        if (id.isEmpty()) {
            setStatus("Enter the desktop ID first");
            return;
        }

        setStatus("Sending\u2026");
        executor.execute(() -> {
            try {
                Result result = request("POST",
                        "/api/devices/" + Uri.encode(id) + "/commands",
                        "{\"command\":\"" + command + "\"}");
                if (result.code == 200 || result.code == 202) {
                    setStatus("Sent: " + label);
                } else if (result.code == 409) {
                    setStatus("Desktop offline");
                } else if (result.code == 404) {
                    setStatus("Unknown device ID");
                } else {
                    setStatus("Failed: " + result.code);
                }
            } catch (Exception e) {
                setStatus("Bridge unreachable");
            }
        });
    }

    private Result request(String method, String path, String body) throws IOException {
        URL url = new URL(currentBaseUrl() + path);
        HttpURLConnection connection = (HttpURLConnection) url.openConnection();
        connection.setRequestMethod(method);
        connection.setConnectTimeout(5000);
        connection.setReadTimeout(5000);
        connection.setRequestProperty("Accept", "application/json");

        if (body != null) {
            connection.setDoOutput(true);
            connection.setRequestProperty("Content-Type", "application/json; charset=utf-8");
            try (OutputStream output = connection.getOutputStream()) {
                output.write(body.getBytes(StandardCharsets.UTF_8));
            }
        }

        int code = connection.getResponseCode();
        InputStream stream = null;
        try {
            stream = code >= 200 && code < 400
                    ? connection.getInputStream()
                    : connection.getErrorStream();
        } catch (IOException ignored) {
        }

        String text = readAll(stream);
        connection.disconnect();
        return new Result(code, text);
    }

    private static String readAll(InputStream stream) throws IOException {
        if (stream == null) {
            return "";
        }
        try (InputStream input = stream; ByteArrayOutputStream output = new ByteArrayOutputStream()) {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = input.read(buffer)) != -1) {
                output.write(buffer, 0, read);
            }
            return output.toString(StandardCharsets.UTF_8.name());
        }
    }

    private void setStatus(String text) {
        runOnUiThread(() -> statusText.setText(text));
    }

    @Override
    protected void onDestroy() {
        executor.shutdownNow();
        super.onDestroy();
    }

    private static final class Result {
        final int code;
        final String body;

        Result(int code, String body) {
            this.code = code;
            this.body = body;
        }
    }
}
