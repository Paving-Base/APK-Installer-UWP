using AdvancedSharpAdbClient.Models;
using APKInstaller.Controls;
using APKInstaller.Helpers;
using APKInstaller.ViewModels.ToolsPages;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace APKInstaller.Pages.ToolsPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ProcessesPage : Page
    {
        private readonly ProcessesViewModel Provider;

        public ProcessesPage()
        {
            InitializeComponent();
            Provider = new ProcessesViewModel(Dispatcher) { TitleBar = TitleBar };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ADBHelper.Monitor.DeviceChanged += OnDeviceChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ADBHelper.Monitor.DeviceChanged -= OnDeviceChanged;
        }

        private void OnDeviceChanged(object sender, DeviceDataEventArgs e) => _ = Provider.GetDevicesAsync();

        private void TitleBar_BackRequested(TitleBar sender, object e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearSort();
            _ = Provider.GetProcessAsync();
        }

        private void TitleBar_RefreshEvent(TitleBar sender, object e)
        {
            ClearSort();
            _ = Provider.GetDevicesAsync().ContinueWith(x => Provider.GetProcessAsync()).Unwrap();
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            Provider.DeviceComboBox = sender as ComboBox;
            _ = Provider.GetDevicesAsync();
        }

        private void DataGrid_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (!TitleBar.IsRefreshButtonVisible) { return; }

            DataGrid dataGrid = sender as DataGrid;
            // Clear previous sorted column if we start sorting a different column
            string previousSortedColumn = Provider.CachedSortedColumn;
            if (previousSortedColumn != string.Empty)
            {
                foreach (DataGridColumn dataGridColumn in dataGrid.Columns)
                {
                    if (dataGridColumn.Tag != null && dataGridColumn.Tag.ToString() == previousSortedColumn &&
                        (e.Column.Tag == null || previousSortedColumn != e.Column.Tag.ToString()))
                    {
                        dataGridColumn.SortDirection = null;
                    }
                }
            }

            // Toggle clicked column's sorting method
            if (e.Column.Tag != null)
            {
                if (e.Column.SortDirection == null)
                {
                    _ = Provider.SortDataAsync(e.Column.Tag.ToString(), true);
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else if (e.Column.SortDirection == DataGridSortDirection.Ascending)
                {
                    _ = Provider.SortDataAsync(e.Column.Tag.ToString(), false);
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
                else
                {
                    _ = Provider.SortDataAsync(e.Column.Tag.ToString(), true);
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
            }
        }

        private void ClearSort()
        {
            // Clear previous sorted column if we start sorting a different column
            string previousSortedColumn = Provider.CachedSortedColumn;
            if (previousSortedColumn != string.Empty)
            {
                foreach (DataGridColumn dataGridColumn in DataGrid.Columns)
                {
                    dataGridColumn.SortDirection = null;
                }
            }
        }
    }
}
