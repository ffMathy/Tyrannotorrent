using System.Windows;
using Tyranotorrent.ViewModels;

namespace Tyranotorrent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = MainWindowViewModel.Instance;
        }
    }
}
