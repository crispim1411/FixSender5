using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using FixSender5.FixEngine;
using NLog;

namespace FixSender5;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private string _status = "OFF";
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly CancellationTokenSource _cts = new();
    
    public string Status
    {
        get => _status;
        set
        {
            if (value == _status) return;
            _status = value;
            OnPropertyChanged();
        }
    }
    
    private Core _core;
    public MainWindow()
    {
        _core = new Core();
        InitializeComponent();
        DataContext = this;
        
        Closing += MainWindow_Closing;
    }

    private void OnSessionLogon()
    {
        Status = "ON";
    }
    
    private void OnSessionLogoff()
    {
        Status = "OFF";
    }

    private async void OnClickInitiator(object sender, RoutedEventArgs _)
    {
        try
        {
            var initiator = new Initiator();
            initiator.OnSessionLogon += OnSessionLogon;
            initiator.OnSessionLogoff += OnSessionLogoff;
            
            await Task.Run(() => initiator.Start(_cts.Token), _cts.Token);
            _logger.Info("Initiator stopped!");
        }
        catch (Exception e)
        {
            _logger.Error("Initiator ERROR: {error}", e.Message);
        }
    }
    
    private async void OnClickAcceptor(object sender, RoutedEventArgs _)
    {
        try
        {
            var acceptor = new Acceptor();
            await Task.Run(() => acceptor.Start(_cts.Token), _cts.Token);
            _logger.Info("Acceptor stopped!");
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

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}