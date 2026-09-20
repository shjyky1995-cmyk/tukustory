namespace Tuku.Desktop.ViewModels
{
    using System;
    using System.Windows;
    using Tuku.Application.Dtos;
    using Tuku.Application.Services;

    public sealed class MainViewModel : ObservableObject
    {
        private readonly IDialogService dialogService;
        private string statusText = "就绪";
        private bool cadConnected;

        public MainViewModel(
            PracticeService practiceService,
            AtlasService atlasService,
            TaxonomyService taxonomyService,
            IDialogService dialogService)
        {
            this.dialogService = dialogService;
            Browser = new BrowserViewModel(taxonomyService, atlasService);
            Search = new SearchViewModel(practiceService);
            Editor = new PracticeEditorViewModel(practiceService, taxonomyService);
            Browser.FilterChanged += (s, e) => Search.ExecuteSearch(Browser.RegionFilter, Browser.PartFilter, Browser.AtlasFilter);
            Search.SelectionChanged += OnSearchSelectionChanged;
            NewPracticeCommand = new RelayCommand(_ => NewPractice());
            InsertToCadCommand = new RelayCommand(_ => InsertToCad(), _ => Editor.HasPractice);
            RefreshCommand = new RelayCommand(_ => Browser.Refresh());
            Search.ExecuteSearch();
        }

        public BrowserViewModel Browser { get; private set; }

        public SearchViewModel Search { get; private set; }

        public PracticeEditorViewModel Editor { get; private set; }

        public string StatusText
        {
            get { return statusText; }
            set { SetProperty(ref statusText, value); }
        }

        public bool CadConnected
        {
            get { return cadConnected; }
            set { SetProperty(ref cadConnected, value); }
        }

        public RelayCommand NewPracticeCommand { get; private set; }

        public RelayCommand InsertToCadCommand { get; private set; }

        public RelayCommand RefreshCommand { get; private set; }

        public event EventHandler<CreatePracticeRequest>? NewPracticeRequested;

        private void OnSearchSelectionChanged(object sender, PracticeListItem? item)
        {
            if (item == null)
            {
                return;
            }

            if (Editor.IsEditing)
            {
                var answer = dialogService.ConfirmSaveDiscardCancel(
                    "当前条目有未保存的修改。是否保存？\n（是＝保存并切换，否＝放弃修改，取消＝留在当前条目）",
                    "未保存的修改");
                if (answer == MessageBoxResult.Cancel)
                {
                    return;
                }

                if (answer == MessageBoxResult.Yes)
                {
                    var result = Editor.Save();
                    if (result.Status != PracticeSaveStatus.Saved)
                    {
                        dialogService.Info("保存失败，已停止切换条目。\n" + string.Join("；", result.Errors), "保存失败");
                        return;
                    }
                }
                else
                {
                    Editor.CancelEdit();
                }
            }

            Editor.Load(item.Id);
            StatusText = "已打开：" + item.Name;
        }

        private void NewPractice()
        {
            var handler = NewPracticeRequested;
            if (handler != null)
            {
                handler(this, new CreatePracticeRequest());
            }
        }

        private void InsertToCad()
        {
            if (!CadConnected)
            {
                dialogService.Info(
                    "未检测到可用的 AutoCAD 连接。\n可使用“复制纯文字”先粘贴到图纸，或检查 CAD 与插件状态。",
                    "无法插入 CAD");
                return;
            }

            dialogService.Info("CAD 插入将在连接可用后启用。", "插入 CAD");
        }

        public sealed class CreatePracticeRequest : EventArgs
        {
        }
    }
}
