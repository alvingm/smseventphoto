const FEED_ENDPOINT = "/api/sms-events";
const REFRESH_INTERVAL_MS = 10000;

const feedElement = document.getElementById("feed");
const statusElement = document.getElementById("status");
const entryTemplate = document.getElementById("entry-template");

function formatTime(value) {
  if (!value) {
    return "Unknown time";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "Unknown time";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function normalizeEntries(payload) {
  if (Array.isArray(payload)) {
    return payload;
  }

  if (payload && Array.isArray(payload.events)) {
    return payload.events;
  }

  return [];
}

function clearFeed() {
  feedElement.replaceChildren();
}

function appendStateMessage(cssClass, text) {
  const message = document.createElement("p");
  message.className = cssClass;
  message.textContent = text;
  feedElement.appendChild(message);
}

function renderFeed(entries) {
  clearFeed();

  if (!entries.length) {
    appendStateMessage("empty-state", "No messages have been received yet.");
    return;
  }

  const sortedEntries = [...entries].sort((a, b) => {
    return new Date(b.receivedAt || b.timestamp || 0) - new Date(a.receivedAt || a.timestamp || 0);
  });

  for (const entry of sortedEntries) {
    const fragment = entryTemplate.content.cloneNode(true);
    const article = fragment.querySelector(".entry");
    const image = fragment.querySelector(".entry-media");
    const mediaWrapper = fragment.querySelector(".entry-media-wrapper");
    const message = fragment.querySelector(".entry-message");
    const phone = fragment.querySelector(".entry-phone");
    const time = fragment.querySelector(".entry-time");

    const bodyText = entry.message || entry.text || "";
    const phoneNumber = entry.phoneNumber || entry.from || "Unknown sender";
    const receivedAt = entry.receivedAt || entry.timestamp || null;
    const photoUrl = entry.photoUrl || entry.mediaUrl || entry.imageUrl || "";

    message.textContent = bodyText || "(No text message)";
    phone.textContent = phoneNumber;
    time.textContent = formatTime(receivedAt);
    time.dateTime = receivedAt || "";

    if (photoUrl) {
      image.src = photoUrl;
      image.alt = `Photo sent by ${phoneNumber}`;
    } else {
      mediaWrapper.remove();
      article.style.gridTemplateColumns = "1fr";
    }

    feedElement.appendChild(fragment);
  }
}

async function loadFeed() {
  try {
    statusElement.textContent = "Refreshing…";
    const response = await fetch(FEED_ENDPOINT, {
      headers: { Accept: "application/json" },
    });

    if (!response.ok) {
      throw new Error(`Request failed with status ${response.status}`);
    }

    const payload = await response.json();
    const entries = normalizeEntries(payload);
    renderFeed(entries);
    statusElement.textContent = `Updated ${new Date().toLocaleTimeString()}`;
  } catch (error) {
    clearFeed();
    appendStateMessage("error-state", "Could not load SMS messages right now. Please try again.");
    statusElement.textContent = "Failed to refresh feed";
  }
}

loadFeed();
setInterval(loadFeed, REFRESH_INTERVAL_MS);
