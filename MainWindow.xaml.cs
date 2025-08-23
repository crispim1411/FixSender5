using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using FixSender5.FixEngine;
using NLog;

namespace FixSender5;

public enum ConnectionType
{
    Acceptor,
    Initiator
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : INotifyPropertyChanged
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly CancellationTokenSource _cts = new();
    
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
        }
    }
    
    public string Status => _isConnected ? "ON" : "OFF";
    
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

    private async void OnClick(object sender, RoutedEventArgs _)
    {
        try
        {
            switch (_connectionType)
            {
                case ConnectionType.Acceptor:
                {
                    var acceptor = new Acceptor()
                    {
                        Host = Host,
                        Port = Port,
                        SenderCompId = Sender,
                        TargetCompId = Target,
                    };
                    acceptor.OnSessionLogon += OnSessionLogon;
                    acceptor.OnSessionLogout += OnSessionLogout;
                    await Task.Run(() => acceptor.Start(_cts.Token), _cts.Token);
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}