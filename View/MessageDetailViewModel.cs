using System.Collections.ObjectModel;
using System.Windows.Media;
using FixSender5.Models;

namespace FixSender5.View;

public class MessageDetailViewModel
{
    public DateTime Timestamp { get; }
    public MessageDirection Direction { get; }
    public string MessageType { get; }
    public string Description { get; }
    public string RawMessage { get; }
    public string FormattedMessage { get; }
    public ObservableCollection<FixField> ParsedFields { get; }
    public Brush DirectionColor => Direction == MessageDirection.Incoming 
        ? new SolidColorBrush(Color.FromRgb(40, 167, 69))   // Verde para Incoming
        : new SolidColorBrush(Color.FromRgb(220, 53, 69));  // Vermelho para Outgoing

    public MessageDetailViewModel(FixMessage message)
    {
        Timestamp = message.Timestamp;
        Direction = message.Direction;
        MessageType = message.MessageType;
        Description = message.Description;
        RawMessage = message.RawMessage;
        
        ParsedFields = [];
        FormattedMessage = FormatMessage(message.RawMessage);
        ParseFields(message.RawMessage);
    }

    private static string FormatMessage(string rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
            return "No message content";

        var fields = rawMessage.Split('\x01', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(Environment.NewLine, 
            fields.Select((field, index) => $"{index + 1,2}: {field}"));
    }

    private void ParseFields(string rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
            return;

        var fields = rawMessage.Split('\x01', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var field in fields)
        {
            var parts = field.Split('=', 2);
            if (parts.Length != 2) continue;
            
            var tag = parts[0];
            var value = parts[1];
            var fieldInfo = GetFieldInfo(tag);
                
            ParsedFields.Add(new FixField
            {
                Tag = tag,
                Name = fieldInfo.Name,
                Value = value,
                Description = fieldInfo.Description
            });
        }
    }

    private static (string Name, string Description) GetFieldInfo(string tag)
    {
        return tag switch
        {
            "8" => ("BeginString", "FIX Protocol version"),
            "9" => ("BodyLength", "Message length"),
            "35" => ("MsgType", "Message type"),
            "49" => ("SenderCompID", "Sender company ID"),
            "56" => ("TargetCompID", "Target company ID"),
            "34" => ("MsgSeqNum", "Message sequence number"),
            "52" => ("SendingTime", "Time of message transmission"),
            "10" => ("CheckSum", "Three character checksum"),
            "58" => ("Text", "Free format text string"),
            "37" => ("OrderID", "Unique order identifier"),
            "11" => ("ClOrdID", "Client order ID"),
            "54" => ("Side", "Side of order (1=Buy, 2=Sell)"),
            "38" => ("OrderQty", "Number of shares ordered"),
            "44" => ("Price", "Price per share"),
            "55" => ("Symbol", "Ticker symbol"),
            "40" => ("OrdType", "Order type"),
            "59" => ("TimeInForce", "Time in force"),
            "6" => ("AvgPx", "Average execution price"),
            "14" => ("CumQty", "Total number of shares filled"),
            "39" => ("OrdStatus", "Order status"),
            "150" => ("ExecType", "Execution type"),
            "17" => ("ExecID", "Execution ID"),
            "31" => ("LastPx", "Price of last fill"),
            "32" => ("LastQty", "Number of shares in last fill"),
            "151" => ("LeavesQty", "Amount of shares open for further execution"),
            _ => ($"Tag{tag}", "Unknown field")
        };
    }
}