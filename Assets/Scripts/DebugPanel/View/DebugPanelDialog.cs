#nullable enable

using System;
using Root.Service;
using Root.State;
using Root.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace DebugPanel.View
{
    /// 指定アイテム (家具 / 着せ替え) の付与と毛糸の付与を行うデバッグ用ダイアログ
    /// デバッグ専用のためプレハブは空の器のみとし、UI はすべてコードから生成する
    public class DebugPanelDialog : BaseDialogView
    {
        enum Category
        {
            Furniture,
            Outfit,
        }

        /// 一覧の 1 行。付与後にその行だけ表示を更新するためにラベルを保持する
        readonly struct ItemRow
        {
            public Category Category { get; }
            public uint Id { get; }
            public string Name { get; }
            public TextMeshProUGUI Label { get; }

            public ItemRow(Category category, uint id, string name, TextMeshProUGUI label)
            {
                Category = category;
                Id = id;
                Name = name;
                Label = label;
            }
        }

        const float PanelWidth = 960f;
        const float PanelHeight = 1560f;
        const float Padding = 24f;
        const float RowHeight = 78f;
        const float GrantButtonWidth = 132f;

        const float HeaderTop = 0f;
        const float HeaderHeight = 96f;
        const float YarnLabelTop = 112f;
        const float YarnLabelHeight = 52f;
        const float YarnButtonsTop = 172f;
        const float YarnButtonsHeight = 88f;
        const float TabsTop = 280f;
        const float TabsHeight = 88f;
        const float ListTop = 388f;
        const float ListBottom = 108f;
        const float StatusBottom = 24f;
        const float StatusHeight = 64f;

        static readonly int[] YarnAmounts = { 100, 1000, 10000 };

        static readonly Color PanelColor = new(0.09f, 0.10f, 0.13f, 0.98f);
        static readonly Color HeaderColor = new(1f, 1f, 1f, 0.05f);
        static readonly Color ViewportColor = new(0f, 0f, 0f, 0.25f);
        static readonly Color RowColor = new(1f, 1f, 1f, 0.06f);
        static readonly Color AccentColor = new(0.16f, 0.47f, 0.82f, 1f);
        static readonly Color MutedColor = new(1f, 1f, 1f, 0.16f);
        static readonly Color CloseColor = new(0.70f, 0.24f, 0.28f, 1f);
        static readonly Color StatusColor = new(1f, 1f, 1f, 0.7f);

        IUserItemInventoryService _userItemInventoryService = null!;
        IUserPointService _userPointService = null!;
        MasterDataState _masterDataState = null!;

        TextMeshProUGUI _yarnLabel = null!;
        TextMeshProUGUI _statusLabel = null!;
        ScrollRect _listScrollRect = null!;
        RectTransform _listContent = null!;
        Image _furnitureTabImage = null!;
        Image _outfitTabImage = null!;

        Category _category = Category.Furniture;

        [Inject]
        public void Construct(
            IUserItemInventoryService userItemInventoryService,
            IUserPointService userPointService,
            MasterDataState masterDataState)
        {
            _userItemInventoryService = userItemInventoryService;
            _userPointService = userPointService;
            _masterDataState = masterDataState;

            BuildLayout();
            SelectCategory(Category.Furniture);
            SetStatus("ready");
        }

        void BuildLayout()
        {
            DebugUiFactory.Stretch((RectTransform)transform);

            var panel = DebugUiFactory.CreateImage("Panel", transform, PanelColor).rectTransform;
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panel.anchoredPosition = Vector2.zero;

            BuildHeader(panel);
            BuildYarnSection(panel);
            BuildCategoryTabs(panel);
            BuildItemList(panel);
            BuildStatus(panel);
        }

        void BuildHeader(RectTransform panel)
        {
            var header = DebugUiFactory.CreateImage("Header", panel, HeaderColor).rectTransform;
            DebugUiFactory.AnchorTop(header, HeaderTop, HeaderHeight, 0f);

            var title = DebugUiFactory.CreateText("Title", header, "DEBUG", 40f, TextAlignmentOptions.Midline);
            DebugUiFactory.Stretch(title.rectTransform);

            var close = DebugUiFactory.CreateButton("CloseButton", header, "X", CloseColor, OnCloseClicked, 32f);
            DebugUiFactory.AnchorTopRight((RectTransform)close.transform, 16f, 16f, 64f, 64f);
        }

        void BuildYarnSection(RectTransform panel)
        {
            _yarnLabel = DebugUiFactory.CreateText(
                "YarnLabel", panel, string.Empty, 32f, TextAlignmentOptions.MidlineLeft);
            DebugUiFactory.AnchorTop(_yarnLabel.rectTransform, YarnLabelTop, YarnLabelHeight, Padding);

            var buttons = DebugUiFactory.CreateRect("YarnButtons", panel);
            DebugUiFactory.AnchorTop(buttons, YarnButtonsTop, YarnButtonsHeight, Padding);
            DebugUiFactory.AddHorizontalGroup(buttons, 12f);

            foreach (var amount in YarnAmounts)
            {
                DebugUiFactory.CreateButton(
                    $"AddYarn{amount}", buttons, $"+{amount:N0}", AccentColor, () => OnAddYarnClicked(amount));
            }

            DebugUiFactory.CreateButton("ResetYarn", buttons, "RESET", MutedColor, OnResetYarnClicked);

            RefreshYarnLabel();
        }

        void BuildCategoryTabs(RectTransform panel)
        {
            var tabs = DebugUiFactory.CreateRect("CategoryTabs", panel);
            DebugUiFactory.AnchorTop(tabs, TabsTop, TabsHeight, Padding);
            DebugUiFactory.AddHorizontalGroup(tabs, 12f);

            _furnitureTabImage = DebugUiFactory.CreateButton(
                "FurnitureTab", tabs, "FURNITURE", MutedColor, () => SelectCategory(Category.Furniture)).image;
            _outfitTabImage = DebugUiFactory.CreateButton(
                "OutfitTab", tabs, "OUTFIT", MutedColor, () => SelectCategory(Category.Outfit)).image;
        }

        void BuildItemList(RectTransform panel)
        {
            var viewport = DebugUiFactory.CreateImage("Viewport", panel, ViewportColor).rectTransform;
            DebugUiFactory.AnchorFill(viewport, ListTop, ListBottom, Padding);
            viewport.gameObject.AddComponent<RectMask2D>();

            _listContent = DebugUiFactory.CreateRect("Content", viewport);
            _listContent.anchorMin = new Vector2(0f, 1f);
            _listContent.anchorMax = new Vector2(1f, 1f);
            _listContent.pivot = new Vector2(0.5f, 1f);
            _listContent.sizeDelta = Vector2.zero;
            _listContent.anchoredPosition = Vector2.zero;

            DebugUiFactory.AddVerticalGroup(_listContent, 6f, new RectOffset(8, 8, 8, 8));
            var fitter = _listContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _listScrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            _listScrollRect.viewport = viewport;
            _listScrollRect.content = _listContent;
            _listScrollRect.horizontal = false;
            _listScrollRect.vertical = true;
            _listScrollRect.movementType = ScrollRect.MovementType.Elastic;
            _listScrollRect.scrollSensitivity = 40f;
        }

        void BuildStatus(RectTransform panel)
        {
            _statusLabel = DebugUiFactory.CreateText(
                "StatusLabel", panel, string.Empty, 26f, TextAlignmentOptions.MidlineLeft);
            DebugUiFactory.AnchorBottom(_statusLabel.rectTransform, StatusBottom, StatusHeight, Padding);
            _statusLabel.color = StatusColor;
        }

        void SelectCategory(Category category)
        {
            _category = category;
            _furnitureTabImage.color = category == Category.Furniture ? AccentColor : MutedColor;
            _outfitTabImage.color = category == Category.Outfit ? AccentColor : MutedColor;
            RebuildList();
        }

        void RebuildList()
        {
            // Destroy はフレーム終端まで遅延するため、レイアウトから即座に外れるよう親子関係を先に切る
            for (var i = _listContent.childCount - 1; i >= 0; i--)
            {
                var child = _listContent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            if (_category == Category.Furniture)
            {
                var furnitures = _masterDataState.Furnitures;
                if (furnitures is null)
                {
                    SetStatus("master data is not imported");
                    return;
                }

                foreach (var furniture in furnitures)
                {
                    CreateFurnitureRow(furniture);
                }
            }
            else
            {
                var outfits = _masterDataState.Outfits;
                if (outfits is null)
                {
                    SetStatus("master data is not imported");
                    return;
                }

                foreach (var outfit in outfits)
                {
                    CreateOutfitRow(outfit);
                }
            }

            _listScrollRect.verticalNormalizedPosition = 1f;
        }

        void CreateFurnitureRow(Furniture furniture)
        {
            var row = CreateRowBase($"Furniture{furniture.Id}");
            var item = new ItemRow(Category.Furniture, furniture.Id, furniture.Name, CreateRowLabel(row));
            RefreshRow(item);

            CreateGrantButton(row, "+1", () => OnAddFurnitureClicked(item, 1));
            CreateGrantButton(row, "+10", () => OnAddFurnitureClicked(item, 10));
        }

        void CreateOutfitRow(Outfit outfit)
        {
            var row = CreateRowBase($"Outfit{outfit.Id}");
            var item = new ItemRow(Category.Outfit, outfit.Id, outfit.Name, CreateRowLabel(row));
            RefreshRow(item);

            CreateGrantButton(row, "GRANT", () => OnGrantOutfitClicked(item));
        }

        RectTransform CreateRowBase(string name)
        {
            var row = DebugUiFactory.CreateImage(name, _listContent, RowColor).rectTransform;
            DebugUiFactory.AddHorizontalGroup(row, 8f, new RectOffset(16, 16, 8, 8), forceExpandWidth: false);
            DebugUiFactory.SetPreferredHeight(row, RowHeight);
            return row;
        }

        TextMeshProUGUI CreateRowLabel(RectTransform row)
        {
            var label = DebugUiFactory.CreateText(
                "Label", row, string.Empty, 26f, TextAlignmentOptions.MidlineLeft);
            DebugUiFactory.SetFlexibleWidth(label.rectTransform, 1f);
            return label;
        }

        void CreateGrantButton(RectTransform row, string label, Action onClick)
        {
            var button = DebugUiFactory.CreateButton($"Grant{label}", row, label, AccentColor, onClick, 24f);
            DebugUiFactory.SetPreferredWidth((RectTransform)button.transform, GrantButtonWidth);
        }

        void RefreshRow(ItemRow row)
        {
            if (row.Category == Category.Furniture)
            {
                row.Label.text = $"{row.Id,3}  {row.Name}   x{_userItemInventoryService.GetFurnitureCount(row.Id)}";
                return;
            }

            var owned = _userItemInventoryService.HasOutfit(row.Id) ? "OWNED" : "-";
            row.Label.text = $"{row.Id,3}  {row.Name}   {owned}";
        }

        void OnAddFurnitureClicked(ItemRow row, int amount)
        {
            var result = _userItemInventoryService.AddFurniture(row.Id, amount);
            if (!result.IsSuccess)
            {
                SetStatus($"failed: {row.Name} ({result.Error})");
                return;
            }

            RefreshRow(row);
            SetStatus($"{row.Name} x{_userItemInventoryService.GetFurnitureCount(row.Id)}");
        }

        void OnGrantOutfitClicked(ItemRow row)
        {
            var result = _userItemInventoryService.GrantOutfit(row.Id);
            if (!result.IsSuccess)
            {
                SetStatus($"failed: {row.Name} ({result.Error})");
                return;
            }

            RefreshRow(row);
            SetStatus($"{row.Name} granted");
        }

        void OnAddYarnClicked(int amount)
        {
            var result = _userPointService.AddYarn(amount);
            RefreshYarnLabel();
            SetStatus(result.IsSuccess ? $"yarn +{amount:N0}" : $"failed: yarn ({result.Error})");
        }

        void OnResetYarnClicked()
        {
            var balance = _userPointService.GetYarnBalance();
            if (balance <= 0)
            {
                SetStatus("yarn is already 0");
                return;
            }

            var result = _userPointService.SpendYarn(balance);
            RefreshYarnLabel();
            SetStatus(result.IsSuccess ? "yarn reset" : $"failed: yarn ({result.Error})");
        }

        void RefreshYarnLabel()
        {
            _yarnLabel.text = $"YARN : {_userPointService.GetYarnBalance():N0}";
        }

        void SetStatus(string message)
        {
            _statusLabel.text = message;
        }

        void OnCloseClicked()
        {
            RequestClose(DialogResult.Close);
        }
    }
}
