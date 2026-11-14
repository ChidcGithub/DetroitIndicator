(() => {
  const idInput = document.getElementById("deviceId");
  const connectButton = document.getElementById("connectButton");
  const statusPill = document.getElementById("statusPill");
  const statusText = document.getElementById("statusText");
  const bridgeState = document.getElementById("bridgeState");
  const pairHint = document.getElementById("pairHint");
  const toast = document.getElementById("toast");
  const buttons = [...document.querySelectorAll("[data-command]")];

  let pollTimer = null;
  let toastTimer = null;
  let connected = false;
  let lastId = "";

  const setStatus = (kind, label, bridgeLabel) => {
    statusPill.classList.remove("status-idle", "status-online", "status-offline", "status-waiting");
    statusPill.classList.add(`status-${kind}`);
    statusText.textContent = label;
    bridgeState.textContent = bridgeLabel;
    connected = kind === "online";
    buttons.forEach(button => { button.disabled = !connected; });
  };

  const showToast = (message, kind = "") => {
    toast.textContent = message;
    toast.className = `toast ${kind}`;
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => {
      toast.textContent = "";
      toast.className = "toast";
    }, 4200);
  };

  const normalizeId = () => idInput.value.trim().toUpperCase();

  const readError = async response => {
    try {
      const body = await response.json();
      return body.error || `HTTP ${response.status}`;
    } catch {
      return `HTTP ${response.status}`;
    }
  };

  const checkStatus = async (announce = false) => {
    const id = normalizeId();
    if (!id) {
      setStatus("idle", "Enter an ID", "waiting");
      return false;
    }

    try {
      const response = await fetch(`/api/devices/${encodeURIComponent(id)}/status`, {
        headers: { "Accept": "application/json" }
      });

      if (response.status === 404) {
        setStatus("offline", "ID not found", "no device");
        if (announce) showToast("No desktop has registered that ID.", "error");
        return false;
      }

      if (!response.ok) {
        setStatus("waiting", "Bridge error", "error");
        if (announce) showToast(await readError(response), "error");
        return false;
      }

      const status = await response.json();
      if (status.connected) {
        setStatus("online", "Desktop online", "online");
        pairHint.textContent = `Paired with ${status.machineName || "Windows PC"}.`;
      } else {
        setStatus("offline", "Desktop offline", "offline");
        pairHint.textContent = "The ID is registered, but the desktop is not connected.";
      }
      return status.connected;
    } catch {
      setStatus("waiting", "Bridge unreachable", "offline");
      if (announce) showToast("Could not reach the bridge website.", "error");
      return false;
    }
  };

  const startPolling = () => {
    clearInterval(pollTimer);
    pollTimer = setInterval(() => {
      if (!document.hidden) checkStatus(false);
    }, 4000);
  };

  const connect = async () => {
    const id = normalizeId();
    if (!id) {
      showToast("Enter the ID shown in the desktop app.", "error");
      idInput.focus();
      return;
    }

    idInput.value = id;
    localStorage.setItem("affectionBridgeDeviceId", id);
    lastId = id;
    setStatus("waiting", "Checking…", "checking");
    const ok = await checkStatus(true);
    if (ok) showToast("Connected. Pick a state below.", "good");
    startPolling();
  };

  const sendCommand = async command => {
    const id = normalizeId();
    if (!id) {
      showToast("Enter a device ID first.", "error");
      return;
    }

    if (!connected) {
      showToast("The desktop is offline.", "error");
      checkStatus(false);
      return;
    }

    try {
      const response = await fetch(`/api/devices/${encodeURIComponent(id)}/commands`, {
        method: "POST",
        headers: { "Content-Type": "application/json", "Accept": "application/json" },
        body: JSON.stringify({ command })
      });

      if (response.status === 202 || response.ok) {
        showToast(`Sent ${command}`, "good");
        return;
      }

      const message = await readError(response);
      showToast(message, "error");
      checkStatus(false);
    } catch {
      showToast("Could not reach the bridge website.", "error");
      setStatus("waiting", "Bridge unreachable", "offline");
    }
  };

  connectButton.addEventListener("click", connect);
  idInput.addEventListener("keydown", event => {
    if (event.key === "Enter") connect();
  });
  idInput.addEventListener("input", () => {
    const id = normalizeId();
    if (id !== lastId) {
      connected = false;
      buttons.forEach(button => { button.disabled = true; });
      setStatus("idle", "Not connected", "waiting");
    }
  });

  buttons.forEach(button => {
    button.addEventListener("click", () => sendCommand(button.dataset.command));
  });

  const saved = localStorage.getItem("affectionBridgeDeviceId");
  if (saved) {
    idInput.value = saved;
    connect();
  } else {
    setStatus("idle", "Not connected", "waiting");
  }
})();
