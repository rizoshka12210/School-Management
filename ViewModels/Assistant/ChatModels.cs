namespace SchoolManagementSystem.Web.ViewModels.Assistant;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;

    public List<ChatHistoryItem> History { get; set; } = new();
}

public class ChatHistoryItem
{
    public string Role { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}

public class ChatResponse
{
    public bool Success { get; set; }

    public string Reply { get; set; } = string.Empty;
}
