package com.chidc.affectionwear.ui

import androidx.activity.compose.BackHandler
import androidx.compose.animation.AnimatedContent
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyListScope
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.wear.compose.material3.Card
import androidx.wear.compose.material3.CompactButton
import androidx.wear.compose.material3.MaterialTheme
import androidx.wear.compose.material3.SwipeToReveal
import androidx.wear.compose.material3.SwipeToRevealDefaults
import androidx.wear.compose.material3.Text
import com.chidc.affectionwear.data.BridgeApi
import com.chidc.affectionwear.data.CommandResult
import com.chidc.affectionwear.data.ConnectionState
import com.chidc.affectionwear.data.Preset
import com.chidc.affectionwear.data.PresetStore
import com.chidc.affectionwear.data.STATE_OPTIONS
import kotlinx.coroutines.launch
import java.util.Locale
import java.util.UUID

private sealed interface Screen {
    data object Main : Screen
    data object Gauge : Screen
    data object Small : Screen
    data object Big : Screen
    data object Path : Screen
    data object Visibility : Screen
    data object Presets : Screen
    data object NewPreset : Screen
    data object StatePicker : Screen
    data object Pairing : Screen
}

private data class GaugeAction(
    val label: String,
    val description: String,
    val command: String,
    val glyph: String
)

@Composable
fun AffectionApp() {
    val context = androidx.compose.ui.platform.LocalContext.current
    val store = remember { PresetStore(context) }
    var bridgeUrl by rememberSaveable { mutableStateOf(store.bridgeUrl) }
    var deviceId by rememberSaveable { mutableStateOf(store.deviceId) }
    var presets by remember { mutableStateOf(store.loadPresets()) }
    var connection by remember {
        mutableStateOf<ConnectionState>(
            if (deviceId.isBlank()) ConnectionState.Unknown else ConnectionState.Checking
        )
    }
    var statusMessage by remember { mutableStateOf("") }
    val stack = remember { mutableStateListOf<Screen>(Screen.Main) }
    val scope = rememberCoroutineScope()

    var draftName by rememberSaveable { mutableStateOf("") }
    var draftAdjective by rememberSaveable { mutableStateOf("") }
    var draftStateIndex by rememberSaveable { mutableIntStateOf(0) }

    fun push(screen: Screen) {
        stack.add(screen)
    }

    fun pop() {
        if (stack.size > 1) {
            stack.removeAt(stack.lastIndex)
        }
    }

    LaunchedEffect(deviceId, bridgeUrl) {
        if (deviceId.isBlank()) {
            connection = ConnectionState.Unknown
            return@LaunchedEffect
        }

        connection = ConnectionState.Checking
        connection = BridgeApi.checkStatus(bridgeUrl, deviceId)
    }

    fun send(
        command: String,
        label: String,
        name: String? = null,
        adjective: String? = null
    ) {
        if (deviceId.isBlank()) {
            statusMessage = "Pair the desktop first"
            connection = ConnectionState.Unknown
            return
        }

        statusMessage = "Sending $label\u2026"
        scope.launch {
            when (val result = BridgeApi.sendCommand(
                bridgeUrl, deviceId, command, name, adjective
            )) {
                is CommandResult.Success -> {
                    connection = ConnectionState.Online
                    statusMessage = "Sent: $label"
                }
                is CommandResult.Failure -> {
                    connection = ConnectionState.Failed(result.message)
                    statusMessage = result.message
                }
                CommandResult.Offline -> {
                    connection = ConnectionState.Offline
                    statusMessage = "Desktop offline"
                }
                CommandResult.Unknown -> {
                    connection = ConnectionState.Unknown
                    statusMessage = "Unknown device ID"
                }
                CommandResult.Unreachable -> {
                    connection = ConnectionState.Unreachable
                    statusMessage = "Bridge unreachable"
                }
            }
        }
    }

    fun savePreset() {
        val name = draftName.trim().uppercase(Locale.ROOT)
        val adjective = draftAdjective.trim().uppercase(Locale.ROOT)
        if (name.isEmpty() || adjective.isEmpty()) {
            statusMessage = "Enter a name and adjective"
            return
        }

        val option = STATE_OPTIONS[draftStateIndex]
        val preset = Preset(
            id = UUID.randomUUID().toString(),
            name = name,
            adjective = adjective,
            command = option.command,
            label = option.label
        )
        presets = listOf(preset) + presets
        store.savePresets(presets)
        statusMessage = "Saved $name / $adjective"
        pop()
    }

    fun deletePreset(preset: Preset) {
        presets = presets.filterNot { it.id == preset.id }
        store.savePresets(presets)
        statusMessage = "Removed ${preset.name} / ${preset.adjective}"
    }

    fun savePairing(url: String, id: String) {
        bridgeUrl = url.trim().trimEnd('/')
        deviceId = id.trim().uppercase(Locale.ROOT)
        store.saveConnection(bridgeUrl, deviceId)
        statusMessage = "Pairing saved"
        connection = ConnectionState.Checking
    }

    BackHandler(enabled = stack.size > 1) { pop() }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(MaterialTheme.colorScheme.background)
    ) {
        AnimatedContent(targetState = stack.last(), label = "wear-screen") { screen ->
            when (screen) {
                Screen.Main -> MainScreen(
                    presets = presets,
                    deviceId = deviceId,
                    connection = connection,
                    statusMessage = statusMessage,
                    onGauge = { push(Screen.Gauge) },
                    onPresets = { push(Screen.Presets) },
                    onPairing = { push(Screen.Pairing) }
                )

                Screen.Gauge -> GaugeScreen(
                    connection = connection,
                    statusMessage = statusMessage,
                    onBack = ::pop,
                    onSmall = { push(Screen.Small) },
                    onBig = { push(Screen.Big) },
                    onPath = { push(Screen.Path) },
                    onVisibility = { push(Screen.Visibility) },
                    onNameOnly = { send("neutral", "name only") }
                )

                Screen.Small -> ActionScreen(
                    title = "Small change",
                    subtitle = "Small + or \u2212",
                    actions = listOf(
                        GaugeAction("Small increase", "Send inc", "inc", "\u2191"),
                        GaugeAction("Small decrease", "Send dec", "dec", "\u2193")
                    ),
                    connection = connection,
                    statusMessage = statusMessage,
                    onBack = ::pop,
                    onAction = { action ->
                        send(action.command, action.label.lowercase(Locale.ROOT))
                    }
                )

                Screen.Big -> ActionScreen(
                    title = "Big change",
                    subtitle = "Big + or \u2212",
                    actions = listOf(
                        GaugeAction("Big increase", "Send inc+", "inc+", "\u21c8"),
                        GaugeAction("Big decrease", "Send dec+", "dec+", "\u21ca")
                    ),
                    connection = connection,
                    statusMessage = statusMessage,
                    onBack = ::pop,
                    onAction = { action ->
                        send(action.command, action.label.lowercase(Locale.ROOT))
                    }
                )

                Screen.Path -> ActionScreen(
                    title = "Path",
                    subtitle = "Unlock or lock",
                    actions = listOf(
                        GaugeAction("Path unlocked", "Send unlock", "unlock", "\u25c7"),
                        GaugeAction("Path locked", "Send lock", "lock", "\u25c6")
                    ),
                    connection = connection,
                    statusMessage = statusMessage,
                    onBack = ::pop,
                    onAction = { action ->
                        send(action.command, action.label.lowercase(Locale.ROOT))
                    }
                )

                Screen.Visibility -> ActionScreen(
                    title = "Visibility",
                    subtitle = "Show or hide",
                    actions = listOf(
                        GaugeAction("Show indicator", "Send show", "show", "\u25c9"),
                        GaugeAction("Hide indicator", "Send hide", "hide", "\u25cc")
                    ),
                    connection = connection,
                    statusMessage = statusMessage,
                    onBack = ::pop,
                    onAction = { action ->
                        send(action.command, action.label.lowercase(Locale.ROOT))
                    }
                )

                Screen.Presets -> PresetsScreen(
                    presets = presets,
                    connection = connection,
                    statusMessage = statusMessage,
                    onBack = ::pop,
                    onNew = {
                        draftName = ""
                        draftAdjective = ""
                        draftStateIndex = 0
                        push(Screen.NewPreset)
                    },
                    onSend = { preset ->
                        send(
                            preset.command,
                            "${preset.name} / ${preset.adjective} \u00b7 ${preset.label}",
                            preset.name,
                            preset.adjective
                        )
                    },
                    onDelete = ::deletePreset
                )

                Screen.NewPreset -> PresetEditorScreen(
                    name = draftName,
                    adjective = draftAdjective,
                    stateIndex = draftStateIndex,
                    connection = connection,
                    statusMessage = statusMessage,
                    onNameChange = { draftName = it.take(32) },
                    onAdjectiveChange = { draftAdjective = it.take(32) },
                    onPickState = { push(Screen.StatePicker) },
                    onSave = ::savePreset,
                    onBack = ::pop
                )

                Screen.StatePicker -> StatePickerScreen(
                    selectedIndex = draftStateIndex,
                    onSelect = { index ->
                        draftStateIndex = index
                        pop()
                    },
                    onBack = ::pop
                )

                Screen.Pairing -> PairingScreen(
                    bridgeUrl = bridgeUrl,
                    deviceId = deviceId,
                    connection = connection,
                    statusMessage = statusMessage,
                    onSave = ::savePairing,
                    onBack = ::pop
                )
            }
        }
    }
}

@Composable
private fun MainScreen(
    presets: List<Preset>,
    deviceId: String,
    connection: ConnectionState,
    statusMessage: String,
    onGauge: () -> Unit,
    onPresets: () -> Unit,
    onPairing: () -> Unit
) {
    ScreenContainer(
        title = "Affection",
        subtitle = "WEAR BRIDGE",
        connection = connection,
        statusMessage = statusMessage
    ) {
        item {
            MenuCard(
                title = "Gauge states",
                subtitle = "Small, big, path",
                onClick = onGauge
            )
        }
        item {
            MenuCard(
                title = "Custom buttons",
                subtitle = if (presets.isEmpty()) "No saved buttons" else "${presets.size} saved",
                onClick = onPresets
            )
        }
        item {
            MenuCard(
                title = "Pairing",
                subtitle = if (deviceId.isBlank()) "Not paired" else deviceId,
                onClick = onPairing
            )
        }
        if (statusMessage.isNotBlank()) {
            item {
                Text(
                    text = statusMessage,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                    modifier = Modifier.padding(horizontal = 4.dp)
                )
            }
        }
    }
}

@Composable
private fun GaugeScreen(
    connection: ConnectionState,
    statusMessage: String,
    onBack: () -> Unit,
    onSmall: () -> Unit,
    onBig: () -> Unit,
    onPath: () -> Unit,
    onVisibility: () -> Unit,
    onNameOnly: () -> Unit
) {
    ScreenContainer(
        title = "Gauge states",
        subtitle = "Pick a category",
        onBack = onBack,
        connection = connection,
        statusMessage = statusMessage
    ) {
        item { MenuCard("Small change", "Small + or \u2212", onSmall) }
        item { MenuCard("Big change", "Big + or \u2212", onBig) }
        item { MenuCard("Path", "Unlock or lock", onPath) }
        item { MenuCard("Visibility", "Show or hide", onVisibility) }
        item { MenuCard("Name only", "Name + adjective", onNameOnly) }
    }
}

@Composable
private fun ActionScreen(
    title: String,
    subtitle: String,
    actions: List<GaugeAction>,
    connection: ConnectionState,
    statusMessage: String,
    onBack: () -> Unit,
    onAction: (GaugeAction) -> Unit
) {
    ScreenContainer(
        title = title,
        subtitle = subtitle,
        onBack = onBack,
        connection = connection,
        statusMessage = statusMessage
    ) {
        actions.forEach { action ->
            item(key = action.command) {
                MenuCard(
                    title = action.label,
                    subtitle = action.description,
                    onClick = { onAction(action) }
                )
            }
        }
    }
}

@Composable
private fun PresetsScreen(
    presets: List<Preset>,
    connection: ConnectionState,
    statusMessage: String,
    onBack: () -> Unit,
    onNew: () -> Unit,
    onSend: (Preset) -> Unit,
    onDelete: (Preset) -> Unit
) {
    ScreenContainer(
        title = "Custom buttons",
        subtitle = "${presets.size} saved",
        onBack = onBack,
        connection = connection,
        statusMessage = statusMessage
    ) {
        item {
            CompactButton(
                onClick = onNew,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("New button")
            }
        }

        if (presets.isEmpty()) {
            item {
                Text(
                    text = "No saved buttons. Create one with a name, adjective and state.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(horizontal = 4.dp)
                )
            }
        } else {
            items(presets, key = { it.id }) { preset ->
                PresetRow(
                    preset = preset,
                    onSend = { onSend(preset) },
                    onDelete = { onDelete(preset) }
                )
            }
            item {
                Text(
                    text = "Swipe left on a saved button to delete it.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(horizontal = 4.dp)
                )
            }
        }
    }
}

@Composable
private fun PresetRow(
    preset: Preset,
    onSend: () -> Unit,
    onDelete: () -> Unit
) {
    SwipeToReveal(
        primaryAction = {
            PrimaryActionButton(
                onClick = onDelete,
                icon = { Text("\u00d7", style = MaterialTheme.typography.titleLarge) },
                text = { Text("Delete") },
                modifier = Modifier.height(SwipeToRevealDefaults.LargeActionButtonHeight)
            )
        },
        onSwipePrimaryAction = onDelete,
        modifier = Modifier.fillMaxWidth()
    ) {
        Card(
            onClick = onSend,
            modifier = Modifier.fillMaxWidth()
        ) {
            Column(modifier = Modifier.fillMaxWidth()) {
                Text(
                    text = "${preset.name} / ${preset.adjective}",
                    style = MaterialTheme.typography.titleSmall,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
                Text(
                    text = preset.label,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.primary,
                    maxLines = 1
                )
            }
        }
    }
}

@Composable
private fun PresetEditorScreen(
    name: String,
    adjective: String,
    stateIndex: Int,
    connection: ConnectionState,
    statusMessage: String,
    onNameChange: (String) -> Unit,
    onAdjectiveChange: (String) -> Unit,
    onPickState: () -> Unit,
    onSave: () -> Unit,
    onBack: () -> Unit
) {
    ScreenContainer(
        title = "New button",
        subtitle = "Name + adjective",
        onBack = onBack,
        connection = connection,
        statusMessage = statusMessage
    ) {
        item {
            WatchField(
                label = "Name",
                value = name,
                placeholder = "HANK",
                capitalization = KeyboardCapitalization.Characters,
                onValueChange = onNameChange
            )
        }
        item {
            WatchField(
                label = "Adjective",
                value = adjective,
                placeholder = "HOSTILE",
                capitalization = KeyboardCapitalization.Characters,
                onValueChange = onAdjectiveChange
            )
        }
        item {
            MenuCard(
                title = "State",
                subtitle = STATE_OPTIONS[stateIndex.coerceIn(0, STATE_OPTIONS.lastIndex)].label,
                onClick = onPickState
            )
        }
        item {
            CompactButton(
                onClick = onSave,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Save button")
            }
        }
        item {
            Text(
                text = "Saved buttons appear on the Custom buttons screen.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(horizontal = 4.dp)
            )
        }
    }
}

@Composable
private fun StatePickerScreen(
    selectedIndex: Int,
    onSelect: (Int) -> Unit,
    onBack: () -> Unit
) {
    ScreenContainer(
        title = "State",
        subtitle = "Button action",
        onBack = onBack
    ) {
        STATE_OPTIONS.forEachIndexed { index, option ->
            item(key = option.command) {
                MenuCard(
                    title = if (index == selectedIndex) "\u2713 ${option.label}" else option.label,
                    subtitle = option.command,
                    onClick = { onSelect(index) }
                )
            }
        }
    }
}

@Composable
private fun PairingScreen(
    bridgeUrl: String,
    deviceId: String,
    connection: ConnectionState,
    statusMessage: String,
    onSave: (String, String) -> Unit,
    onBack: () -> Unit
) {
    var url by rememberSaveable { mutableStateOf(bridgeUrl) }
    var id by rememberSaveable { mutableStateOf(deviceId) }

    ScreenContainer(
        title = "Pairing",
        subtitle = "Bridge + device ID",
        onBack = onBack,
        connection = connection,
        statusMessage = statusMessage
    ) {
        item {
            WatchField(
                label = "Bridge URL",
                value = url,
                placeholder = PresetStore.DEFAULT_URL,
                keyboardType = KeyboardType.Uri,
                onValueChange = { url = it }
            )
        }
        item {
            WatchField(
                label = "Device ID",
                value = id,
                placeholder = "ABCDEF1234",
                capitalization = KeyboardCapitalization.Characters,
                onValueChange = { id = it.take(10) }
            )
        }
        item {
            CompactButton(
                onClick = { onSave(url, id) },
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Save & check")
            }
        }
        item {
            Text(
                text = "Find the ID in the desktop app under Settings \u2192 Bridge.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(horizontal = 4.dp)
            )
        }
    }
}

@Composable
private fun ScreenContainer(
    title: String,
    subtitle: String? = null,
    onBack: (() -> Unit)? = null,
    connection: ConnectionState? = null,
    statusMessage: String = "",
    content: LazyListScope.() -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 24.dp)
    ) {
        Spacer(Modifier.height(36.dp))
        if (onBack == null) {
            Column(
                modifier = Modifier.fillMaxWidth(),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(
                    text = title,
                    style = MaterialTheme.typography.titleMedium,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
                if (subtitle != null) {
                    Text(
                        text = subtitle,
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                }
            }
        } else {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Box(
                    modifier = Modifier
                        .size(32.dp)
                        .clip(CircleShape)
                        .clickable(onClick = onBack),
                    contentAlignment = Alignment.Center
                ) {
                    Text("\u2039", fontSize = 22.sp)
                }

                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = title,
                        style = MaterialTheme.typography.titleSmall,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                    if (subtitle != null) {
                        Text(
                            text = subtitle,
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            maxLines = 1,
                            overflow = TextOverflow.Ellipsis
                        )
                    }
                }
            }
        }

        if (connection != null) {
            StatusLine(connection = connection, message = statusMessage)
        } else if (statusMessage.isNotBlank()) {
            Text(
                text = statusMessage,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
                modifier = Modifier.padding(horizontal = 4.dp)
            )
        }

        LazyColumn(
            modifier = Modifier.weight(1f),
            contentPadding = PaddingValues(top = 4.dp, bottom = 18.dp),
            verticalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            content()
        }
    }
}

@Composable
private fun StatusLine(
    connection: ConnectionState,
    message: String
) {
    val color = connectionColor(connection)
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 4.dp, vertical = 4.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            modifier = Modifier
                .size(7.dp)
                .clip(CircleShape)
                .background(color)
        )
        Spacer(Modifier.width(6.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = connectionLabel(connection),
                style = MaterialTheme.typography.labelSmall,
                color = color,
                maxLines = 1
            )
            if (message.isNotBlank()) {
                Text(
                    text = message,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }
    }
}

@Composable
private fun MenuCard(
    title: String,
    subtitle: String,
    onClick: () -> Unit
) {
    Card(
        onClick = onClick,
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 2.dp)
        ) {
            Text(
                text = title,
                style = MaterialTheme.typography.titleSmall,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
            Text(
                text = subtitle,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
        }
    }
}

@Composable
private fun WatchField(
    label: String,
    value: String,
    placeholder: String,
    onValueChange: (String) -> Unit,
    capitalization: KeyboardCapitalization = KeyboardCapitalization.None,
    keyboardType: KeyboardType = KeyboardType.Text
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .clip(MaterialTheme.shapes.large)
            .background(MaterialTheme.colorScheme.surfaceContainerHigh)
            .padding(horizontal = 12.dp, vertical = 9.dp)
    ) {
        Text(
            text = label,
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Box(modifier = Modifier.fillMaxWidth()) {
            if (value.isEmpty()) {
                Text(
                    text = placeholder,
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.55f),
                    maxLines = 1
                )
            }
            BasicTextField(
                value = value,
                onValueChange = onValueChange,
                modifier = Modifier.fillMaxWidth(),
                textStyle = MaterialTheme.typography.bodyLarge.copy(
                    color = MaterialTheme.colorScheme.onSurface
                ),
                singleLine = true,
                cursorBrush = SolidColor(MaterialTheme.colorScheme.primary),
                keyboardOptions = KeyboardOptions(
                    capitalization = capitalization,
                    keyboardType = keyboardType
                )
            )
        }
    }
}

@Composable
private fun connectionColor(connection: ConnectionState): Color {
    return when (connection) {
        ConnectionState.Online -> MaterialTheme.colorScheme.primary
        ConnectionState.Checking -> MaterialTheme.colorScheme.tertiary
        ConnectionState.Offline,
        ConnectionState.Unreachable,
        is ConnectionState.Failed -> MaterialTheme.colorScheme.error
        ConnectionState.Unknown -> MaterialTheme.colorScheme.onSurfaceVariant
    }
}

private fun connectionLabel(connection: ConnectionState): String {
    return when (connection) {
        ConnectionState.Online -> "DESKTOP ONLINE"
        ConnectionState.Checking -> "CHECKING"
        ConnectionState.Offline -> "DESKTOP OFFLINE"
        ConnectionState.Unknown -> "NOT PAIRED"
        ConnectionState.Unreachable -> "BRIDGE UNREACHABLE"
        is ConnectionState.Failed -> connection.message.uppercase(Locale.ROOT)
    }
}
