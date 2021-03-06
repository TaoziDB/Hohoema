﻿using Mntone.Nico2.Videos.Ranking;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Prism.Commands;
using NicoPlayerHohoema.Models;
using System.Reactive.Linq;
using NicoPlayerHohoema.Helpers;
using Microsoft.Toolkit.Uwp.UI;

namespace NicoPlayerHohoema.ViewModels
{
    public class RankingCategoryListPageViewModel : HohoemaViewModelBase
    {

        private static readonly List<List<RankingCategory>> RankingCategories;

        static RankingCategoryListPageViewModel()
        {
            RankingCategories = new List<List<RankingCategory>>()
            {
                new List<RankingCategory>()
                {
                    RankingCategory.all
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_ent2,
                    RankingCategory.ent,
                    RankingCategory.music,
                    RankingCategory.sing,
                    RankingCategory.dance,
                    RankingCategory.play,
                    RankingCategory.vocaloid,
                    RankingCategory.nicoindies
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_life2,
                    RankingCategory.animal,
                    RankingCategory.cooking,
                    RankingCategory.nature,
                    RankingCategory.travel,
                    RankingCategory.sport,
                    RankingCategory.lecture,
                    RankingCategory.drive,
                    RankingCategory.history,
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_politics
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_tech,
                    RankingCategory.science,
                    RankingCategory.tech,
                    RankingCategory.handcraft,
                    RankingCategory.make,
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_culture2,
                    RankingCategory.anime,
                    RankingCategory.game,
                    RankingCategory.jikkyo,
                    RankingCategory.toho,
                    RankingCategory.imas,
                    RankingCategory.radio,
                    RankingCategory.draw,
                },
                new List<RankingCategory>()
                {
                    RankingCategory.g_other,
                    RankingCategory.are,
                    RankingCategory.diary,
                    RankingCategory.other,

                }

            };

        }


        Services.HohoemaDialogService _HohoemaDialogService;






        public RankingCategoryListPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Services.HohoemaDialogService dialogService)
            : base(hohoemaApp, pageManager)
        {
            _HohoemaDialogService = dialogService;

            _RankingSettings = HohoemaApp.UserSettings.RankingSettings;


            Func<RankingCategory, bool> checkFavorite = (RankingCategory cat) =>
            {
                return _RankingSettings.HighPriorityCategory.Any(x => x.Category == cat);
            };


            RankingCategoryItems = new ObservableCollection<RankingCategoryHostListItem>();
            FavoriteRankingCategoryItems = new ObservableCollection<RankingCategoryListPageListItem>();

            SelectedRankingCategory = new ReactiveProperty<RankingCategoryListPageListItem>();

            AddFavRankingCategory = new DelegateCommand(async () =>
            {
                var items = new AdvancedCollectionView();
                items.SortDescriptions.Add(new SortDescription("IsFavorit", SortDirection.Descending));
                items.SortDescriptions.Add(new SortDescription("Category", SortDirection.Ascending));

                foreach (var i in HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory)
                {
                    items.Add(new CategoryWithFav() { Category = i.Category, IsFavorit = true });
                }
                foreach (var i in HohoemaApp.UserSettings.RankingSettings.MiddlePriorityCategory)
                {
                    items.Add(new CategoryWithFav() { Category = i.Category });
                }
                items.Refresh();

                var choiceItems = await _HohoemaDialogService.ShowMultiChoiceDialogAsync(
                    "優先表示にするカテゴリを選択",
                    items.Cast<CategoryWithFav>().Select(x => new RankingCategoryInfo(x.Category)),
                    _RankingSettings.HighPriorityCategory.ToArray(),
                    x => x.DisplayLabel
                    );

                if (choiceItems == null) { return; }

                // choiceItemsに含まれるカテゴリをMiddleとLowから削除
                _RankingSettings.ResetFavoriteCategory();

                // HighにchoiceItemsを追加（重複しないよう注意）
                foreach (var cat in choiceItems)
                {
                    _RankingSettings.AddFavoritCategory(cat.Category);
                }

                ResetRankingCategoryItems();
            });



            AddDislikeRankingCategory = new DelegateCommand(async () =>
            {
                var items = new AdvancedCollectionView();
                items.SortDescriptions.Add(new SortDescription("IsFavorit", SortDirection.Descending));
                items.SortDescriptions.Add(new SortDescription("Category", SortDirection.Ascending));

                foreach (var i in HohoemaApp.UserSettings.RankingSettings.LowPriorityCategory)
                {
                    items.Add(new CategoryWithFav() { Category = i.Category, IsFavorit = true });
                }
                foreach (var i in HohoemaApp.UserSettings.RankingSettings.MiddlePriorityCategory)
                {
                    items.Add(new CategoryWithFav() { Category = i.Category });
                }
                items.Refresh();

                var choiceItems = await _HohoemaDialogService.ShowMultiChoiceDialogAsync(
                    "非表示にするカテゴリを選択",
                    items.Cast<CategoryWithFav>().Select(x => new RankingCategoryInfo(x.Category)),
                    _RankingSettings.LowPriorityCategory,
                    x => x.DisplayLabel
                    );

                if (choiceItems == null) { return; }

                // choiceItemsに含まれるカテゴリをMiddleとLowから削除
                _RankingSettings.ResetDislikeCategory();

                // HighにchoiceItemsを追加（重複しないよう注意）
                foreach (var cat in choiceItems)
                {
                    _RankingSettings.AddDislikeCategory(cat.Category);
                }

                ResetRankingCategoryItems();
            });

            ResetRankingCategoryItems();
        }

        RankingCategoryListPageListItem CreateRankingCategryListItem(RankingCategory category)
        {
            var categoryInfo = RankingCategoryInfo.CreateFromRankingCategory(category);
            var isFavoriteCategory = HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory.Contains(categoryInfo);
            return new RankingCategoryListPageListItem(category, isFavoriteCategory, OnRankingCategorySelected);
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
        }


        void ResetRankingCategoryItems()
        {
            RankingCategoryItems.Clear();

            RankingCategoryItems.Add(
                new RankingCategoryHostListItem("好きなランキング")
                {
                    ChildItems = HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory
                        .Select(x => new RankingCategoryListPageListItem(x.Category, true, OnRankingCategorySelected))
                        .ToList()
                }
                );
            foreach (var categoryList in RankingCategories)
            {
                // 非表示ランキングを除外したカテゴリリストを作成
                var label = categoryList.First().ToCultulizedText();

                var list = categoryList
                    .Where(x => !HohoemaApp.UserSettings.RankingSettings.IsDislikeRankingCategory(x))
                    .Select(x => CreateRankingCategryListItem(x))
                    .ToList();

                // 表示対象があればリストに追加
                if (list.Count > 0)
                {
                    RankingCategoryItems.Add(new RankingCategoryHostListItem(label) { ChildItems = list });
                }
            }

            RaisePropertyChanged(nameof(RankingCategoryItems));
        }

        internal void OnRankingCategorySelected(RankingCategory info)
        {
            PageManager.OpenPage(HohoemaPageType.RankingCategory, info.ToString());
        }

        public ReactiveProperty<RankingCategoryListPageListItem> SelectedRankingCategory { get; }

        public ObservableCollection<RankingCategoryListPageListItem> FavoriteRankingCategoryItems { get; private set; }
        public ObservableCollection<RankingCategoryHostListItem> RankingCategoryItems { get; private set; }

        RankingSettings _RankingSettings;



        public DelegateCommand AddFavRankingCategory { get; private set; }
        public DelegateCommand AddDislikeRankingCategory { get; private set; }

    }



    public class RankingCategoryHostListItem
    {
        public string Label { get; }
        public RankingCategoryHostListItem(string label)
        {
            Label = label;
            ChildItems = new List<RankingCategoryListPageListItem>();
            SelectedCommand = new DelegateCommand<RankingCategoryListPageListItem>((item) =>
            {
                item.PrimaryCommand.Execute(null);
            });

        }

        public bool HasItem => ChildItems.Count > 0;


        public List<RankingCategoryListPageListItem> ChildItems { get; set; }


        public DelegateCommand<RankingCategoryListPageListItem> SelectedCommand { get; }
    }


    public class RankingCategoryListPageListItem : SelectableItem<RankingCategory>
    {
        public bool IsFavorite { get; private set; }

        public RankingCategoryListPageListItem(RankingCategory category, bool isFavoriteCategory, Action<RankingCategory> selected)
            : base(category, selected)
        {
            IsFavorite = isFavoriteCategory;
        }
    }
}
