namespace Tuku.Desktop.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using Tuku.Desktop.Infrastructure;
    using Tuku.Desktop.ViewModels;

    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            AppBootstrapper.Initialize();
            viewModel = new MainViewModel(
                AppBootstrapper.PracticeService,
                AppBootstrapper.AtlasService,
                AppBootstrapper.TaxonomyService,
                new MessageBoxDialogService());
            DataContext = viewModel;
        }

        private void OnNewPractice(object sender, RoutedEventArgs e)
        {
            var window = new NewPracticeWindow(
                AppBootstrapper.PracticeService,
                AppBootstrapper.TaxonomyService)
            {
                Owner = this
            };
            if (window.ShowDialog() == true)
            {
                viewModel.Search.ExecuteSearch(
                    viewModel.Browser.RegionFilter,
                    viewModel.Browser.PartFilter,
                    viewModel.Browser.AtlasFilter);
                viewModel.StatusText = "新建完成";
            }
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            viewModel.Browser.Refresh();
            viewModel.Search.ExecuteSearch(
                viewModel.Browser.RegionFilter,
                viewModel.Browser.PartFilter,
                viewModel.Browser.AtlasFilter);
        }

        private void OnBrowseModeChanged(object sender, RoutedEventArgs e)
        {
            if (viewModel == null)
            {
                return;
            }

            var radio = sender as RadioButton;
            if (radio == ModeRegionRadio)
            {
                viewModel.Browser.Mode = BrowseMode.ByRegion;
            }
            else if (radio == ModePartRadio)
            {
                viewModel.Browser.Mode = BrowseMode.ByPart;
            }
        }
    }
}
