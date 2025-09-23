using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using FixSender5.FixEngine;
using FixSender5.Models;
using NLog;
using QuickFix;
using QuickFix.Fields;

namespace FixSender5.View;

public class AppViewModel : INotifyPropertyChanged
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private CancellationTokenSource? _cts;

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

    private FixMessage? _selectedMessage;

    public FixMessage? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            if (value == _selectedMessage) return;
            _selectedMessage = value;
            OnPropertyChanged();
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
            OnPropertyChanged();
            OnPropertyChanged(nameof(ValidationColor));
            OnPropertyChanged(nameof(CanSendMessage));
        }
    }

    private bool _isSending;

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

    private string _messageValidationText = string.Empty;

    public string MessageValidationText
    {
        get =>
            string.IsNullOrEmpty(MessageToSend)
                ? string.Empty
                : _messageValidationText;
        set
        {
            if (value == _messageValidationText) return;
            _messageValidationText = value;
            OnPropertyChanged();
        }
    }

    private ulong _inSeqNum = 1;

    public ulong InSeqNum
    {
        get => _inSeqNum;
        set
        {
            if (_inSeqNum != value)
            {
                _inSeqNum = value;
                OnPropertyChanged();
            }
        }
    }

    private ulong _outSeqNum = 1;

    public ulong OutSeqNum
    {
        get => _outSeqNum;
        set
        {
            if (_outSeqNum != value)
            {
                _outSeqNum = value;
                OnPropertyChanged();
            }
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

    private ConnectionState _status = ConnectionState.Disconnected;

    public ConnectionState Status
    {
        get => _status;
        set
        {
            if (value == _status) return;
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ButtonText));
        }
    }

    public string ButtonText =>
        Status switch
        {
            ConnectionState.Connected => "Disconnect",
            ConnectionState.Connecting => "Connecting...",
            ConnectionState.Disconnected => "Connect",
            _ => ""
        };

    public bool CanSendMessage => Status == ConnectionState.Connected && !IsSending;

    public bool CanConnect =>
        Status == ConnectionState.Disconnected
        && Port is > 0 and <= 65535
        && !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Sender)
        && !string.IsNullOrWhiteSpace(Target);

    #endregion

    #region View Methods

    public void OnConnection()
    {
        var currentStatus = Status;
        Status = ConnectionState.Connecting;
        switch (currentStatus)
        {
            case ConnectionState.Disconnected:
                Status = ConnectionState.Connecting;
                Connect();
                break;
            case ConnectionState.Connected:
            case ConnectionState.Connecting:
                Disconnect(); 
                break;
        }
    }

    public void OnResetSeqNum()
    {
        // Reset para 1
        InSeqNum = 1;
        OutSeqNum = 1;
        // alterar na sessão
    }

    public void OnSetSeqNum()
    {
        // Abrir janela para definir novos valores
        var setSeqNumWindow = new SetSeqNumWindow(
            InSeqNum, 
            OutSeqNum);

        if (setSeqNumWindow.ShowDialog() != true) return;
        
        InSeqNum = setSeqNumWindow.NewInSeqNum;
        OutSeqNum = setSeqNumWindow.NewOutSeqNum;
    }

    public void OnWindowClose()
    {
        _cts?.Cancel();
        Task.Delay(1000).Wait();
        _cts?.Dispose();
    }

    public void ClearSendMessage()
    {
        MessageToSend = string.Empty;
    }

    public void ClearMessageListClick()
    {
        Messages.Clear();
    }

    public void SendMessage()
    {
        try
        {
            IsSending = true;
        
            _logger.Info("Attempting to send FIX message");
        
            // Processar mensagem antes de enviar
            var processedMessage = ProcessMessageForSending(MessageToSend.Trim());
            if (processedMessage == null)
            {
                MessageValidationText = "⚠ Invalid FIX message";
                return;
            }
        
            // Aqui você implementaria o envio real da mensagem
            _acceptor?.SendMessage(processedMessage);
            _initiator?.SendMessage(processedMessage);
        
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
    
    #endregion
    
    private void OnSessionLogon()
    {
        Status = ConnectionState.Connected;
    }
    
    private void OnSessionLogout()
    {
        Status = ConnectionState.Disconnected;
    }

    private void OnInboundMessage(Message rawMessage)
    {
        var message = ParseFixMessage(rawMessage, MessageDirection.Incoming);
        Application.Current.Dispatcher.Invoke(() => Messages.Add(message));
    }
    
    private void OnOutboundMessage(Message rawMessage)
    {
        var message = ParseFixMessage(rawMessage, MessageDirection.Outgoing);
        Application.Current.Dispatcher.Invoke(() => Messages.Add(message));
    }

    private void OnChangeSeqNum((ulong, ulong) seqNums)
    {
        var (inSeqNum, outSeqNum) = seqNums;
        InSeqNum = inSeqNum;
        OutSeqNum = outSeqNum;
    }
    
    private FixMessage ParseFixMessage(Message rawMessage, MessageDirection direction)
    {
        FixMessage message = new();
        try
        {
            message = new FixMessage
            {
                Timestamp = GetMessageTimestamp(rawMessage),
                Direction = direction,
                RawMessage = rawMessage.ToString(),
                Description = GetMessageDescription(rawMessage)
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
    
    private static DateTime GetMessageTimestamp(Message message)
    {
        try
        {
            if (message.Header.IsSetField(Tags.SendingTime))
            {
                return message.Header.GetDateTime(Tags.SendingTime);
            }

            if (message.IsSetField(Tags.TransactTime))
            {
                return message.GetDateTime(Tags.TransactTime);
            }

            return DateTime.UtcNow;
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }
    
    private static string GetMessageDescription(Message message)
    {
        if (!message.Header.IsSetField(Tags.MsgType))
            return "Unknown Type";

        var msgType = message.Header.GetString(Tags.MsgType);
        return msgType switch
        {
            "0" => "Heartbeat",
            "1" => "Test Request",
            "2" => "Resend Request",
            "3" => "Reject", 
            "4" => "Sequence Reset",
            "5" => "Logout",
            "8" => "Execution Report",
            "9" => "Order Cancel Reject",
            "A" => "Logon",
            "D" => "New Order Single",
            "F" => "Order Cancel Request",
            "G" => "Order Cancel/Replace Request",
            "H" => "Order Status Request",
            "j" => "Business Message Reject",
            "n" => "XML Message",
            "BE" => "User Request",
            "BF" => "User Response",
            _ => $"Type {msgType}"
        };
    }
    
    private void Disconnect()
    {
        _cts?.Cancel();
        Status = ConnectionState.Disconnected;
    }

    private async void Connect()
    {
        try
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            
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
                    _acceptor.OnChangeSeqNum += OnChangeSeqNum;
                    await Task.Run(() => _acceptor.Start(_cts.Token), _cts.Token);
                    _logger.Info("Acceptor stopped!");
                    break;
                }
                case ConnectionType.Initiator:
                {
                    _initiator = new Initiator()
                    {
                        Host = Host,
                        Port = Port,
                        SenderCompId = Sender,
                        TargetCompId = Target,
                    };
                    _initiator.OnSessionLogon += OnSessionLogon;
                    _initiator.OnSessionLogout += OnSessionLogout;
                    _initiator.OnInboundMessage += OnInboundMessage;
                    _initiator.OnOutboundMessage += OnOutboundMessage;
                    _initiator.OnChangeSeqNum += OnChangeSeqNum;
                    await Task.Run(() => _initiator.Start(_cts.Token), _cts.Token);
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
    
    private Message? ProcessMessageForSending(string message)
    {
        try
        {
            if (message.Contains('|'))
                message = message.Replace("|", Message.SOH.ToString());
            else if (message.Contains("^A"))
                message = message.Replace("^A", Message.SOH.ToString());
            else if (message.Contains('�'))
                message = message.Replace("\ufffd", Message.SOH.ToString());
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