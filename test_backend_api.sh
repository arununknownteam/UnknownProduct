#!/bin/bash

echo "======================================"
echo "Testing Backend API Endpoints"
echo "======================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Base URL
BASE_URL="http://localhost:5243"

# Test 1: Health Check
echo "1. Testing Health Endpoint..."
HEALTH_RESPONSE=$(curl -s $BASE_URL/api/health)
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Backend is running${NC}"
    echo "  Response: $HEALTH_RESPONSE"
else
    echo -e "${RED}✗ Backend is not running${NC}"
    echo "  Please start the backend with: cd backend/BackendApi && dotnet run"
    exit 1
fi
echo ""

# Test 2: Check Python
echo "2. Testing Python Installation..."
PYTHON_RESPONSE=$(curl -s $BASE_URL/api/health/python)
echo "  Response: $PYTHON_RESPONSE"
if echo $PYTHON_RESPONSE | grep -q "ok"; then
    echo -e "${GREEN}✓ Python is working${NC}"
else
    echo -e "${RED}✗ Python check failed${NC}"
fi
echo ""

# Test 3: Check Groq API Key
echo "3. Testing Groq API Key..."
GROQ_RESPONSE=$(curl -s $BASE_URL/api/health/groq)
echo "  Response: $GROQ_RESPONSE"
if echo $GROQ_RESPONSE | grep -q "ok"; then
    echo -e "${GREEN}✓ Groq API key is configured${NC}"
else
    echo -e "${RED}✗ Groq API key check failed${NC}"
    echo "  Make sure GROQ_API_KEY is set in backend/BackendApi/.env"
fi
echo ""

echo "======================================"
echo "Health checks complete!"
echo "======================================"
echo ""
echo "Next steps:"
echo "1. Login via the frontend to get a token"
echo "2. Test AI Chat at: http://localhost:5173 (AI Models → AI Chat)"
echo "3. Test PDF Upload at: http://localhost:5173 (AI Models → PDF Chat)"
echo ""
echo "To test APIs manually with curl:"
echo "1. Login: curl -X POST $BASE_URL/api/auth/login -H 'Content-Type: application/json' -d '{\"email\":\"your-email\",\"password\":\"your-password\"}'"
echo "2. Chat: curl -X POST $BASE_URL/api/chat -H 'Authorization: Bearer YOUR_TOKEN' -H 'Content-Type: application/json' -d '{\"message\":\"Hello\"}'"
echo ""