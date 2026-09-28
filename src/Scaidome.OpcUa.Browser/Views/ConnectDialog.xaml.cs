using System.Windows;
using System.Windows.Controls;
using Scaidome.OpcUa.Browser.ViewModels;

namespace Scaidome.OpcUa.Browser.Views;

public partial class ConnectDialog : UserControl
{
    public ConnectDialog()
    {
        InitializeComponent();
    }

    // PasswordBox.Password is deliberately not a dependency property, so it is pushed to the view model by hand.
    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectViewModel viewModel)
            viewModel.Password = PasswordBox.Password;
    }
}
