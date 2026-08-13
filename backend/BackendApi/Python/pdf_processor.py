import json
import sys
import os
import io
from typing import List, Dict, Tuple
import numpy as np

# PDF processing
try:
    import pdfplumber
except ImportError:
    pdfplumber = None

try:
    from PyPDF2 import PdfReader
except ImportError:
    PdfReader = None

# Embedding model
try:
    from sentence_transformers import SentenceTransformer
except ImportError:
    SentenceTransformer = None


class PDFProcessor:
    def __init__(self):
        self.embedding_model = None
        self._load_embedding_model()
    
    def _load_embedding_model(self):
        """Load sentence transformer model for embeddings"""
        if SentenceTransformer is None:
            sys.stderr.write(json.dumps({"error": "sentence-transformers not installed"}) + "\n")
            return
        
        try:
            # Use a lightweight, fast model good for semantic search
            self.embedding_model = SentenceTransformer('all-MiniLM-L6-v2')
        except Exception as e:
            sys.stderr.write(json.dumps({"error": f"Failed to load embedding model: {str(e)}"}) + "\n")
            self.embedding_model = None
    
    def extract_text_from_pdf(self, pdf_bytes: bytes) -> List[Dict]:
        """
        Extract text from PDF with page numbers
        Returns list of {page_number, text}
        """
        pages_text = []
        
        # Try pdfplumber first (better text extraction)
        if pdfplumber:
            try:
                with pdfplumber.open(io.BytesIO(pdf_bytes)) as pdf:
                    for i, page in enumerate(pdf.pages, 1):
                        text = page.extract_text()
                        if text and text.strip():
                            pages_text.append({
                                "page_number": i,
                                "text": text.strip()
                            })
                return pages_text
            except Exception as e:
                print(f"pdfplumber failed: {e}", file=sys.stderr)
        
        # Fallback to PyPDF2
        if PdfReader:
            try:
                reader = PdfReader(io.BytesIO(pdf_bytes))
                for i, page in enumerate(reader.pages, 1):
                    text = page.extract_text()
                    if text and text.strip():
                        pages_text.append({
                            "page_number": i,
                            "text": text.strip()
                        })
                return pages_text
            except Exception as e:
                print(f"PyPDF2 failed: {e}", file=sys.stderr)
        
        raise Exception("No PDF library available or failed to extract text")
    
    def chunk_text(self, text: str, chunk_size: int = 500, overlap: int = 50) -> List[str]:
        """
        Split text into chunks with overlap
        chunk_size: target chunk size in characters
        overlap: number of characters to overlap between chunks
        """
        if len(text) <= chunk_size:
            return [text]
        
        chunks = []
        start = 0
        
        while start < len(text):
            end = start + chunk_size
            
            # Try to break at sentence boundary
            if end < len(text):
                # Look for sentence endings
                for punct in ['. ', '! ', '? ', '\n\n']:
                    last_punct = text[start:end].rfind(punct)
                    if last_punct > chunk_size * 0.5:  # Only if we're past halfway
                        end = start + last_punct + len(punct)
                        break
            
            chunk = text[start:end].strip()
            if chunk:
                chunks.append(chunk)
            
            # Move start position with overlap
            start = end - overlap if end < len(text) else end
        
        return chunks
    
    def generate_embedding(self, text: str) -> List[float]:
        """Generate embedding for text using sentence transformer"""
        if self.embedding_model is None:
            raise Exception("Embedding model not loaded")
        
        try:
            embedding = self.embedding_model.encode(text, convert_to_numpy=True)
            return embedding.tolist()
        except Exception as e:
            raise Exception(f"Embedding generation failed: {str(e)}")
    
    def process_pdf(self, pdf_bytes: bytes, chunk_size: int = 500, overlap: int = 50) -> Dict:
        """
        Main processing function
        Returns document with chunks and embeddings
        """
        if self.embedding_model is None:
            return {"error": "Embedding model not available"}
        
        try:
            # Extract text from PDF
            pages_text = self.extract_text_from_pdf(pdf_bytes)
            
            if not pages_text:
                return {"error": "No text extracted from PDF"}
            
            # Process each page
            all_chunks = []
            chunk_id = 0
            
            for page_info in pages_text:
                page_num = page_info["page_number"]
                text = page_info["text"]
                
                # Chunk the text
                chunks = self.chunk_text(text, chunk_size, overlap)
                
                for chunk_text in chunks:
                    # Generate embedding
                    embedding = self.generate_embedding(chunk_text)
                    
                    all_chunks.append({
                        "chunk_id": chunk_id,
                        "page_number": page_num,
                        "text": chunk_text,
                        "embedding": embedding
                    })
                    chunk_id += 1
            
            return {
                "success": True,
                "total_pages": len(pages_text),
                "total_chunks": len(all_chunks),
                "chunks": all_chunks
            }
            
        except Exception as e:
            return {"error": str(e)}


def main():
    try:
        # Read input from stdin FIRST (before any heavy initialization)
        input_data = json.load(sys.stdin)
    except Exception as e:
        print(json.dumps({"error": f"Invalid input: {str(e)}"}))
        sys.exit(1)
    
    # Get parameters
    pdf_base64 = input_data.get("pdf_base64")
    chunk_size = input_data.get("chunk_size", 500)
    overlap = input_data.get("overlap", 50)
    
    if not pdf_base64:
        print(json.dumps({"error": "pdf_base64 is required"}))
        sys.exit(1)
    
    try:
        # Decode PDF
        import base64
        pdf_bytes = base64.b64decode(pdf_base64)
        
        # Process PDF
        processor = PDFProcessor()
        result = processor.process_pdf(pdf_bytes, chunk_size, overlap)
        
        # Output result
        print(json.dumps(result))
        sys.exit(0)
        
    except Exception as e:
        print(json.dumps({"error": str(e)}))
        sys.exit(1)


if __name__ == "__main__":
    main()