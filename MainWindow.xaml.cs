using FixSender5.View;

namespace FixSender5;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow 
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += ((AppWindow)Content).OnWindowClosing;
    }
}