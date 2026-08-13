# Testing Guide - RAG PDF Chat System

## ✅ Pre-Testing Checklist

### 1. Backend Configuration
- [ ] PostgreSQL is running with pg_vector extension enabled
- [ ] `.env` file exists in `backend/BackendApi/` with:
  - `GROQ_API_KEY` - Your Groq API key
  - `Jwt__Key` - JWT secret key
  - `ConnectionStrings__DefaultConnection` - Database connection string
- [ ] Python virtual environment is activated
- [ ] Python dependencies installed (`pip install -r requirements.txt`)

### 2. Frontend Configuration
- [ ] Node.js and npm installed
- [ ] Dependencies installed (`npm install`)
- [ ] Axios installed (`npm install axios`)

### 3. Database Setup
```sql
-- Connect to PostgreSQL and run:
CREATE EXTENSION IF NOT EXISTS vector;
```

## 🚀 Starting the Application

### Terminal 1 - Backend
```bash
cd backend/BackendApi
dotnet run
```
**Expected output:**
- NHibernate SessionFactory initialized
- Listening on http://localhost:5243
- Swagger available at http://localhost:5243/swagger

### Terminal 2 - Frontend
```bash
cd frontend
npm run dev
```
**Expected output:**
- Vite dev server running
- Frontend available at http://localhost:5173

## 🧪 Testing AI Chat (Existing Feature)

### Test 1: Basic AI Chat
1. **Navigate to:** http://localhost:5173
2. **Login** with existing credentials
3. **Go to:** AI Models page
4. **Click:** "AI Chat" tab (should be default)
5. **Send message:** "Hello, how are you?"
6. **Expected Result:**
   - ✅ Message appears in chat
   - ✅ AI responds with greeting
   - ✅ No errors in console

### Test 2: AI Chat Error Handling
1. **Send message:** "Test message"
2. **Check:** Message appears immediately
3. **Wait for:** AI response
4. **Expected Result:**
   - ✅ Loading indicator shows while waiting
   - ✅ Response appears when ready
   - ✅ Error handling works if API fails

## 🧪 Testing PDF Chat (New Feature)

### Test 3: PDF Upload
1. **Navigate to:** AI Models page
2. **Click:** "PDF Chat" tab
3. **Click:** "+ Upload PDF" button
4. **Select a PDF file** (recommended: 2-3 page document for testing)
5. **Expected Result:**
   - ✅ Upload dialog appears
   - ✅ File selected shows "Processing document..."
   - ✅ Redirects to document list
   - ✅ Document appears in list with "⏳ Processing..." status

### Test 4: Document Processing
1. **Wait** 10-30 seconds for processing
2. **Refresh** the page or wait for auto-update
3. **Expected Result:**
   - ✅ Status changes to "✅ Ready to chat"
   - ✅ Shows correct page count
   - ✅ Shows chunk count (typically 5-20 chunks for small docs)

### Test 5: PDF Chat - Basic Question
1. **Click:** "💬 Chat" button on processed document
2. **Ask:** "What is this document about?"
3. **Expected Result:**
   - ✅ Message appears in chat
   - ✅ AI responds with relevant information
   - ✅ Citations appear below response
   - ✅ Citations show: [1], filename, page number, match percentage

### Test 6: PDF Chat - Specific Question
1. **Ask:** "Can you summarize the main points?"
2. **Expected Result:**
   - ✅ AI provides summary based on document content
   - ✅ Multiple citations may appear
   - ✅ Response is contextually relevant

### Test 7: PDF Chat - Page Reference
1. **Look at** the citations in the response
2. **Click on** a citation
3. **Expected Result:**
   - ✅ PDF opens in new tab
   - ✅ Browser downloads or displays PDF

### Test 8: Document Management
1. **Go back** to document list
2. **Upload another PDF**
3. **Expected Result:**
   - ✅ Second document appears in list
   - ✅ Both documents show independently
   - ✅ Can chat with either document

### Test 9: Delete Document
1. **Click:** "🗑 Delete" on a document
2. **Confirm** deletion
3. **Expected Result:**
   - ✅ Document removed from list
   - ✅ Associated chunks deleted from database
   - ✅ File removed from filesystem

## 🔍 API Testing with curl/Postman

### Test 10: Upload Document API
```bash
curl -X POST http://localhost:5243/api/documents/upload \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@/path/to/test.pdf"
```

**Expected Response:**
```json
{
  "id": "uuid",
  "file_name": "test",
  "file_size": 12345,
  "total_pages": 0,
  "total_chunks": 0,
  "is_processed": false,
  "uploaded_at": "2024-01-01T00:00:00Z",
  "message": "Document uploaded successfully. Processing in background."
}
```

### Test 11: Get Documents API
```bash
curl http://localhost:5243/api/documents \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Expected Response:**
```json
[
  {
    "id": "uuid",
    "file_name": "test",
    "file_size": 12345,
    "total_pages": 3,
    "total_chunks": 15,
    "is_processed": true,
    "uploaded_at": "2024-01-01T00:00:00Z",
    "processed_at": "2024-01-01T00:00:05Z"
  }
]
```

### Test 12: PDF Chat API
```bash
curl -X POST http://localhost:5243/api/chat/pdf \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What is this document about?",
    "documentId": "UUID_OF_DOCUMENT"
  }'
```

**Expected Response:**
```json
{
  "reply": "This document is about...",
  "document_id": "uuid",
  "document_name": "test.pdf",
  "citations": [
    {
      "citation_id": 1,
      "document_id": "uuid",
      "file_name": "test.pdf",
      "page_number": 1,
      "text_snippet": "relevant text from document...",
      "relevance_score": 0.95
    }
  ]
}
```

## 🐛 Common Issues and Solutions

### Issue 1: "Axios not found" Error
**Solution:**
```bash
cd frontend
npm install axios
```

### Issue 2: "Python executable not found"
**Solution:**
```bash
# Set PYTHON_PATH environment variable
export PYTHON_PATH=/usr/bin/python3  # Linux/Mac
# or
set PYTHON_PATH=C:\Python39\python.exe  # Windows
```

### Issue 3: "pg_vector extension not found"
**Solution:**
```sql
-- Connect to PostgreSQL as superuser
CREATE EXTENSION IF NOT EXISTS vector;
```

### Issue 4: "Embedding model download fails"
**Solution:**
- Ensure internet connection is available
- First run downloads ~100MB model
- Check Python environment has internet access

### Issue 5: "Document processing stuck"
**Solution:**
1. Check backend logs: `backend/BackendApi/logs/`
2. Verify Python script is executable
3. Check PDF is not password protected
4. Ensure sufficient disk space

### Issue 6: "Rate limit reached"
**Solution:**
- Wait 1-2 minutes before retrying
- Check Groq API quota
- Consider upgrading Groq plan

### Issue 7: "CORS errors in browser"
**Solution:**
- Backend already has CORS enabled
- Clear browser cache
- Check browser console for specific errors

## 📊 Performance Benchmarks

### Expected Performance:
- **PDF Upload:** < 2 seconds
- **Processing Time:** 10-30 seconds per document
- **Chat Response:** 2-5 seconds
- **Citation Accuracy:** >80% relevant results

### File Size Limits:
- **Maximum:** 10MB per PDF
- **Recommended:** < 50 pages for optimal performance
- **Large docs:** May take longer to process

## ✅ Success Criteria

The system is working correctly if:

1. ✅ AI Chat responds to messages
2. ✅ PDF uploads successfully
3. ✅ Documents process without errors
4. ✅ PDF chat returns relevant answers
5. ✅ Citations show correct page numbers
6. ✅ Documents can be deleted
7. ✅ No console errors
8. ✅ Backend logs show successful processing

## 📝 Test Results Log

Use this to track your testing:

```
Date: ___________
Tester: ___________

Test 1 (AI Chat):     [ ] PASS  [ ] FAIL
Test 2 (PDF Upload):  [ ] PASS  [ ] FAIL
Test 3 (Processing):  [ ] PASS  [ ] FAIL
Test 4 (PDF Chat):    [ ] PASS  [ ] FAIL
Test 5 (Citations):   [ ] PASS  [ ] FAIL
Test 6 (Delete):      [ ] PASS  [ ] FAIL

Notes:
_________________________________
_________________________________
_________________________________
```

## 🎯 Quick Verification Test

Run this 60-second test to verify everything works:

1. **Start backend** (Terminal 1): `cd backend/BackendApi && dotnet run`
2. **Start frontend** (Terminal 2): `cd frontend && npm run dev`
3. **Open browser:** http://localhost:5173
4. **Login** to your account
5. **Go to:** AI Models → PDF Chat
6. **Upload** any PDF file
7. **Wait** for "Ready to chat" status
8. **Ask:** "What is this document about?"
9. **Verify:** You get a response with citations

**If all steps complete successfully, the system is working! ✅**

## 🆘 Getting Help

If tests fail:

1. **Check backend logs:** `backend/BackendApi/logs/backend-.log`
2. **Check browser console:** F12 → Console tab
3. **Check network tab:** F12 → Network tab (look for failed requests)
4. **Verify environment variables:** `.env` file in backend
5. **Test API directly:** Use Swagger at http://localhost:5243/swagger

## 📚 Additional Resources

- **API Documentation:** See `RAG_PDF_CHAT_IMPLEMENTATION.md`
- **Setup Guide:** See `PDF_RAG_IMPLEMENTATION_GUIDE.md`
- **Backend Code:** `backend/BackendApi/Controllers/`
- **Frontend Code:** `frontend/src/components/`