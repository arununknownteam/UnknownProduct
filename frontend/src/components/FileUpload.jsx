import { useState, useCallback } from 'react';
import axios from 'axios';

export default function FileUpload({ onUploadSuccess, token }) {
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const [dragActive, setDragActive] = useState(false);

  const handleDrag = useCallback((e) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true);
    } else if (e.type === "dragleave") {
      setDragActive(false);
    }
  }, []);

  const handleDrop = useCallback(async (e) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      await uploadFile(e.dataTransfer.files[0]);
    }
  }, [token]);

  const handleChange = useCallback(async (e) => {
    e.preventDefault();
    if (e.target.files && e.target.files[0]) {
      await uploadFile(e.target.files[0]);
    }
  }, [token]);

  const uploadFile = async (file) => {
    const supportedTypes = [
      'application/pdf',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'application/msword',
      'text/plain',
      'text/csv',
      'text/markdown',
      'text/html'
    ];
    
    const supportedExtensions = ['.pdf', '.docx', '.doc', '.txt', '.csv', '.md', '.markdown', '.html', '.htm'];
    const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
    
    if (!supportedTypes.includes(file.type) && !supportedExtensions.includes(fileExtension)) {
      setError('Unsupported file format. Please upload: PDF, DOCX, TXT, CSV, Markdown, or HTML files');
      return;
    }

    if (file.size > 10 * 1024 * 1024) { // 10MB limit
      setError('File size must be less than 10MB');
      return;
    }

    setUploading(true);
    setError('');

    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await axios.post(
        'http://localhost:5243/api/documents/upload',
        formData,
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'multipart/form-data'
          }
        }
      );

      onUploadSuccess(response.data);
    } catch (err) {
      setError(err.response?.data?.error || 'Failed to upload document');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="pdf-upload">
      <div
        className={`upload-zone ${dragActive ? 'drag-active' : ''} ${uploading ? 'uploading' : ''}`}
        onDragEnter={handleDrag}
        onDragLeave={handleDrag}
        onDragOver={handleDrag}
        onDrop={handleDrop}
      >
        <input
          type="file"
          id="file-upload"
          accept=".pdf,.docx,.doc,.txt,.csv,.md,.markdown,.html,.htm"
          onChange={handleChange}
          style={{ display: 'none' }}
        />
        
        <label htmlFor="file-upload" className="upload-label">
          {uploading ? (
            <>
              <div className="spinner"></div>
              <p>Processing document...</p>
            </>
          ) : (
            <>
              <div className="upload-icon">📁</div>
              <p>Drop your file here or click to browse</p>
              <p className="upload-hint">Supported: PDF, DOCX, TXT, CSV, Markdown, HTML (Max 10MB)</p>
            </>
          )}
        </label>
      </div>

      {error && <div className="error-message">{error}</div>}
    </div>
  );
}