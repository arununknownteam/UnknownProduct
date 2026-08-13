#!/usr/bin/env python3
"""
Test script to diagnose PDF processing issues
"""
import json
import sys
import os

print("=" * 60)
print("Testing PDF Processor Dependencies")
print("=" * 60)

# Test 1: Check Python version
print(f"\n1. Python Version: {sys.version}")

# Test 2: Check required libraries
print("\n2. Checking required libraries...")

libraries = {
    'pdfplumber': 'PDF text extraction',
    'PyPDF2': 'PDF text extraction (fallback)',
    'sentence_transformers': 'Embedding generation',
    'numpy': 'Numerical operations'
}

for lib, description in libraries.items():
    try:
        if lib == 'pdfplumber':
            import pdfplumber
            print(f"   ✓ {lib:20s} - {description}")
        elif lib == 'PyPDF2':
            from PyPDF2 import PdfReader
            print(f"   ✓ {lib:20s} - {description}")
        elif lib == 'sentence_transformers':
            from sentence_transformers import SentenceTransformer
            print(f"   ✓ {lib:20s} - {description}")
        elif lib == 'numpy':
            import numpy as np
            print(f"   ✓ {lib:20s} - {description}")
    except ImportError as e:
        print(f"   ✗ {lib:20s} - MISSING: {e}")

# Test 3: Try loading the embedding model
print("\n3. Testing embedding model loading...")
try:
    from sentence_transformers import SentenceTransformer
    print("   Loading model 'all-MiniLM-L6-v2' (this may take a minute on first run)...")
    model = SentenceTransformer('all-MiniLM-L6-v2')
    print("   ✓ Model loaded successfully")
    
    # Test embedding generation
    test_text = "This is a test sentence"
    embedding = model.encode(test_text, convert_to_numpy=True)
    print(f"   ✓ Embedding generated: shape {embedding.shape}")
except Exception as e:
    print(f"   ✗ Failed to load model: {e}")

# Test 4: Test PDF processing with a simple PDF
print("\n4. Testing PDF processing...")
try:
    import pdfplumber
    import io
    
    # Create a simple test PDF in memory
    print("   Creating test PDF...")
    
    # Try to import and use the actual processor
    sys.path.insert(0, os.path.dirname(__file__))
    from pdf_processor import PDFProcessor
    
    print("   Initializing PDFProcessor...")
    processor = PDFProcessor()
    
    if processor.embedding_model is None:
        print("   ✗ Embedding model not loaded in PDFProcessor")
    else:
        print("   ✓ PDFProcessor initialized with embedding model")
    
except Exception as e:
    print(f"   ✗ PDF processing test failed: {e}")
    import traceback
    traceback.print_exc()

print("\n" + "=" * 60)
print("Test Complete")
print("=" * 60)