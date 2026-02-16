# 🚀 Real-Time Chat Application (ASP.NET Core MVC)

A secure and scalable real-time chat application built with ASP.NET Core MVC, SignalR, and PostgreSQL.

## 📌 Project Overview

This application provides instant messaging between users using WebSocket-based real-time communication powered by SignalR. The system follows the MVC (Model-View-Controller) design pattern to ensure separation of concerns, maintainability, and scalability.

## 🏗️ Architecture – MVC Pattern

### 🔹 Model

- **User entity** (ApplicationUser)
- **Message entity**
- **Conversation entity**
- **Group entity**
- **GroupMember entity**
- **Connection tracking entity**

Handles database structure, business rules, and Entity Framework Core integration.

### 🔹 View

- **Razor Views (.cshtml)**
- Real-time chat UI
- Login & registration pages
- Admin dashboard

### 🔹 Controller

- **AuthController** (API) - Authentication
- **ChatController** (API) - Private messaging
- **GroupController** (API) - Group chat
- **AdminController** (API) - Admin operations
- **AuthViewController** - Render login/register views
- **ChatViewController** - Render chat UI

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core MVC (.NET 10)
- **Database**: PostgreSQL (managed via pgAdmin)
- **ORM**: Entity Framework Core
- **Real-Time Communication**: SignalR
- **Authentication**: JWT + Identity
- **Frontend**: Razor Views + Bootstrap 5 + JavaScript
- **API Documentation**: Swagger

## 🔐 Security Features

- Password hashing with Identity
- JWT token-based authentication
- Role-based authorization (Admin / User)
- Secure SignalR hub connections
- HTTPS enforcement

## 💬 Core Features

### 1️⃣ Real-Time Private Messaging

- Instant message delivery
- Live message updates without page reload
- Message timestamps
- Read/unread tracking

### 2️⃣ Group Chat System

- Create chat rooms
- Add/remove members
- Real-time group message broadcasting
- Group admin privileges

### 3️⃣ Online Presence Tracking

- Show online/offline users
- Track active SignalR connections
- Last seen feature
- Typing indicators

### 4️⃣ Message History

- Store messages in PostgreSQL
- Load previous conversations
- Pagination for performance

### 5️⃣ Admin Panel

- Manage users
- Monitor conversations
- Control roles & permissions

## 📦 Prerequisites

Before running the application, ensure you have:

- **.NET 10 SDK** (or compatible version)
- **PostgreSQL** (version 12 or higher)
- **pgAdmin** (optional, for database management)
- **Visual Studio 2022** or **VS Code**

## ⚙️ Setup Instructions

### 1. Clone the Repository

```bash
cd c:/Users/srt/OneDrive/Desktop/chat_dotnet
```

### 2. Configure PostgreSQL Database

1. Open pgAdmin and create a new database named `ChatAppDb`
2. Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ChatAppDb;Username=postgres;Password=your_password"
  }
}
```

**Replace `your_password` with your actual PostgreSQL password.**

### 3. Update JWT Settings (Optional)

In `appsettings.json`, you can customize JWT settings:

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGeneration12345678",
    "Issuer": "ChatApp",
    "Audience": "ChatAppUsers",
    "ExpiryMinutes": 1440
  }
}
```

### 4. Restore NuGet Packages

```bash
dotnet restore
```

### 5. Apply Database Migrations

The application will automatically apply migrations on startup, but you can also run manually:

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migrations to database
dotnet ef database update
```

**Note**: If you encounter migration errors, ensure:

- PostgreSQL service is running
- Connection string is correct
- Database exists in PostgreSQL

### 6. Run the Application

```bash
dotnet run
```

Or press **F5** in Visual Studio to run with debugging.

The application will start at:

- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000
- **Swagger UI**: https://localhost:5001/swagger

## 📝 Default Admin Account

The application creates a default admin account on first run:

- **Username**: `admin`
- **Email**: `admin@chatapp.com`
- **Password**: `Admin@123`
- **Role**: Admin

**⚠️ Important**: Change the admin password after first login!

## 🗄️ Database Schema

### Main Tables:

- **AspNetUsers** - User accounts (with custom fields)
- **AspNetRoles** - User roles (Admin, User)
- **Messages** - Chat messages
- **Conversations** - Private conversations between two users
- **Groups** - Chat groups/rooms
- **GroupMembers** - Group membership
- **Connections** - Active SignalR connections

### Relationships:

- One-to-Many: User → Messages
- One-to-Many: Conversation → Messages
- One-to-Many: Group → Messages
- Many-to-Many: Users ↔ Groups (through GroupMembers)

## 🔌 API Endpoints

### Authentication

- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/logout` - Logout user
- `GET /api/auth/profile` - Get user profile

### Chat

- `GET /api/chat/users` - Get all users
- `GET /api/chat/conversations` - Get user's conversations
- `GET /api/chat/conversation/{userId}` - Get messages with specific user
- `GET /api/chat/search?query={query}` - Search users
- `POST /api/chat/mark-read/{messageId}` - Mark message as read

### Group

- `GET /api/group` - Get user's groups
- `GET /api/group/{groupId}` - Get group details
- `POST /api/group` - Create new group
- `GET /api/group/{groupId}/members` - Get group members
- `GET /api/group/{groupId}/messages` - Get group messages
- `POST /api/group/{groupId}/members` - Add member to group
- `DELETE /api/group/{groupId}/members/{memberId}` - Remove member
- `DELETE /api/group/{groupId}` - Delete group

### Admin (Requires Admin Role)

- `GET /api/admin/users` - Get all users
- `GET /api/admin/users/{userId}` - Get user details
- `DELETE /api/admin/users/{userId}` - Delete user
- `POST /api/admin/users/{userId}/roles` - Add role to user
- `DELETE /api/admin/users/{userId}/roles/{role}` - Remove role
- `GET /api/admin/statistics` - Get system statistics
- `GET /api/admin/conversations` - Get all conversations
- `GET /api/admin/groups` - Get all groups
- `DELETE /api/admin/groups/{groupId}` - Delete group

## 🔄 SignalR Hub Methods

### Client → Server:

- `SendPrivateMessage(receiverId, content)` - Send private message
- `SendGroupMessage(groupId, content)` - Send group message
- `MarkMessageAsRead(messageId)` - Mark message as read
- `Typing(receiverId)` - Notify user is typing
- `StopTyping(receiverId)` - Stop typing notification

### Server → Client:

- `ReceivePrivateMessage(message)` - Receive private message
- `ReceiveGroupMessage(message)` - Receive group message
- `UserOnline(userId)` - User came online
- `UserOffline(userId)` - User went offline
- `UserTyping(userId)` - User is typing
- `UserStoppedTyping(userId)` - User stopped typing
- `MessageSent(message)` - Message sent confirmation
- `MessageRead(messageId)` - Message read confirmation

## 🧪 Testing the Application

### 1. Register Users

1. Navigate to `/Auth/Register`
2. Create multiple test accounts

### 2. Login

1. Navigate to `/Auth/Login`
2. Login with your credentials

### 3. Start Chatting

1. Click "New Chat" to find users
2. Select a user to start a conversation
3. Send messages in real-time

### 4. Create Groups

1. Use the API endpoint to create groups
2. Add members
3. Send group messages

## ⚡ Performance Optimizations

- **Asynchronous programming** (async/await throughout)
- **Optimized EF Core queries** with proper includes
- **Database indexing** on frequently queried columns
- **SignalR connection pooling**
- **Message pagination** to limit data transfer
- **Clean separation of concerns** using MVC

## 🐛 Troubleshooting

### Database Connection Issues

- Verify PostgreSQL is running: `sudo systemctl status postgresql` (Linux) or check Services (Windows)
- Test connection string in pgAdmin
- Check firewall settings

### Migration Errors

```bash
# Drop database and recreate
dotnet ef database drop
dotnet ef database update
```

### SignalR Connection Fails

- Check CORS settings
- Verify JWT token is valid
- Check browser console for errors

### Port Already in Use

```bash
# Change port in Properties/launchSettings.json
# Or kill the process using the port
```

## 📚 Project Structure

```
chat_dotnet/
├── Controllers/
│   ├── AuthController.cs (API)
│   ├── ChatController.cs (API)
│   ├── GroupController.cs (API)
│   ├── AdminController.cs (API)
│   ├── AuthViewController.cs (Views)
│   ├── ChatViewController.cs (Views)
│   └── HomeController.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Hubs/
│   └── ChatHub.cs
├── Models/
│   ├── ApplicationUser.cs
│   ├── Message.cs
│   ├── Conversation.cs
│   ├── Group.cs
│   ├── GroupMember.cs
│   ├── Connection.cs
│   └── DTOs/
│       ├── AuthDtos.cs
│       ├── ChatDtos.cs
│       └── GroupDtos.cs
├── Services/
│   └── TokenService.cs
├── Views/
│   ├── Auth/
│   │   ├── Login.cshtml
│   │   └── Register.cshtml
│   ├── Chat/
│   │   └── Index.cshtml
│   └── Shared/
│       └── _Layout.cshtml
├── wwwroot/
│   ├── css/
│   ├── js/
│   │   ├── chat.js
│   │   └── site.js
│   └── lib/
├── appsettings.json
├── Program.cs
└── chat_dotnet.csproj
```

## 🔮 Future Enhancements

- [ ] File/image sharing
- [ ] Voice/video calls
- [ ] Message reactions (emojis)
- [ ] Message editing and deletion
- [ ] Advanced search and filters
- [ ] User profile customization
- [ ] Push notifications
- [ ] Message encryption
- [ ] Mobile app (Xamarin/MAUI)

## 📄 License

This project is for educational and demonstration purposes.

## 👨‍💻 Developer Notes

- Always use `async/await` for database operations
- Keep controllers thin, move logic to services
- Use DTOs for API responses
- Validate all user inputs
- Log errors and important events
- Keep SignalR hub methods focused
- Test with multiple concurrent users

## 🤝 Contributing

Feel free to fork this project and submit pull requests for improvements!

---

**Happy Coding! 🚀**
