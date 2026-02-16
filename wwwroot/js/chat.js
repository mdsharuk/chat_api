/**
 * ChatApp - Main JavaScript Application
 * Handles real-time messaging with SignalR, UI interactions, and API calls
 */

// Global variables
let connection = null;
let currentUserId = null;
let currentUsername = null;
let currentChatUserId = null;
let allUsers = [];
let conversations = [];
let messages = {};

/**
 * Initialize the application
 */
document.addEventListener("DOMContentLoaded", function () {
  // Check authentication
  const token = localStorage.getItem("token");
  if (!token) {
    window.location.href = "/Home/Login";
    return;
  }

  // Get user info
  currentUserId = parseInt(localStorage.getItem("userId"));
  currentUsername = localStorage.getItem("username");
  const fullName = localStorage.getItem("fullName") || currentUsername;

  // Display user info
  document.getElementById("currentUserName").textContent = fullName;
  document.getElementById("userInitials").textContent = getInitials(fullName);

  // Initialize SignalR connection
  initializeSignalR();

  // Load initial data
  loadAllUsers();
  loadConversations();

  // Setup event listeners
  setupEventListeners();
});

/**
 * Setup all event listeners for UI interactions
 */
function setupEventListeners() {
  // Logout button
  document.getElementById("logoutBtn").addEventListener("click", logout);

  // Tab switching
  document
    .getElementById("conversationsTab")
    .addEventListener("click", () => switchTab("conversations"));
  document
    .getElementById("allUsersTab")
    .addEventListener("click", () => switchTab("allUsers"));

  // Search functionality
  document
    .getElementById("searchInput")
    .addEventListener("input", handleSearch);

  // Message sending
  document.getElementById("sendBtn").addEventListener("click", sendMessage);
  document.getElementById("messageInput").addEventListener("keypress", (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  });

  // Back button (mobile)
  document.getElementById("backBtn").addEventListener("click", () => {
    document.getElementById("chatContainer").classList.add("hidden");
    document.getElementById("chatArea").classList.add("hidden");
    document.getElementById("chatArea").classList.remove("mobile-active");
    document.getElementById("welcomeScreen").classList.remove("hidden");
    document
      .getElementById("sidebar")
      .classList.remove("mobile-hidden-sidebar");
    // Reset current chat
    currentChatUserId = null;
  });

  // File upload button
  document.getElementById("attachBtn").addEventListener("click", () => {
    document.getElementById("fileInput").click();
  });

  // Handle file selection
  document
    .getElementById("fileInput")
    .addEventListener("change", handleFileUpload);
}

/**
 * Initialize SignalR connection for real-time messaging
 */
async function initializeSignalR() {
  const token = localStorage.getItem("token");

  connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub", {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  // Handle incoming messages
  connection.on("ReceiveMessage", (message) => {
    console.log("Message received:", message);
    handleIncomingMessage(message);
  });

  // Handle private messages with media
  connection.on("ReceivePrivateMessage", (message) => {
    console.log("Private message received:", message);
    handleIncomingMessage(message);
  });

  // Handle message sent confirmation
  connection.on("MessageSent", (message) => {
    console.log("Message sent confirmation:", message);
    handleIncomingMessage(message);
  });

  // Handle user online status
  connection.on("UserOnline", (userId) => {
    console.log("User online:", userId);
    updateUserOnlineStatus(userId, true);
  });

  // Handle user offline status
  connection.on("UserOffline", (userId) => {
    console.log("User offline:", userId);
    updateUserOnlineStatus(userId, false);
  });

  // Handle message read receipt
  connection.on("MessageRead", (messageId) => {
    console.log("Message read:", messageId);
    updateMessageReadStatus(messageId);
  });

  // Handle typing indicator
  connection.on("UserTyping", (userId, isTyping) => {
    if (currentChatUserId === userId) {
      showTypingIndicator(isTyping);
    }
  });

  try {
    await connection.start();
    console.log("SignalR Connected");
  } catch (error) {
    console.error("SignalR Connection Error:", error);
    setTimeout(initializeSignalR, 5000);
  }

  // Handle reconnection
  connection.onreconnected(() => {
    console.log("SignalR Reconnected");
    loadConversations();
  });
}

/**
 * Load all users from the API
 */
async function loadAllUsers() {
  try {
    const response = await apiCall("/api/chat/users", "GET");
    allUsers = response;
    displayAllUsers(allUsers);
  } catch (error) {
    console.error("Error loading users:", error);
    showError("Failed to load users");
  }
}

/**
 * Load conversations from the API
 */
async function loadConversations() {
  try {
    const response = await apiCall("/api/chat/conversations", "GET");
    conversations = response;
    displayConversations(conversations);
  } catch (error) {
    console.error("Error loading conversations:", error);
  }
}

/**
 * Display all users in the sidebar
 */
function displayAllUsers(users) {
  const container = document.getElementById("allUsersList");

  if (users.length === 0) {
    container.innerHTML = `
            <div class="text-center py-8 text-gray-500">
                <i class="fas fa-users text-4xl mb-2"></i>
                <p>No users found</p>
            </div>
        `;
    return;
  }

  container.innerHTML = users
    .map(
      (user) => `
        <div class="flex items-center px-4 py-3 hover:bg-wa-gray cursor-pointer border-b transition user-item" 
             data-user-id="${user.id}" 
             onclick="openChat(${user.id}, '${escapeHtml(user.username)}', ${user.isOnline})">
            <div class="relative mr-3">
                <div class="w-12 h-12 rounded-full bg-wa-blue flex items-center justify-center text-white font-semibold">
                    ${getInitials(user.fullName || user.username)}
                </div>
                ${
                  user.isOnline
                    ? '<span class="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full"></span>'
                    : '<span class="absolute bottom-0 right-0 w-3 h-3 bg-gray-400 border-2 border-white rounded-full"></span>'
                }
            </div>
            <div class="flex-1 min-w-0">
                <div class="flex justify-between items-baseline">
                    <h3 class="font-semibold text-gray-800 truncate">${escapeHtml(user.fullName || user.username)}</h3>
                </div>
                <p class="text-sm text-gray-500 truncate">@${escapeHtml(user.username)}</p>
            </div>
        </div>
    `,
    )
    .join("");
}

/**
 * Display conversations in the sidebar
 */
function displayConversations(convos) {
  const container = document.getElementById("conversationsList");

  if (convos.length === 0) {
    container.innerHTML = `
            <div class="text-center py-8 text-gray-500">
                <i class="fas fa-comments text-4xl mb-2"></i>
                <p>No conversations yet</p>
                <p class="text-sm">Start chatting with someone!</p>
            </div>
        `;
    return;
  }

  container.innerHTML = convos
    .map((convo) => {
      const user = allUsers.find((u) => u.id === convo.otherUserId);
      const isOnline = user ? user.isOnline : convo.isOnline;

      return `
            <div class="flex items-center px-4 py-3 hover:bg-wa-gray cursor-pointer border-b transition conversation-item ${convo.unreadCount > 0 ? "bg-blue-50" : ""}" 
                 data-user-id="${convo.otherUserId}" 
                 onclick="openChat(${convo.otherUserId}, '${escapeHtml(convo.otherUserName)}', ${isOnline})">
                <div class="relative mr-3">
                    <div class="w-12 h-12 rounded-full bg-wa-blue flex items-center justify-center text-white font-semibold">
                        ${getInitials(convo.otherUserName)}
                    </div>
                    ${
                      isOnline
                        ? '<span class="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full"></span>'
                        : '<span class="absolute bottom-0 right-0 w-3 h-3 bg-gray-400 border-2 border-white rounded-full"></span>'
                    }
                </div>
                <div class="flex-1 min-w-0">
                    <div class="flex justify-between items-baseline">
                        <h3 class="font-semibold text-gray-800 truncate">${escapeHtml(convo.otherUserName)}</h3>
                        <span class="text-xs text-gray-500">${formatTime(convo.lastMessageAt)}</span>
                    </div>
                    <div class="flex justify-between items-center">
                        <p class="text-sm text-gray-500 truncate">${escapeHtml(convo.lastMessage || "No messages yet")}</p>
                        ${convo.unreadCount > 0 ? `<span class="bg-wa-green text-white text-xs rounded-full px-2 py-0.5 ml-2">${convo.unreadCount}</span>` : ""}
                    </div>
                </div>
            </div>
        `;
    })
    .join("");
}

/**
 * Open a chat with a specific user
 */
async function openChat(userId, username, isOnline) {
  currentChatUserId = userId;

  // Update UI
  document.getElementById("welcomeScreen").classList.add("hidden");
  document.getElementById("chatContainer").classList.remove("hidden");
  document.getElementById("chatArea").classList.remove("hidden");

  // Mobile: hide sidebar and show chat full screen
  document.getElementById("sidebar").classList.add("mobile-hidden-sidebar");
  document.getElementById("chatArea").classList.add("mobile-active");

  // Update chat header
  document.getElementById("chatUserName").textContent = username;
  document.getElementById("chatUserInitials").textContent =
    getInitials(username);
  document.getElementById("chatUserStatus").textContent = isOnline
    ? "Online"
    : "Offline";

  const onlineIndicator = document.getElementById("chatUserOnlineIndicator");
  if (isOnline) {
    onlineIndicator.classList.remove("bg-gray-400");
    onlineIndicator.classList.add("bg-green-500");
  } else {
    onlineIndicator.classList.remove("bg-green-500");
    onlineIndicator.classList.add("bg-gray-400");
  }

  // Load messages
  await loadMessages(userId);

  // Mark messages as read and clear unread count immediately
  await markMessagesAsRead(userId);

  // Immediately clear unread count in UI
  const conversation = conversations.find((c) => c.otherUserId === userId);
  if (conversation) {
    conversation.unreadCount = 0;
    displayConversations(conversations);
  }

  // Focus message input for better UX
  setTimeout(() => {
    const messageInput = document.getElementById("messageInput");
    if (messageInput) {
      messageInput.focus();
    }
  }, 100);
}

/**
 * Load messages for a conversation
 */
async function loadMessages(otherUserId) {
  try {
    const response = await apiCall(
      `/api/chat/conversation/${otherUserId}`,
      "GET",
    );
    messages[otherUserId] = response;
    displayMessages(messages[otherUserId]);
    // Scroll to bottom after loading
    scrollToBottom();
  } catch (error) {
    console.error("Error loading messages:", error);
    document.getElementById("messagesArea").innerHTML = `
            <div class="text-center text-gray-500">
                <p>Error loading messages</p>
            </div>
        `;
  }
}

/**
 * Display messages in the chat area
 */
function displayMessages(msgs) {
  const container = document.getElementById("messagesArea");

  if (!msgs || msgs.length === 0) {
    container.innerHTML = `
            <div class="text-center text-gray-500">
                <i class="far fa-comments text-4xl mb-2"></i>
                <p>No messages yet</p>
                <p class="text-sm">Start the conversation!</p>
            </div>
        `;
    return;
  }

  container.innerHTML = msgs
    .map((msg) => {
      const isSent = msg.senderId === currentUserId;
      const hasMedia = msg.media && msg.media.length > 0;
      const isDeleted = msg.isDeleted;
      const hasReply = msg.replyToMessage;

      let mediaHtml = "";
      if (hasMedia) {
        const images = msg.media.filter((m) => m.mediaType === 0);
        const videos = msg.media.filter((m) => m.mediaType === 1);

        // Handle images in grid layout
        if (images.length > 0) {
          const maxVisible = 4;
          const remaining = images.length - maxVisible;
          const visibleImages = images.slice(0, maxVisible);

          if (images.length === 1) {
            // Single image - full width
            mediaHtml += `
              <div class="mb-1 rounded-lg overflow-hidden">
                <img src="${images[0].filePath}" 
                     alt="${escapeHtml(images[0].fileName)}" 
                     class="w-full h-auto cursor-pointer hover:opacity-90 transition"
                     style="max-height: 300px; object-fit: cover;"
                     onclick="window.open('${images[0].filePath}', '_blank')"
                     loading="lazy">
              </div>
            `;
          } else if (images.length === 2) {
            // Two images - side by side
            mediaHtml += `
              <div class="grid grid-cols-2 gap-0.5 mb-1 rounded-lg overflow-hidden">
                ${visibleImages
                  .map(
                    (img) => `
                  <div class="relative overflow-hidden">
                    <img src="${img.filePath}" 
                         alt="${escapeHtml(img.fileName)}" 
                         class="w-full h-full cursor-pointer hover:opacity-90 transition"
                         style="height: 200px; object-fit: cover;"
                         onclick="window.open('${img.filePath}', '_blank')"
                         loading="lazy">
                  </div>
                `,
                  )
                  .join("")}
              </div>
            `;
          } else if (images.length === 3) {
            // Three images - first full width, bottom two split
            mediaHtml += `
              <div class="mb-1 rounded-lg overflow-hidden">
                <div class="mb-0.5">
                  <img src="${images[0].filePath}" 
                       alt="${escapeHtml(images[0].fileName)}" 
                       class="w-full cursor-pointer hover:opacity-90 transition"
                       style="height: 150px; object-fit: cover;"
                       onclick="window.open('${images[0].filePath}', '_blank')"
                       loading="lazy">
                </div>
                <div class="grid grid-cols-2 gap-0.5">
                  ${images
                    .slice(1)
                    .map(
                      (img) => `
                    <img src="${img.filePath}" 
                         alt="${escapeHtml(img.fileName)}" 
                         class="w-full cursor-pointer hover:opacity-90 transition"
                         style="height: 100px; object-fit: cover;"
                         onclick="window.open('${img.filePath}', '_blank')"
                         loading="lazy">
                  `,
                    )
                    .join("")}
                </div>
              </div>
            `;
          } else {
            // Four or more images - 2x2 grid with overlay
            mediaHtml += `
              <div class="grid grid-cols-2 gap-0.5 mb-1 rounded-lg overflow-hidden">
                ${visibleImages
                  .map(
                    (img, idx) => `
                  <div class="relative overflow-hidden">
                    <img src="${img.filePath}" 
                         alt="${escapeHtml(img.fileName)}" 
                         class="w-full h-full cursor-pointer hover:opacity-90 transition"
                         style="height: 130px; object-fit: cover;"
                         onclick="window.open('${img.filePath}', '_blank')"
                         loading="lazy">
                    ${
                      idx === maxVisible - 1 && remaining > 0
                        ? `
                      <div class="absolute inset-0 bg-black bg-opacity-60 flex items-center justify-center cursor-pointer"
                           onclick="window.open('${img.filePath}', '_blank')">
                        <span class="text-white text-3xl font-semibold">+${remaining}</span>
                      </div>
                    `
                        : ""
                    }
                  </div>
                `,
                  )
                  .join("")}
              </div>
            `;
          }
        }

        // Handle videos
        videos.forEach((video) => {
          mediaHtml += `
            <div class="mb-1 rounded-lg overflow-hidden">
              <video controls class="w-full h-auto rounded-lg" style="max-height: 300px;">
                <source src="${video.filePath}" type="${video.contentType}">
                Your browser does not support the video tag.
              </video>
            </div>
          `;
        });
      }

      // Check if message is just a media indicator text and we have actual media to show
      const isMediaOnlyMessage =
        hasMedia &&
        msg.content &&
        (msg.content.startsWith("[Image]") ||
          msg.content.startsWith("[Video]"));

      // Reply indicator HTML
      let replyHtml = "";
      if (hasReply) {
        replyHtml = `
          <div class="bg-gray-200 bg-opacity-50 border-l-4 border-wa-green px-2 py-1 mb-1 rounded ${hasMedia ? "mx-1" : ""}">
            <div class="text-xs text-wa-green font-semibold">${escapeHtml(msg.replyToMessage.senderName || "User")}</div>
            <div class="text-xs text-gray-600 truncate">${escapeHtml(msg.replyToMessage.content.substring(0, 50))}${msg.replyToMessage.content.length > 50 ? "..." : ""}</div>
          </div>
        `;
      }

      return `
            <div class="flex ${isSent ? "justify-end" : "justify-start"} message-bubble mb-2">
                <div class="max-w-[70%] sm:max-w-[60%] ${isSent ? "bg-wa-green-light" : "bg-white"} rounded-lg ${hasMedia ? "p-1" : "px-3 py-2"} shadow-sm cursor-pointer hover:shadow-md transition" 
                     oncontextmenu="showMessageOptions(${msg.id}, ${isDeleted ? "'This message was deleted'" : "'" + escapeHtml(msg.content).replace(/'/g, "\\'") + "'"}, ${msg.senderId}, event); return false;">
                    ${replyHtml}
                    ${mediaHtml}
                    ${!isMediaOnlyMessage ? `<p class="${isDeleted ? "text-gray-400 italic" : "text-gray-800"} break-words text-sm sm:text-base ${hasMedia ? "px-2 pb-1" : ""}">${isDeleted ? '<i class="fas fa-ban mr-1"></i>' : ""}${escapeHtml(msg.content)}</p>` : ""}
                    <div class="flex items-center justify-end space-x-1 ${hasMedia ? "px-2 pb-1" : "mt-1"}">
                        <span class="text-xs text-gray-500">${formatTime(msg.sentAt)}</span>
                        ${isSent ? `<i class="fas fa-check${msg.isRead ? "-double text-blue-500" : " text-gray-400"} text-xs"></i>` : ""}
                    </div>
                </div>
            </div>
        `;
    })
    .join("");

  // Scroll to bottom
  scrollToBottom();
}

/**
 * Send a message
 */
async function sendMessage() {
  const input = document.getElementById("messageInput");
  const content = input.value.trim();

  if (!content || !currentChatUserId) return;

  try {
    // Clear input immediately for better UX
    input.value = "";

    // Create temporary message object for immediate display
    const tempMessage = {
      id: Date.now(), // Temporary ID
      content: content,
      senderId: currentUserId,
      receiverId: currentChatUserId,
      sentAt: new Date().toISOString(),
      isRead: false,
    };

    // Add to messages array immediately for instant feedback
    if (!messages[currentChatUserId]) {
      messages[currentChatUserId] = [];
    }
    messages[currentChatUserId].push(tempMessage);
    displayMessages(messages[currentChatUserId]);

    // Send via SignalR with reply context
    if (typeof replyingToMessage !== "undefined" && replyingToMessage) {
      await connection.invoke(
        "SendPrivateMessage",
        currentChatUserId,
        content,
        replyingToMessage.id,
      );
      cancelReply(); // Clear reply state
    } else {
      await connection.invoke("SendPrivateMessage", currentChatUserId, content);
    }

    // Message will be updated via SignalR callback with real ID
  } catch (error) {
    console.error("Error sending message:", error);
    showError("Failed to send message");
    // Remove the temporary message on error
    if (messages[currentChatUserId]) {
      messages[currentChatUserId].pop();
      displayMessages(messages[currentChatUserId]);
    }
    input.value = content; // Restore message on error
  }
}

/**
 * Handle incoming message from SignalR
 */
function handleIncomingMessage(message) {
  console.log("Received message from SignalR:", message);

  // Update or create conversation
  updateConversationWithMessage(message);

  // If the message is for the current chat, add it to the display
  if (
    currentChatUserId === message.senderId ||
    currentChatUserId === message.receiverId
  ) {
    const otherUserId =
      message.senderId === currentUserId
        ? message.receiverId
        : message.senderId;

    if (!messages[otherUserId]) {
      messages[otherUserId] = [];
    }

    // Check if message already exists (to avoid duplicates from temp message)
    const existingIndex = messages[otherUserId].findIndex(
      (m) =>
        m.id === message.id ||
        (m.content === message.content &&
          Math.abs(new Date(m.sentAt) - new Date(message.sentAt)) < 2000),
    );

    if (existingIndex === -1) {
      // New message, add it
      messages[otherUserId].push(message);
    } else {
      // Update existing message (replace temp with real)
      messages[otherUserId][existingIndex] = message;
    }

    displayMessages(messages[otherUserId]);

    // Mark as read if we're viewing the conversation and it's from the other user
    if (
      currentChatUserId === message.senderId &&
      message.senderId !== currentUserId
    ) {
      markMessageAsRead(message.id);
    }
  }

  // Reload conversations to update unread counts
  loadConversations();
}

/**
 * Update conversation list with new message
 */
function updateConversationWithMessage(message) {
  const otherUserId =
    message.senderId === currentUserId ? message.receiverId : message.senderId;

  let conversation = conversations.find((c) => c.otherUserId === otherUserId);

  if (conversation) {
    // Show better preview for media messages
    if (
      message.content &&
      (message.content.startsWith("[Image]") ||
        message.content.startsWith("[Video]"))
    ) {
      conversation.lastMessage = message.content.startsWith("[Image]")
        ? "📷 Photo"
        : "🎥 Video";
    } else {
      conversation.lastMessage = message.content;
    }
    conversation.lastMessageAt = message.sentAt;

    if (
      message.senderId !== currentUserId &&
      currentChatUserId !== message.senderId
    ) {
      conversation.unreadCount = (conversation.unreadCount || 0) + 1;
    }
  }
}

/**
 * Mark messages as read
 */
async function markMessagesAsRead(otherUserId) {
  try {
    const result = await apiCall(
      `/api/chat/conversation/${otherUserId}/read`,
      "POST",
    );
    console.log(`Marked ${result.markedCount} messages as read`);

    // Update local unread count
    const conversation = conversations.find(
      (c) => c.otherUserId === otherUserId,
    );
    if (conversation) {
      conversation.unreadCount = 0;
    }

    // Refresh the conversation list to reflect changes
    displayConversations(conversations);
  } catch (error) {
    console.error("Error marking messages as read:", error);
  }
}

/**
 * Mark a single message as read
 */
async function markMessageAsRead(messageId) {
  try {
    await connection.invoke("MarkMessageAsRead", messageId);
  } catch (error) {
    console.error("Error marking message as read:", error);
  }
}

/**
 * Update message read status
 */
function updateMessageReadStatus(messageId) {
  // Update UI to show double check mark
  const messageElements = document.querySelectorAll(".message-bubble");
  // This is a simplified implementation
  // In a real app, you'd find the specific message and update its check mark
}

/**
 * Update user online status
 */
function updateUserOnlineStatus(userId, isOnline) {
  // Update in allUsers array
  const user = allUsers.find((u) => u.id === userId);
  if (user) {
    user.isOnline = isOnline;
  }

  // Update in conversations
  const conversation = conversations.find((c) => c.otherUserId === userId);
  if (conversation) {
    conversation.isOnline = isOnline;
  }

  // Update UI
  displayAllUsers(allUsers);
  displayConversations(conversations);

  // Update current chat if it's with this user
  if (currentChatUserId === userId) {
    document.getElementById("chatUserStatus").textContent = isOnline
      ? "Online"
      : "Offline";
    const indicator = document.getElementById("chatUserOnlineIndicator");
    if (isOnline) {
      indicator.classList.remove("bg-gray-400");
      indicator.classList.add("bg-green-500");
    } else {
      indicator.classList.remove("bg-green-500");
      indicator.classList.add("bg-gray-400");
    }
  }
}

/**
 * Show typing indicator
 */
function showTypingIndicator(isTyping) {
  const indicator = document.getElementById("typingIndicator");
  if (isTyping) {
    indicator.classList.remove("hidden");
  } else {
    indicator.classList.add("hidden");
  }
}

/**
 * Switch between tabs (Conversations / All Users)
 */
function switchTab(tab) {
  const conversationsTab = document.getElementById("conversationsTab");
  const allUsersTab = document.getElementById("allUsersTab");
  const conversationsList = document.getElementById("conversationsList");
  const allUsersList = document.getElementById("allUsersList");

  if (tab === "conversations") {
    conversationsTab.classList.add(
      "text-wa-green",
      "border-b-2",
      "border-wa-green",
    );
    conversationsTab.classList.remove("text-gray-500");
    allUsersTab.classList.remove(
      "text-wa-green",
      "border-b-2",
      "border-wa-green",
    );
    allUsersTab.classList.add("text-gray-500");

    conversationsList.classList.remove("hidden");
    allUsersList.classList.add("hidden");
  } else {
    allUsersTab.classList.add("text-wa-green", "border-b-2", "border-wa-green");
    allUsersTab.classList.remove("text-gray-500");
    conversationsTab.classList.remove(
      "text-wa-green",
      "border-b-2",
      "border-wa-green",
    );
    conversationsTab.classList.add("text-gray-500");

    allUsersList.classList.remove("hidden");
    conversationsList.classList.add("hidden");
  }
}

/**
 * Handle search
 */
function handleSearch(e) {
  const query = e.target.value.toLowerCase();
  const activeTab = document
    .getElementById("conversationsTab")
    .classList.contains("text-wa-green")
    ? "conversations"
    : "allUsers";

  if (activeTab === "conversations") {
    const filtered = conversations.filter((c) =>
      c.otherUserName.toLowerCase().includes(query),
    );
    displayConversations(filtered);
  } else {
    const filtered = allUsers.filter(
      (u) =>
        u.username.toLowerCase().includes(query) ||
        (u.fullName && u.fullName.toLowerCase().includes(query)),
    );
    displayAllUsers(filtered);
  }
}

/**
 * Logout user
 */
function logout() {
  localStorage.clear();
  window.location.href = "/Home/Login";
}

/**
 * Make authenticated API call
 */
async function apiCall(url, method = "GET", body = null) {
  const token = localStorage.getItem("token");

  const options = {
    method,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  };

  if (body) {
    options.body = JSON.stringify(body);
  }

  const response = await fetch(url, options);

  if (response.status === 401) {
    // Token expired, redirect to login
    logout();
    return;
  }

  if (!response.ok) {
    throw new Error(`API call failed: ${response.statusText}`);
  }

  return await response.json();
}

/**
 * Utility: Get initials from name
 */
function getInitials(name) {
  if (!name) return "?";
  const parts = name.split(" ");
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }
  return name.substring(0, 2).toUpperCase();
}

/**
 * Utility: Escape HTML to prevent XSS
 */
function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

/**
 * Utility: Format time
 */
function formatTime(dateString) {
  if (!dateString) return "";

  const date = new Date(dateString);
  const now = new Date();
  const diff = now - date;

  // Less than 1 minute
  if (diff < 60000) {
    return "Just now";
  }

  // Today
  if (date.toDateString() === now.toDateString()) {
    return date.toLocaleTimeString("en-US", {
      hour: "2-digit",
      minute: "2-digit",
    });
  }

  // Yesterday
  const yesterday = new Date(now);
  yesterday.setDate(yesterday.getDate() - 1);
  if (date.toDateString() === yesterday.toDateString()) {
    return "Yesterday";
  }

  // This week
  if (diff < 7 * 24 * 60 * 60 * 1000) {
    return date.toLocaleDateString("en-US", { weekday: "short" });
  }

  // Older
  return date.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}

/**
 * Utility: Scroll to bottom of messages
 */
function scrollToBottom() {
  const container = document.getElementById("messagesArea");
  if (container) {
    // Use requestAnimationFrame for smoother scrolling
    requestAnimationFrame(() => {
      container.scrollTop = container.scrollHeight;
      // Smooth scroll behavior
      container.scrollTo({
        top: container.scrollHeight,
        behavior: "smooth",
      });
    });
  }
}

/**
 * Show error message
 */
function showError(message) {
  // Simple console error for now
  // You could implement a toast notification system here
  console.error(message);
  alert(message);
}

/**
 * Handle file upload
 */
async function handleFileUpload(event) {
  const file = event.target.files[0];
  if (!file) return;

  // Reset file input
  event.target.value = "";

  // Validate file size (50MB max)
  const maxSize = 50 * 1024 * 1024;
  if (file.size > maxSize) {
    showError("File size exceeds 50MB limit");
    return;
  }

  // Determine media type
  let mediaType;
  if (file.type.startsWith("image/")) {
    mediaType = 0; // Image
  } else if (file.type.startsWith("video/")) {
    mediaType = 1; // Video
  } else {
    showError("Only images and videos are supported");
    return;
  }

  try {
    // Show upload progress
    const messageInput = document.getElementById("messageInput");
    const originalPlaceholder = messageInput.placeholder;
    messageInput.placeholder = "Uploading file...";
    messageInput.disabled = true;

    // Prepare form data
    const formData = new FormData();
    formData.append("File", file);
    formData.append("MediaType", mediaType);

    // Upload file
    const token = localStorage.getItem("token");
    const response = await fetch("/api/media/upload", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
      },
      body: formData,
    });

    if (!response.ok) {
      throw new Error("Upload failed");
    }

    const result = await response.json();

    if (result.success && result.media) {
      // Send message with media
      const mediaMessage = `[${mediaType === 0 ? "Image" : "Video"}] ${result.media.fileName}`;
      await sendMessageWithMedia(mediaMessage, result.media);
    }

    // Reset input
    messageInput.placeholder = originalPlaceholder;
    messageInput.disabled = false;
  } catch (error) {
    console.error("Error uploading file:", error);
    showError("Failed to upload file");
    const messageInput = document.getElementById("messageInput");
    messageInput.placeholder = "Type a message";
    messageInput.disabled = false;
  }
}

/**
 * Send message with media attachment
 */
async function sendMessageWithMedia(content, media) {
  if (!currentChatUserId) return;

  try {
    // Create temporary message for display with media array
    const tempMessage = {
      id: Date.now(),
      content: content,
      senderId: currentUserId,
      receiverId: currentChatUserId,
      sentAt: new Date().toISOString(),
      isRead: false,
      media: [media], // Wrap in array for consistent display
    };

    // Add to messages array
    if (!messages[currentChatUserId]) {
      messages[currentChatUserId] = [];
    }
    messages[currentChatUserId].push(tempMessage);
    displayMessages(messages[currentChatUserId]);

    // Send via SignalR with media ID array and reply context
    if (typeof replyingToMessage !== "undefined" && replyingToMessage) {
      await connection.invoke(
        "SendPrivateMessageWithMedia",
        currentChatUserId,
        content,
        [media.id],
        replyingToMessage.id,
      );
      cancelReply(); // Clear reply state
    } else {
      await connection.invoke(
        "SendPrivateMessageWithMedia",
        currentChatUserId,
        content,
        [media.id],
      );
    }

    // Reload messages after a short delay to get the complete message with media from API
    setTimeout(async () => {
      await loadMessages(currentChatUserId);
    }, 500);
  } catch (error) {
    console.error("Error sending message with media:", error);
    showError("Failed to send message");
    // Remove the temporary message on error
    if (messages[currentChatUserId]) {
      messages[currentChatUserId].pop();
      displayMessages(messages[currentChatUserId]);
    }
  }
}
