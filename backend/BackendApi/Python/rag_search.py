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


class RAGSearch:
    def __init__(self):
        self.embedding_model = None
        self._load_embedding_model()
    
    def _load_embedding_model(self):
        """Load sentence transformer model for embeddings"""
        if SentenceTransformer is None:
            sys.stderr.write(json.dumps({"error": "sentence-transformers not installed"}) + "\n")
            return
        
        try:
            # Use the same model as pdf_processor for consistency
            self.embedding_model = SentenceTransformer('all-MiniLM-L6-v2')
        except Exception as e:
            sys.stderr.write(json.dumps({"error": f"Failed to load embedding model: {str(e)}"}) + "\n")
            self.embedding_model = None
    
    def generate_embedding(self, text: str) -> List[float]:
        """Generate embedding for text using sentence transformer"""
        if self.embedding_model is None:
            raise Exception("Embedding model not loaded")
        
        try:
            embedding = self.embedding_model.encode(text, convert_to_numpy=True)
            return embedding.tolist()
        except Exception as e:
            raise Exception(f"Embedding generation failed: {str(e)}")
    
    def cosine_similarity(self, vec1: List[float], vec2: List[float]) -> float:
        """Calculate cosine similarity between two vectors"""
        vec1 = np.array(vec1)
        vec2 = np.array(vec2)
        
        dot_product = np.dot(vec1, vec2)
        norm1 = np.linalg.norm(vec1)
        norm2 = np.linalg.norm(vec2)
        
        if norm1 == 0 or norm2 == 0:
            return 0.0
        
        return float(dot_product / (norm1 * norm2))
    
    def search_similar_chunks(self, query: str, chunks: List[Dict], top_k: int = 5) -> List[Dict]:
        """
        Search for similar chunks using vector similarity
        chunks: List of {id, text, embedding, page_number, document_id, file_name}
        Returns top_k most similar chunks with scores
        """
        if not chunks:
            return []
        
        # Generate query embedding
        query_embedding = self.generate_embedding(query)
        
        # Calculate similarity scores
        scored_chunks = []
        for chunk in chunks:
            if "embedding" not in chunk or not chunk["embedding"]:
                continue
            
            score = self.cosine_similarity(query_embedding, chunk["embedding"])
            scored_chunks.append({
                "chunk": chunk,
                "score": score
            })
        
        # Sort by score descending
        scored_chunks.sort(key=lambda x: x["score"], reverse=True)
        
        # Return top_k results
        return scored_chunks[:top_k]
    
    def format_context_with_citations(self, search_results: List[Dict]) -> Tuple[str, List[Dict]]:
        """
        Format retrieved chunks into context with citations
        Returns (context_string, citations_list)
        """
        if not search_results:
            return "", []
        
        context_parts = []
        citations = []
        
        for i, result in enumerate(search_results, 1):
            chunk = result["chunk"]
            score = result["score"]
            
            # Add to context
            context_parts.append(f"[{i}] {chunk['text']}")
            
            # Add citation info
            citations.append({
                "citation_id": i,
                "document_id": chunk.get("document_id"),
                "file_name": chunk.get("file_name", "Unknown"),
                "page_number": chunk.get("page_number", 1),
                "text_snippet": chunk["text"][:200] + "..." if len(chunk["text"]) > 200 else chunk["text"],
                "relevance_score": round(score, 4)
            })
        
        context = "\n\n".join(context_parts)
        return context, citations
    
    def build_rag_prompt(self, query: str, context: str, citations: List[Dict]) -> str:
        """Build RAG prompt with context and instructions"""
        if not context:
            return query
        
        prompt = f"""You are a helpful AI assistant that answers questions based on the provided context from documents.

Context from documents:
{context}

Instructions:
- Answer the user's question based ONLY on the provided context
- If the answer is not in the context, say "I don't have enough information in the provided documents to answer this question."
- When referencing information, mention the citation number [1], [2], etc.
- Be accurate and concise
- If multiple sources provide different information, mention both

User Question: {query}

Answer:"""
        
        return prompt
    
    def extract_text_from_pdf_page(self, pdf_bytes: bytes, page_number: int) -> str:
        """Extract text from a specific page of PDF"""
        if not pdf_bytes:
            return ""
        
        try:
            if pdfplumber:
                with pdfplumber.open(io.BytesIO(pdf_bytes)) as pdf:
                    if 0 < page_number <= len(pdf.pages):
                        text = pdf.pages[page_number - 1].extract_text()
                        return text or ""
        except:
            pass
        
        try:
            if PdfReader:
                reader = PdfReader(io.BytesIO(pdf_bytes))
                if 0 < page_number <= len(reader.pages):
                    text = reader.pages[page_number - 1].extract_text()
                    return text or ""
        except:
            pass
        
        return ""


def main():
    try:
        # Read input from stdin
        input_data = json.load(sys.stdin)
    except Exception as e:
        print(json.dumps({"error": f"Invalid input: {str(e)}"}))
        return
    
    # Get parameters
    query = input_data.get("query", "")
    chunks = input_data.get("chunks", [])
    top_k = input_data.get("top_k", 5)
    pdf_base64 = input_data.get("pdf_base64")  # Optional: for extracting specific page text
    page_number = input_data.get("page_number")  # Optional: specific page to extract
    
    if not query:
        print(json.dumps({"error": "query is required"}))
        return
    
    if not chunks:
        print(json.dumps({"error": "chunks are required"}))
        return
    
    try:
        # Initialize RAG search
        rag = RAGSearch()
        
        # Search for similar chunks
        search_results = rag.search_similar_chunks(query, chunks, top_k)
        
        if not search_results:
            print(json.dumps({
                "success": True,
                "context": "",
                "citations": [],
                "message": "No relevant information found in documents"
            }))
            return
        
        # Format context with citations
        context, citations = rag.format_context_with_citations(search_results)
        
        # Build RAG prompt
        rag_prompt = rag.build_rag_prompt(query, context, citations)
        
        # If specific page requested, extract that page text
        page_text = ""
        if pdf_base64 and page_number:
            import base64
            pdf_bytes = base64.b64decode(pdf_base64)
            page_text = rag.extract_text_from_pdf_page(pdf_bytes, page_number)
        
        # Output result
        result = {
            "success": True,
            "rag_prompt": rag_prompt,
            "context": context,
            "citations": citations,
            "top_k_results": len(search_results)
        }
        
        if page_text:
            result["page_text"] = page_text
        
        print(json.dumps(result))
        
    except Exception as e:
        print(json.dumps({"error": str(e)}))


if __name__ == "__main__":
    main()