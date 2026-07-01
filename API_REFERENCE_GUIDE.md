# UnknownProduct - Complete API Reference Guide

## Backend API Documentation

---

## **CONTROLLERS** (`namespace BackendApi.Controllers`)

### **1. AuthController : ControllerBase**
**Purpose**: Handles user authentication (login/register) and JWT token generation

#### Methods:
- **`AuthController(LoginService loginService)`**
  - **Type**: Constructor
  - **Purpose**: Initialize AuthController with login service dependency

- **`IActionResult Register(RegisterRequest request)`**
  - **Endpoint**: `POST /api/auth/register`
  - **Purpose**: Register a new user account with email, username, and password
  - **Input**: RegisterRequest (UserName, Email, Password)
  - **Output**: User registration response

- **`IActionResult Login(LoginRequest request)`**
  - **Endpoint**: `POST /api/auth/login`
  - **Purpose**: Authenticate user credentials and return JWT token for session
  - **Input**: LoginRequest (Email, Password)
  - **Output**: JWT token string for authenticated requests

---

### **2. ChatController : ControllerBase**
**Attributes**: `[Authorize]` - Requires valid JWT token
**Purpose**: Handle AI chat messages with LLM fallback

#### Methods:
- **`ChatController(LLMService llmService, ChatFallbackService fallbackService)`**
  - **Type**: Constructor
  - **Purpose**: Initialize ChatController with AI services

- **`Task<IActionResult> Post()`**
  - **Endpoint**: `POST /api/chat`
  - **Purpose**: Process chat message through LLM service, fallback to rule-based bot if LLM fails
  - **Input**: ChatRequest (Message or Messages array for history)
  - **Output**: AI-generated chat response

---

### **3. UserController : ControllerBase**
**Attributes**: `[Authorize]` - Requires valid JWT token
**Purpose**: Retrieve authenticated user profile information

#### Methods:
- **`UserController(ISession session)`**
  - **Type**: Constructor
  - **Purpose**: Initialize UserController with NHibernate session

- **`IActionResult GetProfile()`**
  - **Endpoint**: `GET /api/user/profile`
  - **Purpose**: Fetch current authenticated user's profile data (name, bio, image, posts, etc.)
  - **Input**: JWT token (from authorization header)
  - **Output**: User profile object

---

## **SERVICES** (`namespace BackendApi.Services`)

### **1. LoginService**
**Purpose**: Handle user authentication logic, password hashing, JWT token generation

#### Methods:
- **`LoginService(NHibernateSession session, IConfiguration configuration)`**
  - **Type**: Constructor
  - **Purpose**: Initialize with database session and configuration for JWT secret

- **`string Login(LoginRequest request)`**
  - **Purpose**: Authenticate user by email/password, verify with BCrypt, generate JWT token
  - **Input**: LoginRequest (Email, Password)
  - **Output**: JWT token string
  - **Process**: Query user by email → BCrypt password verification → Generate token

- **`void Register(RegisterRequest request)`**
  - **Purpose**: Create new user account with email validation and password hashing
  - **Input**: RegisterRequest (UserName, Email, Password)
  - **Process**: Validate email uniqueness → Hash password with BCrypt → Save to database

- **`string GenerateJwtToken(User user)` (Private)**
  - **Purpose**: Create signed JWT token with user claims for session management
  - **Input**: User object
  - **Output**: Encoded JWT token string
  - **Claims**: User ID, Email, UserName (7-day expiry)

---

### **2. LLMService**
**Purpose**: Integrate with Python-based LLM (Large Language Model) for AI chat responses

#### Methods:
- **`LLMService(IWebHostEnvironment env, ILogger<LLMService> logger)`**
  - **Type**: Constructor
  - **Purpose**: Initialize with web environment and logging

- **`string GetPythonExecutable()` (Private)**
  - **Purpose**: Locate Python executable path in system
  - **Output**: Full path to python/python3 binary
  - **Logic**: Check system PATH, fallback to common locations

- **`Task<string> AskAsync(string prompt)`**
  - **Purpose**: Send chat prompt to Python AI script and receive response
  - **Input**: User chat message (prompt)
  - **Output**: AI-generated response string
  - **Process**: Call Python subprocess → Send prompt → Parse response → Handle timeout

---

### **3. ChatFallbackService**
**Purpose**: Rule-based chatbot fallback when LLM service is unavailable

#### Methods:
- **`string Respond(string input)`**
  - **Purpose**: Process user input with regex patterns and return predefined responses
  - **Input**: User message
  - **Output**: Rule-matched or generic fallback response
  - **Patterns**: 
    - "hello|hi|hey" → Greeting responses
    - "how are you" → Status responses
    - "your name" → Identity responses
    - "exit|bye|goodbye" → Farewell responses

- **`string Pick(string[] options)` (Private)**
  - **Purpose**: Randomly select one response from array
  - **Input**: Array of response strings
  - **Output**: Random response string for variety

---

## **DTOs** (`namespace BackendApi.DTOs`)
**Purpose**: Data Transfer Objects for API request/response serialization

### **1. LoginRequest**
```csharp
// Properties used in POST /api/auth/login
public class LoginRequest {
    public string Email { get; set; }           // User email address
    public string Password { get; set; }        // User password (plain text)
}
```

### **2. RegisterRequest**
```csharp
// Properties used in POST /api/auth/register
public class RegisterRequest {
    public string UserName { get; set; }        // Unique username
    public string Email { get; set; }           // Unique email address
    public string Password { get; set; }        // Password to hash
}
```

### **3. ChatRequest**
```csharp
// Properties used in POST /api/chat
public class ChatRequest {
    public string Message { get; set; }                 // Single chat message
    public List<ChatMessage> Messages { get; set; }    // Message history for context
}
```

### **4. ChatMessage**
```csharp
// Message object in conversation history
public class ChatMessage {
    public string Role { get; set; }           // "user" or "assistant"
    public string Content { get; set; }        // Message text
}
```

---

## **ENTITIES** (`namespace BackendApi.Entities`)
**Purpose**: Database models with NHibernate ORM mappings

### **1. User**
**Purpose**: User account entity with authentication and profile data
```csharp
public class User {
    Guid Id                      // Primary key - unique user identifier
    string UserName              // Username for login
    string Email                 // Email (unique, used for login)
    string PasswordHash          // BCrypt hashed password
    string Bio                   // User biography/about section
    string ProfileImageUrl       // Profile picture URL
    bool IsPrivate               // Privacy setting
    bool IsPublic                // Public visibility flag
    DateTime CreatedAt           // Account creation timestamp
    IList<Post> Posts            // User's posts (navigation)
    IList<Video> Videos          // User's videos (navigation)
}
```

### **2. Post**
**Purpose**: Social media post with media and engagement metrics
```csharp
public class Post {
    Guid Id                      // Primary key
    User User                    // Post author (foreign key)
    string Caption               // Post caption/description
    string MediaUrl              // URL to image/video file
    string MediaType             // "image" or "video"
    int LikesCount               // Number of likes
    DateTime CreatedAt           // Post creation timestamp
}
```

### **3. Comment**
**Purpose**: Comments on posts (currently unused in controllers)
```csharp
public class Comment {
    Guid Id                      // Primary key
    User User                    // Comment author
    Post Post                    // Parent post
    string Content               // Comment text
    DateTime CreatedAt           // Timestamp
}
```

### **4. Like**
**Purpose**: Like/reaction on posts (currently unused)
```csharp
public class Like {
    Guid Id                      // Primary key
    User User                    // User who liked
    Post Post                    // Liked post
    DateTime CreatedAt           // Like timestamp
}
```

### **5. Follow**
**Purpose**: User follow relationship (currently unused)
```csharp
public class Follow {
    Guid Id                      // Primary key
    User Follower                // Following user
    User Following               // Followed user
    DateTime CreatedAt           // Follow timestamp
}
```

### **6. Story**
**Purpose**: Temporary user stories (currently unused)
```csharp
public class Story {
    Guid Id                      // Primary key
    User User                    // Story author
    string MediaUrl              // Story media URL
    DateTime ExpiresAt           // Story expiration time (auto-delete)
    DateTime CreatedAt           // Creation timestamp
}
```

### **7. Video**
**Purpose**: User uploaded videos (currently unused)
```csharp
public class Video {
    Guid Id                      // Primary key
    User User                    // Video uploader
    string Title                 // Video title
    string Description           // Video description
    string VideoUrl              // Video file URL
    string ThumbnailUrl          // Video thumbnail image
    long ViewsCount              // Total views
    DateTime CreatedAt           // Upload timestamp
}
```

### **8. Notification**
**Purpose**: User notifications (currently unused)
```csharp
public class Notification {
    Guid Id                      // Primary key
    User User                    // Notification recipient
    string Type                  // Notification type
    bool IsRead                  // Read/unread status
    DateTime CreatedAt           // Notification timestamp
}
```

---

## **REPOSITORIES** (`namespace BackendApi.Repositories`)

### **UserRepository**
**Purpose**: Data access for user queries (currently unused - LoginService queries directly)
- Defines user database operations
- Not integrated into current authentication flow

---

## **FRONTEND COMPONENTS** (`namespace: N/A - React JSX`)

### **1. src/pages/AuthPage.jsx**
**Purpose**: Login/Register page switcher
#### Components:
- **`AuthPage()`** - Main auth page component
  - Toggle between login and register forms
  - Handle form submissions
  - Store JWT token in localStorage

### **2. src/pages/Dashboard.jsx**
**Purpose**: Authenticated user dashboard with profile and chat
#### Components:
- **`Dashboard()`** - Main dashboard after login
  - Display user profile information
  - Integrate ChatBox component
  - Show user's posts/profile data

### **3. src/components/LoginForm.jsx**
**Purpose**: Login form component
#### Methods:
- **`LoginForm()`** - Render login form
  - Email and password inputs
  - Call `authService.loginUser()`
  - Store token on success

### **4. src/components/RegisterForm.jsx**
**Purpose**: Registration form component
#### Methods:
- **`RegisterForm()`** - Render registration form
  - Username, email, password inputs
  - Call `authService.registerUser()`
  - Redirect to login on success

### **5. src/components/ChatBox.jsx**
**Purpose**: AI chat interface component
#### Methods:
- **`ChatBox()`** - Render chat interface
  - Send chat messages to backend
  - Display message history
  - Show loading states
  - Handle errors with fallback messages

---

## **FRONTEND SERVICES** (`frontend/src/services/`)

### **authService.js**
**Purpose**: Authentication API client
#### Methods:
- **`loginUser(email, password)`**
  - Call `POST /api/auth/login`
  - Store JWT token in localStorage
  - Return token

- **`registerUser(username, email, password)`**
  - Call `POST /api/auth/register`
  - Create new account
  - Return success response

- **`getAuthHeaders()`**
  - Return Authorization header with Bearer token
  - Used in all authenticated requests

---

## **SECURITY IMPLEMENTATION**

### **JWT Token Flow**:
1. User provides email + password → LoginController
2. LoginService verifies password with BCrypt
3. GenerateJwtToken() creates signed token with claims
4. Token returned to frontend, stored in localStorage
5. Frontend sends token in Authorization header for authenticated endpoints
6. Backend validates token signature, issuer, audience, and expiration

### **Password Security**:
- Passwords hashed with BCrypt (4.2.0) during registration
- Never stored in plain text
- Verification uses BCrypt.Verify() in Login method

---

## **API ENDPOINTS SUMMARY**

| Method | Endpoint | Auth | Purpose |
|--------|----------|------|---------|
| POST | `/api/auth/register` | ❌ | Register new user |
| POST | `/api/auth/login` | ❌ | Login and get JWT token |
| GET | `/api/user/profile` | ✅ | Get authenticated user profile |
| POST | `/api/chat` | ✅ | Send chat message to AI |

---

## **CURRENT USAGE STATUS**

### **In Use**:
- ✅ User, Post entities
- ✅ AuthController, UserController, ChatController
- ✅ LoginService, LLMService, ChatFallbackService
- ✅ LoginRequest, RegisterRequest, ChatRequest DTOs

### **Unused** (Defined but not referenced):
- ❌ Comment, Like, Follow, Story, Video, Notification entities
- ❌ UserRepository class
- ❌ Post, Comment, Like, Follow, Video, Notification in controllers

---

**Last Updated**: July 2, 2026
**Framework**: ASP.NET Core 8.0 + React 19
**Database**: NHibernate ORM with PostgreSQL
