/**
 * Message Actions - Reply and Delete Functionality
 */

let replyingToMessage = null;

/**
 * Show message options menu
 */
function showMessageOptions(messageId, content, senderId, event) {
  event.stopPropagation();

  // Remove any existing menu
  const existingMenu = document.getElementById("messageOptionsMenu");
  if (existingMenu) {
    existingMenu.remove();
  }

  // Create menu
  const menu = document.createElement("div");
  menu.id = "messageOptionsMenu";
  menu.className = "absolute bg-white shadow-lg rounded-lg py-2 z-50";
  menu.style.left = event.pageX + "px";
  menu.style.top = event.pageY + "px";

  // Reply option
  const replyBtn = document.createElement("button");
  replyBtn.className =
    "w-full text-left px-4 py-2 hover:bg-gray-100 flex items-center space-x-2";
  replyBtn.innerHTML =
    '<i class="fas fa-reply text-wa-green"></i><span>Reply</span>';
  replyBtn.onclick = () => {
    setReplyTo(messageId, content, senderId);
    menu.remove();
  };
  menu.appendChild(replyBtn);

  // Delete option (only for own messages)
  if (senderId === currentUserId) {
    const deleteBtn = document.createElement("button");
    deleteBtn.className =
      "w-full text-left px-4 py-2 hover:bg-gray-100 flex items-center space-x-2 text-red-600";
    deleteBtn.innerHTML = '<i class="fas fa-trash"></i><span>Delete</span>';
    deleteBtn.onclick = () => {
      deleteMessage(messageId);
      menu.remove();
    };
    menu.appendChild(deleteBtn);
  }

  document.body.appendChild(menu);

  // Close menu on click outside
  setTimeout(() => {
    document.addEventListener("click", function closeMenu() {
      menu.remove();
      document.removeEventListener("click", closeMenu);
    });
  }, 100);
}

/**
 * Set reply to message
 */
function setReplyTo(messageId, content, senderId) {
  replyingToMessage = { id: messageId, content, senderId };

  // Show reply bar
  const replyBar = document.getElementById("replyBar") || createReplyBar();

  if (!replyBar) {
    return;
  }

  const senderName =
    messages[currentChatUserId]?.find((m) => m.id === messageId)?.senderName ||
    "User";

  document.getElementById("replyToUserName").textContent = senderName;
  document.getElementById("replyToContent").textContent =
    content.substring(0, 50) + (content.length > 50 ? "..." : "");
  replyBar.classList.remove("hidden");

  document.getElementById("messageInput").focus();
}

/**
 * Cancel reply
 */
function cancelReply() {
  replyingToMessage = null;
  const replyBar = document.getElementById("replyBar");
  if (replyBar) {
    replyBar.classList.add("hidden");
  }
}

/**
 * Create reply bar element
 */
function createReplyBar() {
  const messageInputContainer = document.getElementById(
    "messageInputContainer",
  );
  const messageInputArea = document.getElementById("messageInputArea");

  if (!messageInputContainer || !messageInputArea) {
    return null;
  }

  const replyBar = document.createElement("div");
  replyBar.id = "replyBar";
  replyBar.className =
    "bg-gray-100 px-4 py-2 flex items-center justify-between border-l-4 border-wa-green hidden";
  replyBar.innerHTML = `
        <div class="flex-1">
            <div class="text-xs text-wa-green font-semibold" id="replyToUserName"></div>
            <div class="text-sm text-gray-600 truncate" id="replyToContent"></div>
        </div>
        <button onclick="cancelReply()" class="text-gray-400 hover:text-gray-600">
            <i class="fas fa-times"></i>
        </button>
    `;
  messageInputContainer.insertBefore(replyBar, messageInputArea);
  return replyBar;
}

/**
 * Delete message
 */
async function deleteMessage(messageId) {
  if (!confirm("Delete this message?")) return;

  try {
    const token = localStorage.getItem("token");
    const response = await fetch(`/api/chat/message/${messageId}`, {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });

    if (response.ok) {
      // Reload messages to show deletion
      await loadMessages(currentChatUserId);
    } else {
      showError("Failed to delete message");
    }
  } catch (error) {
    showError("Failed to delete message");
  }
}

// Expose functions globally
window.showMessageOptions = showMessageOptions;
window.cancelReply = cancelReply;
window.setReplyTo = setReplyTo;
window.deleteMessage = deleteMessage;
