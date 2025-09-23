using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using FixSender5.Models;

namespace FixSender5.View;

public partial class AppWindow
{
    private readonly AppViewModel _viewmodel = new();
    
    public AppWindow()
    {
        InitializeComponent();
        DataContext = _viewmodel;
    }

    private void OnConnectionButtonClick(object sender, RoutedEventArgs _)
    {
        _viewmodel.OnConnection();
    }
    
    private void OnResetSeqNumClick(object sender, RoutedEventArgs e)
    {
        _viewmodel.OnResetSeqNum();
    }

    private void OnSetSeqNumClick(object sender, RoutedEventArgs e)
    {
        _viewmodel.OnSetSeqNum();
    }

    public void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        try
        {
            _viewmodel.OnWindowClose();
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

        var messageWindow = new MessageDetailWindow(message);
        messageWindow.ShowDialog();
    }
    
    private void ClearSendMessageClick(object sender, RoutedEventArgs routedEventArgs)
    {
        _viewmodel.ClearSendMessage();
    }

    private void ClearMessageListClick(object sender, RoutedEventArgs routedEventArgs)
    {
        _viewmodel.ClearMessageListClick();
    }

    private void SendMessageClick(object sender, RoutedEventArgs routedEventArgs)
    {
        _viewmodel.SendMessage();
    }
}