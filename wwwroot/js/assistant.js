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
    const scriptTag = document.currentScript;

    const greeting = scriptTag.getAttribute("data-greeting") || "";
    const errorText = scriptTag.getAttribute("data-error") || "Error.";
    const shouldAutoOpen = scriptTag.getAttribute("data-auto-open") === "true";
    const suggestions = [
        scriptTag.getAttribute("data-suggestion-1"),
        scriptTag.getAttribute("data-suggestion-2"),
        scriptTag.getAttribute("data-suggestion-3")
    ].filter(Boolean);

    const HISTORY_KEY = "ai-assistant-history";

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
