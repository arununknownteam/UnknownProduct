import { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import FileUpload from './FileUpload';
import DocumentChat from './DocumentChat';
import '../styles/pdf-chat.css';

export default function DocumentManager({ token }) {
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selectedDocument, setSelectedDocument] = useState(null);
  const [viewMode, setViewMode] = useState('list'); // 'list', 'upload', 'chat'
  const pollingIntervals = useRef(new Map());

  useEffect(() => {
    fetchDocuments();
  }, []);

  const fetchDocuments = async () => {
    try {
      const response = await axios.get(
        'http://localhost:5243/api/documents',
        {
          headers: {
            'Authorization': `Bearer ${token}`
          }
        }
      );
      setDocuments(response.data);
    } catch (err) {
      setError('Failed to fetch documents');
    } finally {
      setLoading(false);
    }
  };

  const handleUploadSuccess = (document) => {
    setDocuments(prev => [document, ...prev]);
    setViewMode('list');
    // Poll for processing completion
    pollDocumentStatus(document.id);
  };

  const pollDocumentStatus = async (documentId) => {
    const maxAttempts = 20;
    let attempts = 0;

    const interval = setInterval(async () => {
      try {
        const response = await axios.get(
          `http://localhost:5243/api/documents/${documentId}`,
          {
            headers: {
              'Authorization': `Bearer ${token}`
            }
          }
        );

        setDocuments(prev => prev.map(doc => 
          doc.id === documentId ? response.data : doc
        ));

        if (response.data.is_processed || attempts >= maxAttempts) {
          clearInterval(interval);
          pollingIntervals.current.delete(documentId);
        }
      } catch (err) {
        clearInterval(interval);
        pollingIntervals.current.delete(documentId);
      }

      attempts++;
    }, 2000); // Poll every 2 seconds

    pollingIntervals.current.set(documentId, interval);
  };

  const handleDeleteDocument = async (documentId) => {
    if (!window.confirm('Are you sure you want to delete this document?')) {
      return;
    }

    try {
      // Clear any active polling for this document
      if (pollingIntervals.current.has(documentId)) {
        clearInterval(pollingIntervals.current.get(documentId));
        pollingIntervals.current.delete(documentId);
      }

      await axios.delete(
        `http://localhost:5243/api/documents/${documentId}`,
        {
          headers: {
            'Authorization': `Bearer ${token}`
          }
        }
      );

      setDocuments(prev => prev.filter(doc => doc.id !== documentId));
      if (selectedDocument?.id === documentId) {
        setSelectedDocument(null);
        setViewMode('list');
      }
    } catch (err) {
      setError(err.response?.data?.error || 'Failed to delete document');
    }
  };

  const handleViewDocument = (document) => {
    setSelectedDocument(document);
    setViewMode('chat');
  };

  const handleViewPage = (pageNumber) => {
    // Open PDF in new tab at specific page
    if (selectedDocument) {
      const pdfUrl = `http://localhost:5243/api/documents/${selectedDocument.id}/file`;
      window.open(pdfUrl, '_blank');
    }
  };

  const formatFileSize = (bytes) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  };

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  if (viewMode === 'upload') {
    return (
      <div className="document-manager">
        <div className="document-header">
          <button 
            className="back-button"
            onClick={() => setViewMode('list')}
          >
            ← Back to Documents
          </button>
          <h2>Upload Document</h2>
        </div>
        <FileUpload 
          onUploadSuccess={handleUploadSuccess}
          token={token}
        />
      </div>
    );
  }

  if (viewMode === 'chat' && selectedDocument) {
    return (
      <div className="document-manager">
        <div className="document-header">
          <button 
            className="back-button"
            onClick={() => {
              setViewMode('list');
              setSelectedDocument(null);
            }}
          >
            ← Back to Documents
          </button>
          <h2>PDF Chat</h2>
        </div>
        <DocumentChat 
          document={selectedDocument}
          token={token}
          onViewPage={handleViewPage}
        />
      </div>
    );
  }

  return (
    <div className="document-manager">
      <div className="document-header">
          <h2>📚 My Documents</h2>
        <button 
          className="upload-button"
          onClick={() => setViewMode('upload')}
        >
          + Upload Document
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

        {loading ? (
          <div className="loading">Loading documents...</div>
        ) : documents.length === 0 ? (
          <div className="empty-state">
            <p>📄 No documents uploaded yet</p>
            <p>Upload a document to start chatting with it</p>
          </div>
      ) : (
        <div className="documents-list">
          {documents.map(doc => (
            <div key={doc.id} className="document-card">
              <div className="document-info">
                <h3>{doc.fileName}{doc.fileExtension}</h3>
                <div className="document-meta">
                  <span>📊 {formatFileSize(doc.fileSize)}</span>
                  <span>📄 {doc.totalPages} sections</span>
                  <span>🔢 {doc.totalChunks} chunks</span>
                  <span>🕒 {formatDate(doc.uploadedAt)}</span>
                </div>
                
                {doc.processingError && (
                  <div className="processing-error">
                    ⚠️ Error: {doc.processingError}
                  </div>
                )}

                {!doc.isProcessed && !doc.processingError && (
                  <div className="processing-status">
                    ⏳ Processing...
                  </div>
                )}

                {doc.isProcessed && (
                  <div className="processing-success">
                    ✅ Ready to chat
                  </div>
                )}
              </div>

              <div className="document-actions">
                {doc.isProcessed && (
                  <button
                    className="chat-button"
                    onClick={() => handleViewDocument(doc)}
                  >
                    💬 Chat
                  </button>
                )}
                
                <button
                  className="delete-button"
                  onClick={() => handleDeleteDocument(doc.id)}
                >
                  🗑 Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}