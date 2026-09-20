namespace Tuku.Desktop.Views
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using Tuku.Application.Dtos;
    using Tuku.Application.Services;
    using Tuku.Domain.Taxonomy;

    public partial class NewPracticeWindow : Window
    {
        private readonly PracticeService practiceService;
        private readonly TaxonomyService taxonomyService;

        public NewPracticeWindow(PracticeService practiceService, TaxonomyService taxonomyService)
        {
            this.practiceService = practiceService;
            this.taxonomyService = taxonomyService;
            InitializeComponent();
            Parts = new ObservableCollection<TaxonomyNode>();
            DataContext = this;
            Loaded += Window_Loaded;
        }

        public ObservableCollection<TaxonomyNode> Parts { get; private set; }

        public string? PracticeName { get; set; }

        public string? Code { get; set; }

        public Guid MainPartId { get; set; }

        public string? TagsText { get; set; }

        public string? Notes { get; set; }

        public string? ReferenceNote { get; set; }

        public string? LayersText { get; set; }

        public string? ErrorText { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Parts.Clear();
            foreach (var part in taxonomyService.ListParts())
            {
                Parts.Add(part);
            }

            if (Parts.Count > 0)
            {
                MainPartId = Parts[0].Id;
            }
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            var layers = (LayersText ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Select(l => new PracticeEditCommand.LayerEdit { OriginalText = l, CurrentText = l })
                .ToList();

            var command = new CreatePracticeCommand
            {
                AtlasId = null,
                Name = PracticeName,
                Code = Code,
                MainPartId = MainPartId,
                Notes = Notes,
                ReferenceNote = ReferenceNote,
                Layers = layers,
                Sources = Enumerable.Empty<PracticeEditCommand.SourceEdit>().ToList(),
                TagNames = SplitTags(TagsText)
            };

            var result = practiceService.CreateManual(command);
            if (result.Status != PracticeSaveStatus.Saved)
            {
                ErrorText = string.Join("\n", result.Errors);
                return;
            }

            DialogResult = true;
            Close();
        }

        private static string[] SplitTags(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new string[0];
            }

            return text!
                .Split(new[] { '，', ',', '；', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToArray();
        }
    }
}
