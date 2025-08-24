using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FixSender5.FixEngine;
using NLog;
using QuickFix;

namespace FixSender5;

public enum ConnectionType
{
    Acceptor,
    Initiator
}

public enum MessageDirection
{
    Incoming,
    Outgoing
}

public class FixMessage
{
    public DateTime Timestamp { get; set; }
    public MessageDirection Direction { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RawMessage { get; set; } = string.Empty;
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : INotifyPropertyChanged
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly CancellationTokenSource _cts = new();
    
    private Acceptor? _acceptor;
    private Initiator? _initiator; 
    public ObservableCollection<FixMessage> Messages { get; } = [];
    
    #region Properties

    private string _host = "127.0.0.1";
    public string Host
    {
        get => _host;
        set
        {
            if (value == _host) return;
            _host = value;
            OnPropertyChanged(nameof(CanConnect));
        }
    }
    
    private int _port = 9878;
    public int Port
    {
        get => _port;
        set
        {
            if (value == _port) return;
            _port = value;
            OnPropertyChanged(nameof(CanConnect));
        }
    }
    private string _sender = "";
    public string Sender
    {
        get => _sender;
        set
        {
            if (value == _sender) return;
            _sender = value;
            OnPropertyChanged(nameof(CanConnect));
        }
    }
    
    private string _target = "";
    public string Target
    {
        get => _target;
        set
        {
            if (value == _target) return;
            _target = value;
            OnPropertyChanged(nameof(CanConnect));
        }
    }
    
    private ConnectionType _connectionType = ConnectionType.Initiator;
    public ConnectionType ConnectionType
    {
        get => _connectionType;
        set
        {
            if (Equals(value, _connectionType)) return;
            _connectionType = value;
        }
    }
    
    private bool _isConnected = false;
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (value == _isConnected) return;
            _isConnected = value;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CanConnect));
            OnPropertyChanged(nameof(CanSendMessage));
        }
    }
    
    private FixMessage? _selectedMessage;
    public FixMessage? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            if (value == _selectedMessage) return;
            _selectedMessage = value;
            OnPropertyChanged(nameof(SelectedMessage));
        }
    }
    
    private string _messageToSend = string.Empty;
    public string MessageToSend
    {
        get => _messageToSend;
        set
        {
            if (value == _messageToSend) return;
            _messageToSend = value;
            OnPropertyChanged(nameof(EstimatedFieldCount));
            OnPropertyChanged(nameof(MessageValidationText));
            OnPropertyChanged(nameof(ValidationColor));
            OnPropertyChanged(nameof(CanSendMessage));
        }
    }
    
    private bool _isSending = false;
    public bool IsSending
    {
        get => _isSending;
        private set
        {
            if (value == _isSending) return;
            _isSending = value;
            OnPropertyChanged(nameof(CanSendMessage));
        }
    }
    
    public int EstimatedFieldCount => string.IsNullOrEmpty(MessageToSend) 
        ? 0 
        : MessageToSend.Split(["\x01", "^"], StringSplitOptions.RemoveEmptyEntries).Length;

    public string MessageValidationText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(MessageToSend))
                return "";
            
            if (EstimatedFieldCount < 3)
                return "⚠ Message seems incomplete";
            
            if (MessageToSend.Contains("35="))
                return "✓ Valid FIX structure detected";
            
            return "⚠ No MsgType (35) found";
        }
    }

    public Brush ValidationColor
    {
        get
        {
            if (string.IsNullOrWhiteSpace(MessageToSend))
                return new SolidColorBrush(Colors.Gray);
            
            if (MessageValidationText.StartsWith($"✓"))
                return new SolidColorBrush(Colors.Green);
            
            return new SolidColorBrush(Colors.Orange);
        }
    }
    
    public string MessagesStatusText => Messages.Count == 0 
        ? "No messages received yet" 
        : $"Total: {Messages.Count} messages";
    
    public string Status => _isConnected ? "ON" : "OFF";

    public bool CanSendMessage => IsConnected && !IsSending; 
    
    public bool CanConnect => !IsConnected 
        && Port is > 0 and <= 65535
        && !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Sender)
        && !string.IsNullOrWhiteSpace(Target);
    
    #endregion
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        Closing += MainWindow_Closing;
    }

    private void OnSessionLogon()
    {
        IsConnected = true;
    }
    
    private void OnSessionLogout()
    {
        IsConnected = false;
    }

    private void OnInboundMessage(Message rawMessage)
    {
        var message = ParseFixMessage(rawMessage, MessageDirection.Incoming);
        Dispatcher.Invoke(() => Messages.Add(message));
    }
    
    private void OnOutboundMessage(Message rawMessage)
    {
        var message = ParseFixMessage(rawMessage, MessageDirection.Outgoing);
        Dispatcher.Invoke(() => Messages.Add(message));
    }
    
    private FixMessage ParseFixMessage(Message rawMessage, MessageDirection direction)
    {
        FixMessage message = new();
        try
        {
            message = new FixMessage
            {
                Timestamp = DateTime.Now,
                Direction = direction,
                RawMessage = rawMessage.ToString(),
                Description = "New Order Single"
            };
        }
        catch (Exception e)
        {
            _logger.Error($"Error parsing FIX message: {e.Message}");
            message.MessageType = "Unknown";
            message.Description = "Error parsing message";
        }

        return message;
    }

    private async void OnClick(object sender, RoutedEventArgs _)
    {
        try
        {
            switch (_connectionType)
            {
                case ConnectionType.Acceptor:
                {
                    _acceptor = new Acceptor()
                    {
                        Host = Host,
                        Port = Port,
                        SenderCompId = Sender,
                        TargetCompId = Target,
                    };
                    _acceptor.OnSessionLogon += OnSessionLogon;
                    _acceptor.OnSessionLogout += OnSessionLogout;
                    _acceptor.OnInboundMessage += OnInboundMessage;
                    _acceptor.OnOutboundMessage += OnOutboundMessage;
                    await Task.Run(() => _acceptor.Start(_cts.Token), _cts.Token);
                    _logger.Info("Acceptor stopped!");
                    break;
                }
                case ConnectionType.Initiator:
                {
                    var initiator = new Initiator()
                    {
                        Host = Host,
                        Port = Port,
                        SenderCompId = Sender,
                        TargetCompId = Target,
                    };
                    initiator.OnSessionLogon += OnSessionLogon;
                    initiator.OnSessionLogout += OnSessionLogout;
                    initiator.OnInboundMessage += OnInboundMessage;
                    initiator.OnOutboundMessage += OnOutboundMessage;
                    await Task.Run(() => initiator.Start(_cts.Token), _cts.Token);
                    _logger.Info("Initiator stopped!");
                    break;
                }
                default:
                    return;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Info("Acceptor operation was canceled!");
        }
        catch (Exception e)
        {
            _logger.Error("Acceptor ERROR: {error}", e.Message);
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        try
        {
            _cts?.Cancel();
            Task.Delay(1000).Wait();
            _cts?.Dispose();
        }
        finally
        {
            e.Cancel = false;
            Application.Current.Shutdown();
        }
    }
    
    private void OnMessageDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListViewItem { DataContext: FixMessage message }) 
            return;
        
        try
        {
            var messageWindow = new MessageDetailWindow(message);
            messageWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error dragging message: {ex.Message}");
        }
    }
    
    private void ClearSendMessageClick(object sender, RoutedEventArgs routedEventArgs)
    {
        MessageToSend = string.Empty;
    }

    private void LoadSendMessageClick(object sender, RoutedEventArgs routedEventArgs)
    {
        // Templates de exemplo
        // var templates = new Dictionary<string, string>
        // {
        //     ["Heartbeat"] = @"8=FIX.4.4\x019=XX\x0135=0\x0149=SENDER\x0156=TARGET\x0134=X\x0152=TIMESTAMP\x0110=XXX\x01",
        //     ["Test Request"] = @"8=FIX.4.4\x019=XX\x0135=1\x0149=SENDER\x0156=TARGET\x0134=X\x0152=TIMESTAMP\x01112=TESTID\x0110=XXX\x01",
        //     ["New Order"] = @"8=FIX.4.4\x019=XX\x0135=D\x0149=SENDER\x0156=TARGET\x0134=X\x0152=TIMESTAMP\x0111=CLORDID\x0155=SYMBOL\x0154=1\x0140=2\x0138=100\x0144=25.50\x0110=XXX\x01"
        // };
        //
        // // Aqui você poderia mostrar um diálogo para seleção
        // // Por simplicidade, vamos usar Test Request como padrão
        // MessageToSend = templates["Test Request"];
    }

    private async void SendMessageClick(object sender, RoutedEventArgs routedEventArgs)
    {
        try
        {
            IsSending = true;
        
            _logger.Info("Attempting to send FIX message");
        
            // Processar mensagem antes de enviar
            var processedMessage = ProcessMessageForSending(MessageToSend);
            if (processedMessage == null)
            {
                MessageToSend = "Error";
                return;
            }
        
            // Aqui você implementaria o envio real da mensagem
            _acceptor?.SendMessage(processedMessage);
        
            // Limpar campo após envio bem-sucedido
            MessageToSend = string.Empty;
        }
        catch (Exception e)
        {
            _logger.Error($"Error sending message: {e.Message}");
        }
        finally
        {
            IsSending = false;
        }
    }
    
    private Message? ProcessMessageForSending(string message)
    {
        try
        {
            return new Message(message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing message to send: {ex.Message}");
        }
        return null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}