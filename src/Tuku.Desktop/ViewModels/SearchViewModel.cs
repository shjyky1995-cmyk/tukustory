namespace Tuku.Desktop.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Data;
    using Tuku.Application.Dtos;
    using Tuku.Application.Services;

    public sealed class SearchViewModel : ObservableObject
    {
        private readonly PracticeService practiceService;
        private readonly ICollectionView resultsView;
        private string? keywords;
        private string statusText = "就绪";
        private bool isBusy;
        private PracticeListItem? selectedItem;

        public SearchViewModel(PracticeService practiceService)
        {
            this.practiceService = practiceService;
            Results = new ObservableCollection<PracticeListItem>();
            resultsView = CollectionViewSource.GetDefaultView(Results);
            SearchCommand = new RelayCommand(_ => ExecuteSearch());
        }

        public ObservableCollection<PracticeListItem> Results { get; private set; }

        public ICollectionView ResultsView
        {
            get { return resultsView; }
        }

        public string? Keywords
        {
            get { return keywords; }
            set { SetProperty(ref keywords, value); }
        }

        public string StatusText
        {
            get { return statusText; }
            set { SetProperty(ref statusText, value); }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        public PracticeListItem? SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if (SetProperty(ref selectedItem, value))
                {
                    OnSelectionChanged();
                }
            }
        }

        public RelayCommand SearchCommand { get; private set; }

        public event EventHandler<PracticeListItem?>? SelectionChanged;

        public void ExecuteSearch(Guid? regionId = null, Guid? partId = null, Guid? atlasId = null)
        {
            IsBusy = true;
            try
            {
                var query = new PracticeQuery
                {
                    Keywords = keywords,
                    RegionId = regionId,
                    PartId = partId,
                    AtlasId = atlasId,
                    Page = 1,
                    PageSize = 500
                };
                var result = practiceService.Search(query);
                Results.Clear();
                foreach (var item in result.Items)
                {
                    Results.Add(item);
                }

                StatusText = string.Format(
                    "共 {0} 条{1}",
                    result.TotalCount,
                    result.TotalCount == 0 ? "（可清空筛选再试）" : string.Empty);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnSelectionChanged()
        {
            var handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, selectedItem);
            }
        }
    }
}
