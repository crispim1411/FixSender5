namespace FixSender5.Models;

public class FixMessage
{
    public DateTime Timestamp { get; set; }
    public MessageDirection Direction { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RawMessage { get; set; } = string.Empty;
}