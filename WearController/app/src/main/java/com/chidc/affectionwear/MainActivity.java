package com.chidc.affectionwear;

import android.app.Activity;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.net.Uri;
import android.os.Bundle;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Spinner;
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
    private static final String KEY_PRESET_NAME = "preset_name";
    private static final String KEY_PRESET_ADJECTIVE = "preset_adjective";
    private static final String KEY_PRESET_COMMAND = "preset_command";
    private static final String KEY_PRESET_LABEL = "preset_label";
    private static final String KEY_PRESET_STATE = "preset_state";
    private static final String DEFAULT_URL = "http://10.0.2.2:5210";

    private static final String[] STATE_LABELS = {
            "Small +", "Small \u2212", "Big +", "Big \u2212",
            "Unlock", "Lock", "Name only"
    };
    private static final String[] STATE_COMMANDS = {
            "inc", "dec", "inc+", "dec+", "unlock", "lock", "neutral"
    };

    private final ExecutorService executor = Executors.newSingleThreadExecutor();

    private EditText deviceId;
    private EditText bridgeUrl;
    private EditText presetName;
    private EditText presetAdjective;
    private Spinner presetState;
    private Button customButton;
    private TextView statusText;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        deviceId = findViewById(R.id.device_id);
        bridgeUrl = findViewById(R.id.bridge_url);
        presetName = findViewById(R.id.preset_name);
        presetAdjective = findViewById(R.id.preset_adjective);
        presetState = findViewById(R.id.preset_state);
        customButton = findViewById(R.id.btn_custom);
        statusText = findViewById(R.id.status_text);

        deviceId.setSelectAllOnFocus(true);
        bridgeUrl.setSelectAllOnFocus(true);
        presetName.setSelectAllOnFocus(true);
        presetAdjective.setSelectAllOnFocus(true);

        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        deviceId.setText(prefs.getString(KEY_ID, ""));
        bridgeUrl.setText(prefs.getString(KEY_URL, DEFAULT_URL));
        presetName.setText(prefs.getString(KEY_PRESET_NAME, ""));
        presetAdjective.setText(prefs.getString(KEY_PRESET_ADJECTIVE, ""));
        setupStateSpinner(prefs.getInt(KEY_PRESET_STATE, 0));
        updateCustomButton();

        findViewById(R.id.btn_save).setOnClickListener(v -> {
            savePrefs();
            checkStatus();
        });

        findViewById(R.id.btn_save_preset).setOnClickListener(v -> savePreset());
        customButton.setOnClickListener(v -> {
            savePrefs();
            sendPreset();
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

    private void setupStateSpinner(int selected) {
        ArrayAdapter<String> adapter = new ArrayAdapter<String>(
                this, android.R.layout.simple_spinner_item, STATE_LABELS) {
            @Override
            public View getView(int position, View convertView, ViewGroup parent) {
                TextView view = (TextView) super.getView(position, convertView, parent);
                view.setTextColor(Color.WHITE);
                view.setTextSize(12);
                view.setPadding(0, 0, 0, 0);
                return view;
            }

            @Override
            public View getDropDownView(int position, View convertView, ViewGroup parent) {
                TextView view = (TextView) super.getDropDownView(position, convertView, parent);
                view.setTextColor(Color.WHITE);
                view.setTextSize(13);
                return view;
            }
        };
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        presetState.setAdapter(adapter);
        if (selected < 0 || selected >= STATE_LABELS.length) {
            selected = 0;
        }
        presetState.setSelection(selected);
    }

    private void savePreset() {
        String name = presetName.getText().toString().trim().toUpperCase(Locale.ROOT);
        String adjective = presetAdjective.getText().toString().trim().toUpperCase(Locale.ROOT);
        if (name.isEmpty() || adjective.isEmpty()) {
            setStatus("Enter a name and adjective");
            return;
        }

        int index = presetState.getSelectedItemPosition();
        if (index < 0 || index >= STATE_COMMANDS.length) {
            index = 0;
        }

        presetName.setText(name);
        presetAdjective.setText(adjective);
        getSharedPreferences(PREFS, MODE_PRIVATE)
                .edit()
                .putString(KEY_PRESET_NAME, name)
                .putString(KEY_PRESET_ADJECTIVE, adjective)
                .putString(KEY_PRESET_COMMAND, STATE_COMMANDS[index])
                .putString(KEY_PRESET_LABEL, STATE_LABELS[index])
                .putInt(KEY_PRESET_STATE, index)
                .apply();
        updateCustomButton();
        setStatus("Saved: " + name + " / " + adjective);
    }

    private void updateCustomButton() {
        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        String name = prefs.getString(KEY_PRESET_NAME, "");
        String adjective = prefs.getString(KEY_PRESET_ADJECTIVE, "");
        String label = prefs.getString(KEY_PRESET_LABEL, "");
        if (name.isEmpty() || adjective.isEmpty() || label.isEmpty()) {
            customButton.setVisibility(View.GONE);
            return;
        }

        customButton.setText(name + " / " + adjective + "\n" + label);
        customButton.setVisibility(View.VISIBLE);
    }

    private void sendPreset() {
        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        String name = prefs.getString(KEY_PRESET_NAME, "");
        String adjective = prefs.getString(KEY_PRESET_ADJECTIVE, "");
        String command = prefs.getString(KEY_PRESET_COMMAND, "");
        String label = prefs.getString(KEY_PRESET_LABEL, "");
        if (name.isEmpty() || adjective.isEmpty() || command.isEmpty()) {
            setStatus("Save a custom button first");
            return;
        }

        sendCommand(command, name + " / " + adjective + " \u00b7 " + label, name, adjective);
    }

    private void sendCommand(String command, String label) {
        sendCommand(command, label, null, null);
    }

    private void sendCommand(String command, String label, String name, String adjective) {
        String id = currentId();
        if (id.isEmpty()) {
            setStatus("Enter the desktop ID first");
            return;
        }

        StringBuilder body = new StringBuilder("{\"command\":\"")
                .append(command)
                .append("\"");
        if (name != null && !name.isEmpty()) {
            body.append(",\"name\":\"").append(jsonEscape(name)).append("\"");
        }
        if (adjective != null && !adjective.isEmpty()) {
            body.append(",\"adjective\":\"").append(jsonEscape(adjective)).append("\"");
        }
        body.append("}");

        setStatus("Sending\u2026");
        String payload = body.toString();
        executor.execute(() -> {
            try {
                Result result = request("POST",
                        "/api/devices/" + Uri.encode(id) + "/commands", payload);
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

    private static String jsonEscape(String value) {
        StringBuilder escaped = new StringBuilder(value.length() + 8);
        for (char c : value.toCharArray()) {
            if (c == '\\') {
                escaped.append("\\\\");
            } else if (c == '"') {
                escaped.append("\\\"");
            } else if (c >= 0x20) {
                escaped.append(c);
            }
        }
        return escaped.toString();
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
