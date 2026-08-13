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

# DOCX processing
try:
    import docx2txt
except ImportError:
    docx2txt = None

try:
    from docx import Document as DocxDocument
except ImportError:
    DocxDocument = None

# Text processing
import csv
import html

# Embedding model
try:
    from sentence_transformers import SentenceTransformer
except ImportError:
    SentenceTransformer = None


class FileProcessor:
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
    
    def extract_text_from_docx(self, docx_bytes: bytes) -> List[Dict]:
        """
        Extract text from DOCX file
        Returns list of {page_number, text} (paragraphs grouped as pages)
        """
        sections_text = []
        
        # Try docx2txt first (simpler)
        if docx2txt:
            try:
                text = docx2txt.process(io.BytesIO(docx_bytes))
                if text and text.strip():
                    # Split by paragraphs and group them
                    paragraphs = [p.strip() for p in text.split('\n') if p.strip()]
                    if paragraphs:
                        # Group paragraphs into sections (simulating pages)
                        section_size = 10  # paragraphs per section
                        for i in range(0, len(paragraphs), section_size):
                            section_paragraphs = paragraphs[i:i + section_size]
                            sections_text.append({
                                "page_number": (i // section_size) + 1,
                                "text": "\n\n".join(section_paragraphs)
                            })
                return sections_text
            except Exception as e:
                print(f"docx2txt failed: {e}", file=sys.stderr)
        
        # Fallback to python-docx
        if DocxDocument:
            try:
                doc = DocxDocument(io.BytesIO(docx_bytes))
                paragraphs = []
                for para in doc.paragraphs:
                    if para.text.strip():
                        paragraphs.append(para.text.strip())
                
                # Group paragraphs into sections
                section_size = 10
                for i in range(0, len(paragraphs), section_size):
                    section_paragraphs = paragraphs[i:i + section_size]
                    sections_text.append({
                        "page_number": (i // section_size) + 1,
                        "text": "\n\n".join(section_paragraphs)
                    })
                
                return sections_text
            except Exception as e:
                print(f"python-docx failed: {e}", file=sys.stderr)
        
        raise Exception("No DOCX library available or failed to extract text")
    
    def extract_text_from_txt(self, txt_bytes: bytes) -> List[Dict]:
        """
        Extract text from plain text file
        Returns list of {page_number, text} (lines grouped as pages)
        """
        sections_text = []
        
        try:
            # Try different encodings
            text = None
            for encoding in ['utf-8', 'latin-1', 'cp1252']:
                try:
                    text = txt_bytes.decode(encoding)
                    break
                except UnicodeDecodeError:
                    continue
            
            if text is None:
                raise Exception("Unable to decode text file")
            
            # Split by lines and group them
            lines = [line.strip() for line in text.split('\n') if line.strip()]
            if lines:
                # Group lines into sections (simulating pages)
                section_size = 50  # lines per section
                for i in range(0, len(lines), section_size):
                    section_lines = lines[i:i + section_size]
                    sections_text.append({
                        "page_number": (i // section_size) + 1,
                        "text": "\n".join(section_lines)
                    })
            
            return sections_text
        except Exception as e:
            raise Exception(f"Failed to extract text from TXT: {str(e)}")
    
    def extract_text_from_csv(self, csv_bytes: bytes) -> List[Dict]:
        """
        Extract text from CSV file
        Returns list of {page_number, text} (rows grouped as pages)
        """
        sections_text = []
        
        try:
            # Try different encodings
            text = None
            for encoding in ['utf-8', 'latin-1', 'cp1252']:
                try:
                    text = csv_bytes.decode(encoding)
                    break
                except UnicodeDecodeError:
                    continue
            
            if text is None:
                raise Exception("Unable to decode CSV file")
            
            # Parse CSV
            csv_reader = csv.reader(io.StringIO(text))
            rows = list(csv_reader)
            
            if rows:
                # Group rows into sections
                section_size = 100  # rows per section
                for i in range(0, len(rows), section_size):
                    section_rows = rows[i:i + section_size]
                    # Convert rows to readable text
                    section_text = "\n".join([", ".join(row) for row in section_rows])
                    sections_text.append({
                        "page_number": (i // section_size) + 1,
                        "text": section_text
                    })
            
            return sections_text
        except Exception as e:
            raise Exception(f"Failed to extract text from CSV: {str(e)}")
    
    def extract_text_from_markdown(self, md_bytes: bytes) -> List[Dict]:
        """
        Extract text from Markdown file
        Returns list of {page_number, text}
        """
        sections_text = []
        
        try:
            # Try different encodings
            text = None
            for encoding in ['utf-8', 'latin-1', 'cp1252']:
                try:
                    text = md_bytes.decode(encoding)
                    break
                except UnicodeDecodeError:
                    continue
            
            if text is None:
                raise Exception("Unable to decode Markdown file")
            
            # Split by headers or double newlines
            sections = []
            current_section = []
            
            for line in text.split('\n'):
                if line.strip().startswith('#') and current_section:
                    sections.append("\n".join(current_section))
                    current_section = [line]
                else:
                    current_section.append(line)
            
            if current_section:
                sections.append("\n".join(current_section))
            
            # Group sections into pages
            section_size = 5  # sections per page
            for i in range(0, len(sections), section_size):
                page_sections = sections[i:i + section_size]
                sections_text.append({
                    "page_number": (i // section_size) + 1,
                    "text": "\n\n".join(page_sections)
                })
            
            return sections_text
        except Exception as e:
            raise Exception(f"Failed to extract text from Markdown: {str(e)}")
    
    def extract_text_from_html(self, html_bytes: bytes) -> List[Dict]:
        """
        Extract text from HTML file
        Returns list of {page_number, text}
        """
        sections_text = []
        
        try:
            # Try different encodings
            text = None
            for encoding in ['utf-8', 'latin-1', 'cp1252']:
                try:
                    text = html_bytes.decode(encoding)
                    break
                except UnicodeDecodeError:
                    continue
            
            if text is None:
                raise Exception("Unable to decode HTML file")
            
            # Simple HTML text extraction (remove tags)
            # This is a basic implementation - for production, use BeautifulSoup
            import re
            # Remove script and style content
            text = re.sub(r'<script[^>]*>.*?</script>', '', text, flags=re.DOTALL | re.IGNORECASE)
            text = re.sub(r'<style[^>]*>.*?</style>', '', text, flags=re.DOTALL | re.IGNORECASE)
            # Remove HTML tags
            text = re.sub(r'<[^>]+>', ' ', text)
            # Decode HTML entities
            text = html.unescape(text)
            # Clean up whitespace
            text = re.sub(r'\s+', ' ', text).strip()
            
            # Split into sections
            words = text.split()
            section_size = 500  # words per section
            
            for i in range(0, len(words), section_size):
                section_words = words[i:i + section_size]
                sections_text.append({
                    "page_number": (i // section_size) + 1,
                    "text": " ".join(section_words)
                })
            
            return sections_text
        except Exception as e:
            raise Exception(f"Failed to extract text from HTML: {str(e)}")
    
    def extract_text_from_file(self, file_bytes: bytes, file_extension: str) -> List[Dict]:
        """
        Extract text from file based on extension
        Returns list of {page_number, text}
        """
        ext = file_extension.lower()
        
        if ext == '.pdf':
            return self.extract_text_from_pdf(file_bytes)
        elif ext in ['.docx', '.doc']:
            return self.extract_text_from_docx(file_bytes)
        elif ext == '.txt':
            return self.extract_text_from_txt(file_bytes)
        elif ext == '.csv':
            return self.extract_text_from_csv(file_bytes)
        elif ext in ['.md', '.markdown']:
            return self.extract_text_from_markdown(file_bytes)
        elif ext in ['.html', '.htm']:
            return self.extract_text_from_html(file_bytes)
        else:
            raise Exception(f"Unsupported file format: {ext}")
    
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
    
    def process_file(self, file_bytes: bytes, file_extension: str, chunk_size: int = 500, overlap: int = 50) -> Dict:
        """
        Main processing function
        Returns document with chunks and embeddings
        """
        if self.embedding_model is None:
            return {"error": "Embedding model not available"}
        
        try:
            # Extract text from file
            sections_text = self.extract_text_from_file(file_bytes, file_extension)
            
            if not sections_text:
                return {"error": "No text extracted from file"}
            
            # Process each section
            all_chunks = []
            chunk_id = 0
            
            for section_info in sections_text:
                page_num = section_info["page_number"]
                text = section_info["text"]
                
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
                "total_pages": len(sections_text),
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
    file_base64 = input_data.get("file_base64")
    file_extension = input_data.get("file_extension", ".pdf")
    chunk_size = input_data.get("chunk_size", 500)
    overlap = input_data.get("overlap", 50)
    
    if not file_base64:
        print(json.dumps({"error": "file_base64 is required"}))
        sys.exit(1)
    
    try:
        # Decode file
        import base64
        file_bytes = base64.b64decode(file_base64)
        
        # Process file
        processor = FileProcessor()
        result = processor.process_file(file_bytes, file_extension, chunk_size, overlap)
        
        # Output result
        print(json.dumps(result))
        sys.exit(0)
        
    except Exception as e:
        print(json.dumps({"error": str(e)}))
        sys.exit(1)


if __name__ == "__main__":
    main()