namespace Tuku.Desktop.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using Tuku.Application.Dtos;
    using Tuku.Application.Services;
    using Tuku.Domain.Taxonomy;

    public sealed class LayerEditItem : ObservableObject
    {
        private string? originalText;
        private string? currentText;

        public LayerEditItem(string? original, string? current)
        {
            originalText = original;
            currentText = current;
        }

        public string? OriginalText
        {
            get { return originalText; }
            set { SetProperty(ref originalText, value); }
        }

        public string? CurrentText
        {
            get { return currentText; }
            set { SetProperty(ref currentText, value); }
        }
    }

    public sealed class PracticeEditorViewModel : ObservableObject
    {
        private readonly PracticeService practiceService;
        private readonly TaxonomyService taxonomyService;
        private Guid currentPracticeId;
        private bool isEditing;
        private string? name;
        private string? code;
        private Guid mainPartId;
        private string? notes;
        private string? referenceNote;
        private string? tagsText;
        private string? message;
        private bool isVerified;
        private string sourceSummary = "（无图集来源：手动创建）";
        private RevisionSummary? selectedRevision;

        public PracticeEditorViewModel(PracticeService practiceService, TaxonomyService taxonomyService)
        {
            this.practiceService = practiceService;
            this.taxonomyService = taxonomyService;
            Layers = new ObservableCollection<LayerEditItem>();
            Parts = new ObservableCollection<TaxonomyNode>();
            Revisions = new ObservableCollection<RevisionSummary>();
            BeginEditCommand = new RelayCommand(_ => BeginEdit(), _ => currentPracticeId != Guid.Empty && !isEditing);
            SaveCommand = new RelayCommand(_ => Save(), _ => isEditing);
            CancelEditCommand = new RelayCommand(_ => CancelEdit(), _ => isEditing);
            AddLayerCommand = new RelayCommand(_ => Layers.Add(new LayerEditItem(null, string.Empty)), _ => isEditing);
            RemoveLayerCommand = new RelayCommand(p => RemoveLayer(p as LayerEditItem), _ => isEditing);
            MoveLayerUpCommand = new RelayCommand(p => MoveLayer(p as LayerEditItem, -1), _ => isEditing);
            MoveLayerDownCommand = new RelayCommand(p => MoveLayer(p as LayerEditItem, 1), _ => isEditing);
            RestoreRevisionCommand = new RelayCommand(_ => RestoreSelectedRevision(), _ => !isEditing && selectedRevision != null && currentPracticeId != Guid.Empty);
            CopyPlainTextCommand = new RelayCommand(_ => CopyPlainText(), _ => currentPracticeId != Guid.Empty);
        }

        public ObservableCollection<LayerEditItem> Layers { get; private set; }

        public ObservableCollection<TaxonomyNode> Parts { get; private set; }

        public ObservableCollection<RevisionSummary> Revisions { get; private set; }

        public Guid CurrentPracticeId
        {
            get { return currentPracticeId; }
        }

        public bool HasPractice
        {
            get { return currentPracticeId != Guid.Empty; }
        }

        public bool IsEditing
        {
            get { return isEditing; }
            private set
            {
                if (SetProperty(ref isEditing, value))
                {
                    RaisePropertyChanged("HasPractice");
                }
            }
        }

        public bool IsDirty
        {
            get { return isEditing && Layers.Any(l => !string.IsNullOrWhiteSpace(l.CurrentText)); }
        }

        public string? Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public string? Code
        {
            get { return code; }
            set { SetProperty(ref code, value); }
        }

        public Guid MainPartId
        {
            get { return mainPartId; }
            set { SetProperty(ref mainPartId, value); }
        }

        public string? Notes
        {
            get { return notes; }
            set { SetProperty(ref notes, value); }
        }

        public string? ReferenceNote
        {
            get { return referenceNote; }
            set { SetProperty(ref referenceNote, value); }
        }

        public string? TagsText
        {
            get { return tagsText; }
            set { SetProperty(ref tagsText, value); }
        }

        public string? Message
        {
            get { return message; }
            set { SetProperty(ref message, value); }
        }

        public bool IsVerified
        {
            get { return isVerified; }
            set
            {
                if (isVerified == value)
                {
                    return;
                }

                if (currentPracticeId != Guid.Empty && !isEditing)
                {
                    var result = practiceService.SetVerified(currentPracticeId, value);
                    if (result.Status != PracticeSaveStatus.Saved)
                    {
                        Message = "更新核对状态失败";
                        return;
                    }
                }

                isVerified = value;
                RaisePropertyChanged("IsVerified");
                Message = value ? "已标记为核对通过" : "已恢复为未核对";
            }
        }

        public string SelectedPartName
        {
            get
            {
                var part = Parts.FirstOrDefault(p => p.Id == mainPartId);
                return part == null ? "其他/待分类" : part.Name;
            }
        }

        public string SourceSummary
        {
            get { return sourceSummary; }
            set { SetProperty(ref sourceSummary, value); }
        }

        public RevisionSummary? SelectedRevision
        {
            get { return selectedRevision; }
            set
            {
                if (SetProperty(ref selectedRevision, value))
                {
                    ShowRevisionSnapshot();
                }
            }
        }

        public string? RevisionSnapshotText { get; private set; }

        public RelayCommand BeginEditCommand { get; private set; }

        public RelayCommand SaveCommand { get; private set; }

        public RelayCommand CancelEditCommand { get; private set; }

        public RelayCommand AddLayerCommand { get; private set; }

        public RelayCommand RemoveLayerCommand { get; private set; }

        public RelayCommand MoveLayerUpCommand { get; private set; }

        public RelayCommand MoveLayerDownCommand { get; private set; }

        public RelayCommand RestoreRevisionCommand { get; private set; }

        public RelayCommand CopyPlainTextCommand { get; private set; }

        public void Load(Guid practiceId)
        {
            currentPracticeId = practiceId;
            IsEditing = false;
            Message = null;
            RevisionSnapshotText = null;
            selectedRevision = null;
            Parts.Clear();
            foreach (var part in taxonomyService.ListParts())
            {
                Parts.Add(part);
            }

            if (practiceId == Guid.Empty)
            {
                ClearFields();
                return;
            }

            var detail = practiceService.GetDetail(practiceId);
            if (detail == null)
            {
                ClearFields();
                Message = "条目不存在或已删除";
                return;
            }

            name = detail.Name;
            code = detail.Code;
            mainPartId = detail.MainPartId;
            notes = detail.Notes;
            referenceNote = detail.ReferenceNote;
            tagsText = string.Join("，", detail.TagNames ?? new System.Collections.Generic.List<string>());
            isVerified = detail.IsVerified;
            Layers.Clear();
            foreach (var layer in detail.Layers)
            {
                Layers.Add(new LayerEditItem(layer.OriginalText, layer.CurrentText));
            }

            Revisions.Clear();
            foreach (var revision in detail.Revisions)
            {
                Revisions.Add(revision);
            }

            SourceSummary = detail.AtlasId.HasValue && detail.Sources.Count > 0
                ? string.Join("；", detail.Sources.Select(s =>
                    string.Format(
                        "PDF 第 {0} 页{1}",
                        s.PageNumber,
                        string.IsNullOrEmpty(s.PrintedPageLabel) ? string.Empty : "／图集标注第 " + s.PrintedPageLabel + " 页")))
                : "（无图集来源：手动创建）";

            RaisePropertyChanged("Name");
            RaisePropertyChanged("Code");
            RaisePropertyChanged("MainPartId");
            RaisePropertyChanged("SelectedPartName");
            RaisePropertyChanged("Notes");
            RaisePropertyChanged("ReferenceNote");
            RaisePropertyChanged("TagsText");
            RaisePropertyChanged("IsVerified");
            RaisePropertyChanged("HasPractice");
            RaisePropertyChanged("SourceSummary");
        }

        public void BeginEdit()
        {
            if (currentPracticeId == Guid.Empty)
            {
                return;
            }

            IsEditing = true;
            Message = null;
        }

        public void CancelEdit()
        {
            IsEditing = false;
            Load(currentPracticeId);
            Message = "已放弃修改";
        }

        public PracticeSaveResult Save()
        {
            var command = new PracticeEditCommand
            {
                PracticeId = currentPracticeId,
                ExpectedRevision = practiceService.GetDetail(currentPracticeId).CurrentRevision,
                Name = name,
                Code = code,
                MainPartId = mainPartId,
                Notes = notes,
                ReferenceNote = referenceNote,
                Layers = Layers
                    .Where(l => !string.IsNullOrWhiteSpace(l.CurrentText) || !string.IsNullOrWhiteSpace(l.OriginalText))
                    .Select(l => new PracticeEditCommand.LayerEdit { OriginalText = l.OriginalText, CurrentText = l.CurrentText })
                    .ToList(),
                Sources = practiceService.GetDetail(currentPracticeId).Sources
                    .Select(s => new PracticeEditCommand.SourceEdit
                    {
                        PageId = s.PageId,
                        X = s.X,
                        Y = s.Y,
                        Width = s.Width,
                        Height = s.Height,
                        RegionSource = s.RegionSource,
                        RegionReliable = s.RegionReliable
                    })
                    .ToList(),
                TagNames = SplitTags(tagsText),
                Reason = "界面编辑"
            };

            var result = practiceService.Edit(command);
            if (result.Status == PracticeSaveStatus.Saved)
            {
                IsEditing = false;
                Load(currentPracticeId);
                Message = "已保存（修订 " + result.NewRevision + "）";
            }
            else if (result.Status == PracticeSaveStatus.RevisionConflict)
            {
                Message = "保存失败：该条目已被其他窗口修改，请重新打开条目再编辑";
                Load(currentPracticeId);
                IsEditing = false;
            }
            else
            {
                Message = "保存失败：" + string.Join("；", result.Errors);
            }

            return result;
        }

        public void CopyPlainText()
        {
            var builder = new StringBuilder();
            builder.AppendLine(name);
            if (!string.IsNullOrWhiteSpace(code))
            {
                builder.AppendLine(code);
            }

            foreach (var layer in Layers)
            {
                if (!string.IsNullOrWhiteSpace(layer.CurrentText))
                {
                    builder.AppendLine(layer.CurrentText);
                }
            }

            if (!string.IsNullOrWhiteSpace(notes))
            {
                builder.AppendLine(notes);
            }

            if (!string.IsNullOrWhiteSpace(referenceNote))
            {
                builder.AppendLine(referenceNote);
            }

            try
            {
                Clipboard.SetText(builder.ToString());
                Message = "已复制纯文字到剪贴板";
            }
            catch (Exception ex)
            {
                Message = "复制失败：" + ex.Message;
            }
        }

        private void RestoreSelectedRevision()
        {
            if (selectedRevision == null || currentPracticeId == Guid.Empty)
            {
                return;
            }

            var detail = practiceService.GetDetail(currentPracticeId);
            var result = practiceService.RestoreRevision(currentPracticeId, selectedRevision.RevisionNumber, detail.CurrentRevision);
            if (result.Status == PracticeSaveStatus.Saved)
            {
                Load(currentPracticeId);
                Message = "已恢复修订 " + selectedRevision.RevisionNumber + "（生成新修订 " + result.NewRevision + "）";
            }
            else
            {
                Message = "恢复失败：" + string.Join("；", result.Errors);
            }
        }

        private void ShowRevisionSnapshot()
        {
            RevisionSnapshotText = null;
            if (selectedRevision == null || currentPracticeId == Guid.Empty)
            {
                RaisePropertyChanged("RevisionSnapshotText");
                return;
            }

            var snapshot = practiceService.GetRevisionSnapshot(currentPracticeId, selectedRevision.RevisionNumber);
            if (snapshot == null)
            {
                RevisionSnapshotText = "（修订快照不存在）";
            }
            else
            {
                var builder = new StringBuilder();
                builder.AppendLine("名称：" + snapshot.Name);
                builder.AppendLine("编号：" + (snapshot.Code ?? "（空）"));
                foreach (var layer in snapshot.Layers)
                {
                    builder.AppendLine(layer.Order + ". " + layer.CurrentText);
                }

                RevisionSnapshotText = builder.ToString();
            }

            RaisePropertyChanged("RevisionSnapshotText");
        }

        private void RemoveLayer(LayerEditItem? item)
        {
            if (item != null)
            {
                Layers.Remove(item);
            }
        }

        private void MoveLayer(LayerEditItem? item, int offset)
        {
            if (item == null)
            {
                return;
            }

            var index = Layers.IndexOf(item);
            var target = index + offset;
            if (index < 0 || target < 0 || target >= Layers.Count)
            {
                return;
            }

            Layers.Move(index, target);
        }

        private void ClearFields()
        {
            name = null;
            code = null;
            mainPartId = Parts.Count > 0 ? Parts[0].Id : Guid.Empty;
            notes = null;
            referenceNote = null;
            tagsText = null;
            isVerified = false;
            Layers.Clear();
            Revisions.Clear();
            RaisePropertyChanged("Name");
            RaisePropertyChanged("Code");
            RaisePropertyChanged("MainPartId");
            RaisePropertyChanged("SelectedPartName");
            RaisePropertyChanged("Notes");
            RaisePropertyChanged("ReferenceNote");
            RaisePropertyChanged("TagsText");
            RaisePropertyChanged("IsVerified");
            RaisePropertyChanged("HasPractice");
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
