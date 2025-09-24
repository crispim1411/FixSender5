using System;
using System.Windows;

namespace FixSender5
{
    public partial class SetSeqNumWindow
    {
        public ulong NewInSeqNum { get; private set; }
        public ulong NewOutSeqNum { get; private set; }

        public SetSeqNumWindow(ulong currentInSeqNum, ulong currentOutSeqNum)
        {
            InitializeComponent();
            
            // Set current values
            InSeqNumTextBox.Text = currentInSeqNum.ToString();
            OutSeqNumTextBox.Text = currentOutSeqNum.ToString();
            
            // Focus on first textbox
            InSeqNumTextBox.Focus();
            InSeqNumTextBox.SelectAll();
            
            // Handle Enter key to move to next field
            InSeqNumTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    OutSeqNumTextBox.Focus();
                    OutSeqNumTextBox.SelectAll();
                }
            };
            
            OutSeqNumTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    OnOkClick(s, new RoutedEventArgs());
                }
            };
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (!ValidateInput())
                return;
            
            // Set the new values
            NewInSeqNum = ulong.Parse(InSeqNumTextBox.Text);
            NewOutSeqNum = ulong.Parse(OutSeqNumTextBox.Text);
            
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            // Validate Incoming SeqNum
            if (!int.TryParse(InSeqNumTextBox.Text, out int inSeqNum) || inSeqNum < 1 || inSeqNum > 999999)
            {
                MessageBox.Show("Incoming SeqNum must be a number between 1 and 999999.", 
                               "Invalid Input", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Warning);
                InSeqNumTextBox.Focus();
                InSeqNumTextBox.SelectAll();
                return false;
            }

            // Validate Outgoing SeqNum
            if (!int.TryParse(OutSeqNumTextBox.Text, out int outSeqNum) || outSeqNum < 1 || outSeqNum > 999999)
            {
                MessageBox.Show("Outgoing SeqNum must be a number between 1 and 999999.", 
                               "Invalid Input", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Warning);
                OutSeqNumTextBox.Focus();
                OutSeqNumTextBox.SelectAll();
                return false;
            }

            return true;
        }
    }
}