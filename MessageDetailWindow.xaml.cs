using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace FixSender5
{
    public partial class MessageDetailWindow : Window
    {
        public MessageDetailViewModel ViewModel { get; }

        public MessageDetailWindow(FixMessage message)
        {
            InitializeComponent();
            ViewModel = new MessageDetailViewModel(message);
            DataContext = ViewModel;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyFields(object sender, RoutedEventArgs e)
        {
            try
            {
                var fieldsText = string.Join(Environment.NewLine, 
                    ViewModel.ParsedFields.Select(f => $"{f.Tag}={f.Value} ({f.Name})"));
                Clipboard.SetText(fieldsText);
                MessageBox.Show("Fields copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying fields: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyRawMessage(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ViewModel.RawMessage);
                MessageBox.Show("Raw message copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyFormattedMessage(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ViewModel.FormattedMessage);
                MessageBox.Show("Formatted message copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportMessage(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = $"FIX_Message_{ViewModel.Timestamp:yyyyMMdd_HHmmss}.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var content = $"FIX Message Export\n" +
                                 $"==================\n" +
                                 $"Timestamp: {ViewModel.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\n" +
                                 $"Direction: {ViewModel.Direction}\n" +
                                 $"Message Type: {ViewModel.MessageType}\n" +
                                 $"Description: {ViewModel.Description}\n\n" +
                                 $"Raw Message:\n{ViewModel.RawMessage}\n\n" +
                                 $"Formatted Message:\n{ViewModel.FormattedMessage}\n\n" +
                                 $"Parsed Fields:\n" +
                                 string.Join("\n", ViewModel.ParsedFields.Select(f => 
                                     $"{f.Tag,3} | {f.Name,-20} | {f.Value} | {f.Description}"));

                    File.WriteAllText(saveFileDialog.FileName, content);
                    MessageBox.Show("Message exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // ViewModel para a janela de detalhes
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
            
            ParsedFields = new ObservableCollection<FixField>();
            FormattedMessage = FormatMessage(message.RawMessage);
            ParseFields(message.RawMessage);
        }

        private string FormatMessage(string rawMessage)
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
                if (parts.Length == 2)
                {
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
        }

        private (string Name, string Description) GetFieldInfo(string tag)
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

    // Classe para representar um campo FIX
    public class FixField
    {
        public string Tag { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}