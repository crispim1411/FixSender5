using System.IO;
using System.Windows;
using FixSender5.Models;
using Microsoft.Win32;

namespace FixSender5.View;

public partial class MessageDetailWindow
{
    private MessageDetailViewModel ViewModel { get; }

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
            MessageBox.Show(
                "Fields copied to clipboard!", 
                "Success", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error copying fields: {ex.Message}", 
                "Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }

    private void CopyRawMessage(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(ViewModel.RawMessage);
            MessageBox.Show(
                "Raw message copied to clipboard!", 
                "Success", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error copying message: {ex.Message}", 
                "Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }

    private void CopyFormattedMessage(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(ViewModel.FormattedMessage);
            MessageBox.Show(
                "Formatted message copied to clipboard!", 
                "Success", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error copying message: {ex.Message}", 
                "Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
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

            if (saveFileDialog.ShowDialog() != true) return;
            
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
            MessageBox.Show(
                "Message exported successfully!", 
                "Success", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error exporting message: {ex.Message}", 
                "Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }
}