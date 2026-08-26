(() => {
    const root = document.getElementById("aiAssistant");
    if (!root) {
        return;
    }

    const toggleBtn = document.getElementById("aiAssistantToggle");
    const closeBtn = document.getElementById("aiAssistantClose");
    const panel = document.getElementById("aiAssistantPanel");
    const badge = document.getElementById("aiAssistantBadge");
    const messagesEl = document.getElementById("aiAssistantMessages");
    const form = document.getElementById("aiAssistantForm");
    const input = document.getElementById("aiAssistantInput");
    const micBtn = document.getElementById("aiAssistantMic");
    const scriptTag = document.currentScript;

    const greeting = scriptTag.getAttribute("data-greeting") || "";
    const errorText = scriptTag.getAttribute("data-error") || "Error.";
    const shouldAutoOpen = scriptTag.getAttribute("data-auto-open") === "true";
    const suggestions = [
        scriptTag.getAttribute("data-suggestion-1"),
        scriptTag.getAttribute("data-suggestion-2"),
        scriptTag.getAttribute("data-suggestion-3")
    ].filter(Boolean);

    const voiceCulture = scriptTag.getAttribute("data-voice-culture") || "en-US";
    const voiceUnsupportedText = scriptTag.getAttribute("data-voice-unsupported") || "";
    const voiceDeniedText = scriptTag.getAttribute("data-voice-denied") || "";
    const voiceNoSpeechText = scriptTag.getAttribute("data-voice-no-speech") || "";
    const voiceErrorText = scriptTag.getAttribute("data-voice-error") || "";

    const HISTORY_KEY = "ai-assistant-history";
    const REOPEN_KEY = "ai-assistant-reopen";

    function loadHistory() {
        try {
            const raw = sessionStorage.getItem(HISTORY_KEY);
            return raw ? JSON.parse(raw) : [];
        } catch {
            return [];
        }
    }

    function saveHistory(history) {
        try {
            sessionStorage.setItem(HISTORY_KEY, JSON.stringify(history));
        } catch {
            // ignore storage errors
        }
    }

    let history = loadHistory();
    let suggestionsEl = null;

    if (shouldAutoOpen) {
        history = [];
        saveHistory(history);
    }

    function renderMessage(role, text) {
        const row = document.createElement("div");
        row.className = "ai-assistant-msg ai-assistant-msg-" + role;

        const bubble = document.createElement("div");
        bubble.className = "ai-assistant-bubble";
        bubble.textContent = text;

        row.appendChild(bubble);
        messagesEl.appendChild(row);
        messagesEl.scrollTop = messagesEl.scrollHeight;

        return row;
    }

    function removeSuggestions() {
        if (suggestionsEl) {
            suggestionsEl.remove();
            suggestionsEl = null;
        }
    }

    function renderSuggestions() {
        if (suggestions.length === 0) {
            return;
        }

        suggestionsEl = document.createElement("div");
        suggestionsEl.className = "ai-assistant-suggestions";

        suggestions.forEach((suggestion) => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "ai-assistant-suggestion";
            button.textContent = suggestion;

            button.addEventListener("click", () => {
                input.value = suggestion;
                form.requestSubmit();
            });

            suggestionsEl.appendChild(button);
        });

        messagesEl.appendChild(suggestionsEl);
        messagesEl.scrollTop = messagesEl.scrollHeight;
    }

    function renderHistory() {
        messagesEl.innerHTML = "";
        suggestionsEl = null;

        if (history.length === 0) {
            if (greeting) {
                renderMessage("assistant", greeting);
            }

            renderSuggestions();
        } else {
            history.forEach((item) => renderMessage(item.role, item.text));
        }
    }

    function openPanel() {
        panel.hidden = false;
        toggleBtn.setAttribute("aria-expanded", "true");
        badge.hidden = true;

        window.setTimeout(() => input.focus(), 80);
    }

    function closePanel() {
        panel.hidden = true;
        toggleBtn.setAttribute("aria-expanded", "false");
    }

    toggleBtn.addEventListener("click", () => {
        if (panel.hidden) {
            openPanel();
        } else {
            closePanel();
        }
    });

    closeBtn.addEventListener("click", closePanel);

    renderHistory();

    if (shouldAutoOpen) {
        window.setTimeout(openPanel, 450);
    } else if (sessionStorage.getItem(REOPEN_KEY)) {
        sessionStorage.removeItem(REOPEN_KEY);
        openPanel();
    }

    if (micBtn) {
        const SpeechRecognitionCtor = window.SpeechRecognition || window.webkitSpeechRecognition;

        if (!SpeechRecognitionCtor) {
            micBtn.disabled = true;
            micBtn.title = voiceUnsupportedText;
        } else {
            const recognition = new SpeechRecognitionCtor();
            recognition.lang = voiceCulture;
            recognition.interimResults = false;
            recognition.maxAlternatives = 1;

            let listening = false;

            recognition.addEventListener("start", () => {
                listening = true;
                micBtn.classList.add("listening");
            });

            recognition.addEventListener("end", () => {
                listening = false;
                micBtn.classList.remove("listening");
            });

            recognition.addEventListener("result", (event) => {
                const transcript = event.results[0]?.[0]?.transcript?.trim();

                if (transcript) {
                    input.value = transcript;
                    form.requestSubmit();
                }
            });

            recognition.addEventListener("error", (event) => {
                let message = voiceErrorText;

                if (event.error === "not-allowed" || event.error === "service-not-allowed") {
                    message = voiceDeniedText;
                } else if (event.error === "no-speech") {
                    message = voiceNoSpeechText;
                }

                if (message) {
                    renderMessage("assistant", message);
                    history.push({ role: "assistant", text: message });
                    saveHistory(history);
                }
            });

            micBtn.addEventListener("click", () => {
                if (listening) {
                    recognition.stop();
                    return;
                }

                removeSuggestions();

                try {
                    recognition.start();
                } catch {
                    // recognition already running or unavailable; ignore
                }
            });
        }
    }

    let sending = false;

    form.addEventListener("submit", async (e) => {
        e.preventDefault();

        const text = input.value.trim();

        if (!text || sending) {
            return;
        }

        sending = true;
        input.value = "";
        input.disabled = true;
        removeSuggestions();

        renderMessage("user", text);
        history.push({ role: "user", text });
        saveHistory(history);

        const thinkingRow = renderMessage("assistant", "…");
        thinkingRow.classList.add("ai-assistant-msg-thinking");

        const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');

        try {
            const response = await fetch("/Assistant/Ask", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": tokenInput ? tokenInput.value : ""
                },
                body: JSON.stringify({
                    message: text,
                    history: history.slice(0, -1)
                })
            });

            const data = await response.json();

            thinkingRow.remove();

            const reply = data && data.reply ? data.reply : errorText;
            renderMessage("assistant", reply);
            history.push({ role: "assistant", text: reply });
            saveHistory(history);

            if (data && data.navigateUrl) {
                sessionStorage.setItem(REOPEN_KEY, "1");
                setTimeout(() => {
                    window.location.href = data.navigateUrl;
                }, 900);
            }
        } catch {
            thinkingRow.remove();
            renderMessage("assistant", errorText);
        } finally {
            sending = false;
            input.disabled = false;
            input.focus();
        }
    });
})();
