using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VideoTools.Services;
using VideoTools.ViewModels;

namespace VideoTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel mainViewModel;
        public MainWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = this.mainViewModel = mainViewModel;
        }

        private void TabControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var tabControl = (TabControl)sender;
            if (tabControl.SelectedIndex == -1)
                tabControl.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainViewModel.StopUpdate();
            Hide();
            e.Cancel = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            mainViewModel.StartUpdate();
        }

    }
}