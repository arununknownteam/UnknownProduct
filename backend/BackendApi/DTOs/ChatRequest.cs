namespace BackendApi.DTOs
{
    public class ChatRequest
    {
        public string Message { get; set; }

        public System.Collections.Generic.List<ChatMessage> Messages { get; set; }
    }
}
