namespace Tuku.Desktop.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Tuku.Application.Abstractions;
    using Tuku.Application.Services;
    using Tuku.Domain.Taxonomy;

    public enum BrowseMode
    {
        ByRegion = 0,
        ByPart = 1
    }

    public sealed class BrowserNode : ObservableObject
    {
        private bool isSelected;

        public BrowserNode(string header, NodeKind kind, Guid? id)
        {
            Header = header;
            Kind = kind;
            Id = id;
            Children = new ObservableCollection<BrowserNode>();
        }

        public enum NodeKind
        {
            All = 0,
            Region = 1,
            Atlas = 2,
            Part = 3
        }

        public string Header { get; private set; }

        public NodeKind Kind { get; private set; }

        public Guid? Id { get; private set; }

        public ObservableCollection<BrowserNode> Children { get; private set; }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (SetProperty(ref isSelected, value) && value)
                {
                    OnSelected();
                }
            }
        }

        public event EventHandler<BrowserNode>? Selected;

        private void OnSelected()
        {
            var handler = Selected;
            if (handler != null)
            {
                handler(this, this);
            }
        }
    }

    public sealed class BrowserViewModel : ObservableObject
    {
        private readonly TaxonomyService taxonomyService;
        private readonly AtlasService atlasService;
        private BrowseMode mode = BrowseMode.ByRegion;

        public BrowserViewModel(TaxonomyService taxonomyService, AtlasService atlasService)
        {
            this.taxonomyService = taxonomyService;
            this.atlasService = atlasService;
            Modes = new[] { BrowseMode.ByRegion, BrowseMode.ByPart };
            Rebuild();
        }

        public IEnumerable<BrowseMode> Modes { get; private set; }

        public ObservableCollection<BrowserNode> Roots { get; private set; } = null!;

        public Guid? RegionFilter { get; private set; }

        public Guid? AtlasFilter { get; private set; }

        public Guid? PartFilter { get; private set; }

        public BrowseMode Mode
        {
            get { return mode; }
            set
            {
                if (SetProperty(ref mode, value))
                {
                    Rebuild();
                    RaiseFilterChanged();
                }
            }
        }

        public event EventHandler? FilterChanged;

        public void Refresh()
        {
            Rebuild();
            RaiseFilterChanged();
        }

        private void Rebuild()
        {
            var roots = new ObservableCollection<BrowserNode>();
            var all = new BrowserNode("全部做法", BrowserNode.NodeKind.All, null);
            AttachSelectionHandler(all);
            roots.Add(all);

            if (mode == BrowseMode.ByRegion)
            {
                foreach (var region in taxonomyService.ListRegions())
                {
                    var regionNode = new BrowserNode(region.Name, BrowserNode.NodeKind.Region, region.Id);
                    AttachSelectionHandler(regionNode);
                    foreach (var atlas in atlasService.List(false).Where(a => a.RegionId == region.Id))
                    {
                        var atlasNode = new BrowserNode(atlas.Name, BrowserNode.NodeKind.Atlas, atlas.Id);
                        AttachSelectionHandler(atlasNode);
                        regionNode.Children.Add(atlasNode);
                    }

                    roots.Add(regionNode);
                }
            }
            else
            {
                foreach (var part in taxonomyService.ListParts())
                {
                    var partNode = new BrowserNode(part.Name, BrowserNode.NodeKind.Part, part.Id);
                    AttachSelectionHandler(partNode);
                    roots.Add(partNode);
                }
            }

            Roots = roots;
            RaisePropertyChanged("Roots");
        }

        private void AttachSelectionHandler(BrowserNode node)
        {
            node.Selected += (s, e) => ApplySelection(e);
            foreach (var child in node.Children)
            {
                AttachSelectionHandler(child);
            }
        }

        public void ApplySelection(BrowserNode node)
        {
            RegionFilter = null;
            AtlasFilter = null;
            PartFilter = null;
            switch (node.Kind)
            {
                case BrowserNode.NodeKind.Region:
                    RegionFilter = node.Id;
                    break;
                case BrowserNode.NodeKind.Atlas:
                    AtlasFilter = node.Id;
                    break;
                case BrowserNode.NodeKind.Part:
                    PartFilter = node.Id;
                    break;
            }

            RaiseFilterChanged();
        }

        private void RaiseFilterChanged()
        {
            var handler = FilterChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
