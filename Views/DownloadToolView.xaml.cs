using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace VideoTools.Views
{
    /// <summary>
    /// Interaction logic for DownloadToolView.xaml
    /// </summary>
    public partial class DownloadToolView : UserControl
    {
        public DownloadToolView(DownloadToolViewModel downloadToolViewModel)
        {
            InitializeComponent();
            tgrid.DataContext = DataContext = downloadToolViewModel;
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {

            if (System.Uri.IsWellFormedUriString(Clipboard.GetText().Trim(), UriKind.Absolute))
                site.Text = Clipboard.GetText().Trim();
        }
    }
}
