using System.Windows;
using Tyrannotorrent.ViewModels;

namespace Tyrannotorrent
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
