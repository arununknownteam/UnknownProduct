# Debug and Testing Guide - Fix AI Chat & PDF Upload Issues

## 🔍 Current Issues Identified

From the screenshots:
1. **AI Chat:** "JSON.parse: unexpected end of data at line 1 column 1 of the JSON data"
2. **PDF Upload:** "Failed to upload document"

## 🛠️ Step-by-Step Debugging

### Step 1: Check Backend Health

Open a new terminal and run these commands:

```bash
# Test 1: Check if backend is running
curl http://localhost:5243/api/health

# Expected response:
# {"status":"healthy","timestamp":"2024-01-01T00:00:00Z","message":"Backend is running"}

# Test 2: Check Python installation
curl http://localhost:5243/api/health/python

# Expected response:
# {"status":"ok","python_version":"Python 3.x.x"}

# Test 3: Check Groq API key
curl http://localhost:5243/api/health/groq

# Expected response:
# {"status":"ok","message":"GROQ_API_KEY is configured"}
```

### Step 2: Check Backend Logs

```bash
# View backend logs
tail -f backend/BackendApi/logs/backend-.log

# View NHibernate logs
tail -f backend/BackendApi/logs/nhibernate-error.log
```

**Look for:**
- Python script errors
- JSON parsing errors
- Database connection errors
- Authentication errors

### Step 3: Test AI Chat API Directly

```bash
# First, login to get a token
curl -X POST http://localhost:5243/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"your-email@example.com","password":"your-password"}'

# Copy the token from response

# Test AI Chat
curl -X POST http://localhost:5243/api/chat \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{"message":"Hello"}'

# Expected response:
# {"reply":"Hello! How can I assist you today?","fallback":false}
```

**If this fails:**
- Check if Python script is working
- Check if GROQ_API_KEY is set
- Check backend logs for errors

### Step 4: Test PDF Upload API Directly

```bash
# Create a test PDF file first (or use any existing PDF)
# Then test upload:

curl -X POST http://localhost:5243/api/documents/upload \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -F "file=@/path/to/your/test.pdf"

# Expected response:
# {"id":"uuid","file_name":"test","file_size":12345,"total_pages":0,"total_chunks":0,"is_processed":false,"uploaded_at":"2024-01-01T00:00:00Z","message":"Document uploaded successfully. Processing in background."}
```

**If this fails:**
- Check if file path is correct
- Check if file is a valid PDF
- Check backend logs for processing errors

## 🔧 Common Fixes

### Fix 1: Python Script Issues

The most common cause is Python script failing. Test manually:

```bash
cd backend/BackendApi/Python

# Activate virtual environment
source .venv/bin/activate  # Linux/Mac
# or .venv\Scripts\activate  # Windows

# Test Python script directly
echo '{"prompt":"Hello"}' | python ai_chat.py

# Expected output:
# {"reply":"Hello! How can I assist you today?"}

# If this fails, check:
# 1. GROQ_API_KEY is set
# 2. Python dependencies are installed
# 3. Internet connection is available
```

### Fix 2: Set Environment Variables

Create or update `.env` file in `backend/BackendApi/`:

```env
# Required for AI Chat
GROQ_API_KEY=your_actual_groq_api_key_here

# Required for JWT Authentication
Jwt__Key=your_jwt_secret_key_here_min_32_chars
Jwt__Issuer=YourApp
Jwt__Audience=YourAppUsers

# Database Connection
ConnectionStrings__DefaultConnection=Host=localhost;Database=your_db;Username=postgres;Password=postgres

# Optional: Python path (if not in PATH)
PYTHON_PATH=/usr/bin/python3
```

**Important:** After changing `.env`, restart the backend!

### Fix 3: Install Python Dependencies

```bash
cd backend/BackendApi/Python

# Create virtual environment (if not exists)
python -m venv .venv

# Activate it
source .venv/bin/activate  # Linux/Mac
# or .venv\Scripts\activate  # Windows

# Install dependencies
pip install --upgrade -r requirements.txt

# Verify installation
pip list | grep -E "openai|pdfplumber|sentence-transformers|numpy"
```

### Fix 4: Check Database

```bash
# Connect to PostgreSQL
psql -U postgres -d your_db

# Check if pg_vector is installed
CREATE EXTENSION IF NOT EXISTS vector;

# Check if tables exist
\dt

# Should see:
# documents
# document_chunks
# posts
# users
# videos
# etc.

# If tables don't exist, restart backend (NHibernate will create them)
```

### Fix 5: Frontend Configuration

The frontend might be using wrong API URL. Check:

```javascript
// frontend/src/services/authService.js
const BASE_URL = "http://localhost:5243/api/auth";  // Should be 5243, not 5000

// frontend/src/components/ChatBox.jsx
const response = await fetch("http://localhost:5243/api/chat", {  // Should be 5243

// frontend/src/components/PDFUpload.jsx
await axios.post('http://localhost:5243/api/documents/upload',  // Should be 5243
```

## 🧪 Quick Diagnostic Script

Create a file `test_api.sh`:

```bash
#!/bin/bash

echo "=== Testing Backend APIs ==="
echo ""

# Test 1: Health check
echo "1. Testing health endpoint..."
curl -s http://localhost:5243/api/health | jq .
echo ""

# Test 2: Python check
echo "2. Testing Python installation..."
curl -s http://localhost:5243/api/health/python | jq .
echo ""

# Test 3: Groq check
echo "3. Testing Groq API key..."
curl -s http://localhost:5243/api/health/groq | jq .
echo ""

echo "=== Tests Complete ==="
echo ""
echo "If any test failed, check the backend logs:"
echo "tail -f backend/BackendApi/logs/backend-.log"
```

Run it:
```bash
chmod +x test_api.sh
./test_api.sh
```

## 📊 Monitoring Backend Logs

### Real-time Log Monitoring

```bash
# Terminal 1: Backend logs
cd backend/BackendApi
tail -f logs/backend-.log

# Terminal 2: NHibernate logs
tail -f logs/nhibernate-error.log
```

### What to Look For

**For AI Chat:**
```
INFO: Chat POST raw body: {"message":"Hello"}
INFO: Python script output: {"reply":"Hello!..."}
INFO: Chat reply: Hello! How can I assist you today?
```

**For PDF Upload:**
```
INFO: Starting to process document: test.pdf
INFO: Successfully processed document: test.pdf, Pages: 3, Chunks: 15
```

**Error Patterns:**
```
ERROR: Python AI script error: ...
ERROR: Unable to parse AI response: ...
ERROR: Python PDF processor error: ...
```

## 🔨 Manual Testing with Swagger

1. Open browser: http://localhost:5243/swagger
2. Find the API endpoint you want to test
3. Click "Try it out"
4. Enter parameters
5. Click "Execute"
6. Check the response

**Test these endpoints:**
- `POST /api/auth/login` - Get authentication token
- `POST /api/chat` - Test AI chat (use token from login)
- `POST /api/documents/upload` - Test PDF upload (use token)
- `GET /api/health` - Check backend status

## 🐛 Specific Error Solutions

### Error: "JSON.parse: unexpected end of data"

**Cause:** Python script returned empty or invalid response

**Solution:**
1. Check if Python script runs: `python ai_chat.py`
2. Check if GROQ_API_KEY is set
3. Check internet connection
4. Check backend logs for Python errors

### Error: "Failed to upload document"

**Cause:** Could be multiple issues

**Solution:**
1. Check if file is a valid PDF
2. Check file size (< 10MB)
3. Check if user is authenticated
4. Check backend logs for specific error
5. Verify upload folder exists and has write permissions

### Error: "Python executable not found"

**Solution:**
```bash
# Set PYTHON_PATH in .env
PYTHON_PATH=/usr/bin/python3

# Or install Python
sudo apt install python3  # Ubuntu/Debian
brew install python3      # Mac
```

### Error: "GROQ_API_KEY not found"

**Solution:**
```bash
# Add to .env file
GROQ_API_KEY=gsk_your_actual_key_here

# Restart backend after changing .env
```

## ✅ Verification Checklist

After applying fixes, verify:

- [ ] Backend starts without errors
- [ ] Health endpoint returns "healthy"
- [ ] Python health check returns "ok"
- [ ] Groq health check returns "ok"
- [ ] Can login successfully
- [ ] AI Chat responds to messages
- [ ] Can upload PDF
- [ ] PDF processes successfully
- [ ] PDF Chat returns responses with citations

## 🚀 Restart Backend After Changes

```bash
# Stop backend (Ctrl+C)

# Start again
cd backend/BackendApi
dotnet run

# Watch for errors in console
```

## 📝 Debug Information to Collect

If issues persist, collect this information:

1. **Backend logs:** `logs/backend-.log`
2. **Error logs:** `logs/nhibernate-error.log`
3. **Console output** when starting backend
4. **Browser console** (F12 → Console tab)
5. **Network tab** (F12 → Network tab → failed requests)
6. **Python script output** when run manually

## 🎯 Quick Fixes to Try Now

1. **Restart backend** (most common fix)
2. **Check .env file** has GROQ_API_KEY
3. **Activate Python venv** before starting backend
4. **Clear browser cache** and reload page
5. **Check browser console** for specific errors

## 📞 Getting Help

If still not working:

1. Share backend logs
2. Share browser console errors
3. Share output from health check endpoints
4. Confirm Python script works manually
5. Confirm GROQ_API_KEY is valid

The most likely issue is:
- **Python script not running** (check PYTHON_PATH)
- **GROQ_API_KEY not set** (check .env file)
- **Backend not restarted** after changes (restart required)