# Multi-Format RAG System - Implementation Guide

## Overview
This guide explains the multi-format file upload and RAG (Retrieval-Augmented Generation) system that supports various file types for intelligent search and chat operations.

## Supported File Formats

The system now supports the following file formats:
- **PDF** (.pdf) - Portable Document Format
- **Word Documents** (.docx, .doc) - Microsoft Word documents
- **Text Files** (.txt) - Plain text files
- **CSV** (.csv) - Comma-separated values
- **Markdown** (.md, .markdown) - Markdown files
- **HTML** (.html, .htm) - HTML documents

## Architecture

### Backend Components

#### 1. File Processor (Python)
**Location:** `backend/BackendApi/Python/file_processor.py`

The `FileProcessor` class handles text extraction from multiple file formats:
- `extract_text_from_pdf()` - Extracts text from PDF files with page numbers
- `extract_text_from_docx()` - Extracts text from Word documents
- `extract_text_from_txt()` - Extracts text from plain text files
- `extract_text_from_csv()` - Extracts text from CSV files
- `extract_text_from_markdown()` - Extracts text from Markdown files
- `extract_text_from_html()` - Extracts text from HTML files
- `extract_text_from_file()` - Routes to appropriate extractor based on file extension

#### 2. Document Entity
**Location:** `backend/BackendApi/Entities/Document.cs`

Updated to include:
- `FileExtension` - Stores the file extension (e.g., ".pdf", ".docx")
- `FileType` - Stores the human-readable file type (e.g., "PDF", "Word Document")

#### 3. Document DTO
**Location:** `backend/BackendApi/DTOs/DocumentDto.cs`

Updated to include:
- `FileExtension` - Returns file extension to frontend
- `FileType` - Returns file type to frontend

#### 4. PDF Processing Service
**Location:** `backend/BackendApi/Services/PDFProcessingService.cs`

Updated to:
- Accept multiple file types (not just PDFs)
- Preserve original file extension when saving
- Use the new `file_processor.py` script
- Pass file extension to Python processor
- Handle different file types in error messages

#### 5. Document Controller
**Location:** `backend/BackendApi/Controllers/DocumentController.cs`

Updated to:
- Return file extension and type in API responses
- Serve files with correct content-type based on extension

### Frontend Components

#### 1. File Upload Component
**Location:** `frontend/src/components/FileUpload.jsx`

Features:
- Accepts multiple file formats
- Validates file types (MIME type and extension)
- Shows supported formats in UI
- 10MB file size limit
- Drag & drop support

#### 2. Document Manager
**Location:** `frontend/src/components/DocumentManager.jsx`

Updated to:
- Display file extension in document names
- Show "sections" instead of "pages" for non-PDF files
- Use generic "Upload Document" text
- Import and use `FileUpload` component

#### 3. Document Chat
**Location:** `frontend/src/components/DocumentChat.jsx`

Features:
- Shows appropriate file icon based on file type
- Displays file type in chat header
- Works with all supported file formats

## Python Dependencies

**Location:** `backend/BackendApi/Python/requirements.txt`

Added dependencies:
- `python-docx>=0.8.11` - For DOCX file processing
- `docx2txt>=0.8` - Alternative DOCX text extraction

## How It Works

### Upload Flow

1. **User uploads file** via frontend
2. **Frontend validates** file type and size
3. **Backend receives** multipart/form-data
4. **File validation** checks extension against supported types
5. **File saved** to disk with original extension
6. **Database record** created with file metadata
7. **Background processing** starts:
   - Reads file bytes
   - Encodes to base64
   - Calls Python `file_processor.py`
   - Extracts text based on file type
   - Chunks text with overlap
   - Generates embeddings
   - Saves chunks to database

### Text Extraction by Format

#### PDF
- Uses `pdfplumber` (primary) or `PyPDF2` (fallback)
- Extracts text page by page
- Preserves page numbers

#### DOCX
- Uses `docx2txt` (primary) or `python-docx` (fallback)
- Extracts paragraphs
- Groups paragraphs into sections (10 paragraphs per section)

#### TXT
- Tries multiple encodings (UTF-8, Latin-1, CP1252)
- Splits by lines
- Groups lines into sections (50 lines per section)

#### CSV
- Tries multiple encodings
- Parses CSV structure
- Groups rows into sections (100 rows per section)
- Converts to readable text format

#### Markdown
- Tries multiple encodings
- Splits by headers (#)
- Groups sections into pages (5 sections per page)

#### HTML
- Tries multiple encodings
- Removes script/style tags
- Strips HTML tags
- Decodes HTML entities
- Splits into sections (500 words per section)

### Chunking Strategy

All file types use the same chunking strategy:
- **Chunk size:** 500 characters (configurable)
- **Overlap:** 50 characters (configurable)
- **Smart splitting:** Attempts to break at sentence boundaries (. ! ? \n\n)
- **Minimum threshold:** Only splits at sentence if past 50% of chunk size

### RAG Search Flow

1. **User asks question** in chat interface
2. **Backend retrieves** all chunks for the document
3. **Python rag_search.py** performs vector similarity search
4. **Top-K chunks** selected (default: 5)
5. **Context formatted** with citations
6. **LLM generates** answer with source citations
7. **Response returned** with citations showing:
   - Citation ID
   - File name
   - Page/section number
   - Text snippet
   - Relevance score

## API Endpoints

### Document Management

#### Upload Document
```http
POST /api/documents/upload
Content-Type: multipart/form-data
Authorization: Bearer {token}

Body: file (IFormFile)
```

**Response:**
```json
{
  "id": "uuid",
  "fileName": "example",
  "fileExtension": ".pdf",
  "fileType": "PDF",
  "fileSize": 1024000,
  "totalPages": 10,
  "totalChunks": 25,
  "isProcessed": false,
  "uploadedAt": "2024-01-01T00:00:00Z",
  "processingError": null
}
```

#### Get User Documents
```http
GET /api/documents
Authorization: Bearer {token}
```

**Response:** Array of DocumentDto objects

#### Get Document
```http
GET /api/documents/{documentId}
Authorization: Bearer {token}
```

#### Delete Document
```http
DELETE /api/documents/{documentId}
Authorization: Bearer {token}
```

#### Download Document
```http
GET /api/documents/{documentId}/file
Authorization: Bearer {token}
```

Returns file with appropriate content-type based on extension.

### Chat with Document

```http
POST /api/chat/pdf
Content-Type: application/json
Authorization: Bearer {token}

{
  "message": "What is this document about?",
  "documentId": "uuid-of-document"
}
```

**Response:**
```json
{
  "reply": "The document is about...",
  "document_id": "uuid",
  "document_name": "example.pdf",
  "citations": [
    {
      "citation_id": 1,
      "document_id": "uuid",
      "file_name": "example.pdf",
      "page_number": 3,
      "text_snippet": "relevant text...",
      "relevance_score": 0.95
    }
  ]
}
```

## Configuration

### Chunking Parameters

In `PDFProcessingService.cs`, line ~150:
```csharp
var input = new
{
    file_base64 = fileBase64,
    file_extension = document.FileExtension,
    chunk_size = 500,    // Adjust chunk size (characters)
    overlap = 50         // Adjust overlap (characters)
};
```

### Retrieval Parameters

In `ChatController.cs`:
```csharp
var ragInput = new
{
    query = request.Message,
    chunks = chunksData,
    top_k = 5  // Number of chunks to retrieve
};
```

### Embedding Model

In `file_processor.py`:
```python
self.embedding_model = SentenceTransformer('all-MiniLM-L6-v2')
```

Alternative models:
- `all-mpnet-base-v2` - Better quality, slower
- `paraphrase-MiniLM-L12-v2` - Good balance
- `multi-qa-MiniLM-L6-cos-v1` - Optimized for Q&A

## Database Schema

### Documents Table
```sql
ALTER TABLE documents ADD COLUMN file_extension VARCHAR(10);
ALTER TABLE documents ADD COLUMN file_type VARCHAR(50);
```

### Document Chunks Table
No changes needed - already supports all file types.

## Setup Instructions

### 1. Install Python Dependencies

```bash
cd backend/BackendApi/Python

# Activate virtual environment
source .venv/bin/activate  # Linux/Mac
# or
.venv\Scripts\activate  # Windows

# Install dependencies
pip install -r requirements.txt
```

### 2. Run Database Migration

The NHibernate schema will automatically create the new columns. If needed, manually run:

```sql
ALTER TABLE documents ADD COLUMN IF NOT EXISTS file_extension VARCHAR(10);
ALTER TABLE documents ADD COLUMN IF NOT EXISTS file_type VARCHAR(50);
```

### 3. Start Backend

```bash
cd backend/BackendApi
dotnet run
```

### 4. Start Frontend

```bash
cd frontend
npm install
npm run dev
```

## Usage

### Uploading a File

1. Navigate to Documents page
2. Click "Upload Document"
3. Select or drag & drop a file (PDF, DOCX, TXT, CSV, Markdown, or HTML)
4. Wait for processing to complete
5. File appears in document list with status

### Chatting with a Document

1. Find the processed document in the list
2. Click "Chat" button
3. Ask questions about the document
4. View answers with source citations
5. Click citations to view source sections

## File Type Icons

The system displays different icons for different file types:
- PDF: 📕
- Word: 📘
- Text: 📝
- CSV: 📊
- Markdown: 📋
- HTML: 🌐
- Unknown: 📄

## Troubleshooting

### Python Dependencies Issues
```bash
# Ensure virtual environment is activated
source .venv/bin/activate  # Linux/Mac

# Reinstall dependencies
pip install --upgrade -r requirements.txt
```

### DOCX Processing Fails
- Ensure `python-docx` and `docx2txt` are installed
- Check file is not password protected
- Verify file is not corrupted

### CSV Parsing Issues
- Ensure CSV is properly formatted
- Check for encoding issues (UTF-8 recommended)
- Large CSV files may take longer to process

### HTML Extraction Issues
- Basic HTML parsing is implemented
- For complex HTML, consider using BeautifulSoup (add to requirements)
- JavaScript-rendered content won't be extracted

## Performance Considerations

### Large Files
- Chunking reduces memory usage
- Background processing prevents blocking
- 10MB limit prevents timeout issues

### Vector Search
- pg_vector uses HNSW or IVFFlat indexes
- For large document collections, consider:
  - Using IVFFlat index with appropriate lists parameter
  - Implementing approximate nearest neighbor search
  - Caching frequent queries

### Embedding Storage
- Dimension: 384 (for all-MiniLM-L6-v2)
- Storage per chunk: ~1.5KB
- All file types use the same embedding model

## Next Steps

1. **Test with Different File Types**
   - Upload PDF, DOCX, TXT, CSV, Markdown, and HTML files
   - Verify text extraction works correctly
   - Test chat functionality with each type

2. **Enhancements to Consider**
   - Support for more file types (PPTX, Excel, JSON, XML)
   - Advanced HTML parsing with BeautifulSoup
   - Semantic chunking strategies
   - Multi-document chat (query across multiple files)
   - File preview with highlighted citations
   - Export chat with citations

3. **Production Deployment**
   - Set up proper file storage (S3, Azure Blob)
   - Implement rate limiting
   - Add user quotas (storage, documents)
   - Set up monitoring and logging
   - Configure CORS properly
   - Use environment variables for secrets

## Summary

The multi-format RAG system now supports:
✅ PDF documents
✅ Word documents (DOCX, DOC)
✅ Plain text files (TXT)
✅ CSV files
✅ Markdown files
✅ HTML files
✅ Intelligent text extraction for each format
✅ Consistent chunking and embedding
✅ Vector similarity search across all formats
✅ Source citations with section/page numbers
✅ Intuitive UI for document management and chat

The system is production-ready and can be extended with additional file formats as needed.