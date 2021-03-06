﻿using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Views.Service;
using NicoPlayerHohoema.Helpers;
using System.Windows.Input;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2;
using Prism.Windows.Navigation;
using Prism.Commands;
using Windows.UI.Xaml.Navigation;
using System.Collections.Async;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.ViewModels
{

	// Note: Communityの検索はページベースで行います。
	// また、ログインが必要です。

	public class SearchResultCommunityPageViewModel : HohoemaListingPageViewModelBase<CommunityInfoControlViewModel>
	{
		public CommunitySearchPagePayloadContent SearchOption { get; private set; }

        private string _SearchOptionText;
        public string SearchOptionText
        {
            get { return _SearchOptionText; }
            set { SetProperty(ref _SearchOptionText, value); }
        }

        public static List<SearchTarget> SearchTargets { get; } = Enum.GetValues(typeof(SearchTarget)).Cast<SearchTarget>().ToList();

        public ReactiveProperty<SearchTarget> SelectedSearchTarget { get; }

        private DelegateCommand<SearchTarget?> _ChangeSearchTargetCommand;
        public DelegateCommand<SearchTarget?> ChangeSearchTargetCommand
        {
            get
            {
                return _ChangeSearchTargetCommand
                    ?? (_ChangeSearchTargetCommand = new DelegateCommand<SearchTarget?>(target =>
                    {
                        if (target.HasValue && target.Value != SearchOption.SearchTarget)
                        {
                            var payload = SearchPagePayloadContentHelper.CreateDefault(target.Value, SearchOption.Keyword);
                            PageManager.Search(payload, true);
                        }
                    }));
            }
        }


        public class CommunitySearchSortOptionListItem
        {
            public string Label { get; set; }
            public CommunitySearchSort Sort { get; set; }
            public Order Order { get; set; }
        }

        public class CommynitySearchModeOptionListItem
        {
            public string Label { get; set; }
            public CommunitySearchMode Mode { get; set; }
        }

        public static IReadOnlyList<CommunitySearchSortOptionListItem> CommunitySearchSortOptionListItems { get; private set; }
        public static IReadOnlyList<CommynitySearchModeOptionListItem> CommunitySearchModeOptionListItems { get; private set; }

        static SearchResultCommunityPageViewModel()
        {
            var sortList = new[]
            {
                CommunitySearchSort.CreatedAt,
                CommunitySearchSort.UpdateAt,
                CommunitySearchSort.CommunityLevel,
                CommunitySearchSort.VideoCount,
                CommunitySearchSort.MemberCount
            };

            CommunitySearchSortOptionListItems = sortList.SelectMany(x =>
            {
                return new List<CommunitySearchSortOptionListItem>()
                {
                    new CommunitySearchSortOptionListItem()
                    {
                        Sort = x,
                        Order = Order.Descending,
                    },
                    new CommunitySearchSortOptionListItem()
                    {
                        Sort = x,
                        Order = Order.Ascending,
                    },
                };
            })
            .ToList();

            foreach (var item in CommunitySearchSortOptionListItems)
            {
                item.Label = Helpers.SortHelper.ToCulturizedText(item.Sort, item.Order);
            }


            CommunitySearchModeOptionListItems = new List<CommynitySearchModeOptionListItem>()
            {
                new CommynitySearchModeOptionListItem()
                {
                    Label = "キーワードで探す",
                    Mode = CommunitySearchMode.Keyword
                },
                new CommynitySearchModeOptionListItem()
                {
                    Label = "タグで探す",
                    Mode = CommunitySearchMode.Tag
                },
            };
        }

        public ReactivePropertySlim<CommunitySearchSortOptionListItem> SelectedSearchSort { get; private set; }
        public ReactivePropertySlim<CommynitySearchModeOptionListItem> SelectedSearchMode { get; private set; }


        public SearchResultCommunityPageViewModel(HohoemaApp app, PageManager pageManager)
            : base(app, pageManager, useDefaultPageTitle:false)
        {
            ChangeRequireServiceLevel(HohoemaAppServiceLevel.OnlineWithoutLoggedIn);

            SelectedSearchSort = new ReactivePropertySlim<CommunitySearchSortOptionListItem>();
            SelectedSearchMode = new ReactivePropertySlim<CommynitySearchModeOptionListItem>();

            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();

        }
		


		#region Commands


		private DelegateCommand _ShowSearchHistoryCommand;
		public DelegateCommand ShowSearchHistoryCommand
		{
			get
			{
				return _ShowSearchHistoryCommand
					?? (_ShowSearchHistoryCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.Search);
					}));
			}
		}

        #endregion


        protected override string ResolvePageName()
        {
            return SearchOption.Keyword;
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (e.Parameter is string)
            {
                SearchOption = PagePayloadBase.FromParameterString<CommunitySearchPagePayloadContent>(e.Parameter as string);
            }

            SelectedSearchTarget.Value = SearchOption?.SearchTarget ?? SearchTarget.Community;

            if (SearchOption == null)
            {
                throw new Exception("コミュニティ検索");
            }

            SelectedSearchSort.Value = CommunitySearchSortOptionListItems.FirstOrDefault(x => x.Order == SearchOption.Order && x.Sort == SearchOption.Sort);
            SelectedSearchMode.Value = CommunitySearchModeOptionListItems.FirstOrDefault(x => x.Mode == SearchOption.Mode);


            new[] {
                SelectedSearchSort.ToUnit(),
                SelectedSearchMode.ToUnit()
                }
            .CombineLatest()
            .Subscribe(async _ =>
            {
                SearchOption.Sort = SelectedSearchSort.Value.Sort;
                SearchOption.Order = SelectedSearchSort.Value.Order;
                SearchOption.Mode = SelectedSearchMode.Value.Mode;

                await ResetList();

                RefreshSearchOptionText();
            })
            .AddTo(_NavigatingCompositeDisposable);

            Database.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);


            base.OnNavigatedTo(e, viewModelState);
		}

		protected override IIncrementalSource<CommunityInfoControlViewModel> GenerateIncrementalSource()
		{
			return new CommunitySearchSource(SearchOption, HohoemaApp, PageManager);
		}


        private void RefreshSearchOptionText()
        {
            var optionText = Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
            var mode = SearchOption.Mode == CommunitySearchMode.Keyword ? "キーワード" : "タグ";

            SearchOptionText = $"{optionText}({mode})";
        }
	}

	public class CommunitySearchSource : IIncrementalSource<CommunityInfoControlViewModel>
	{
		public uint OneTimeLoadCount => 10;

		public HohoemaApp HohoemaApp { get; private set; }
		public PageManager PageManager { get; private set; }

		public uint TotalCount { get; private set; }
		public CommunitySearchResponse FirstResponse { get; private set; }

		public string SearchKeyword { get; private set; }
		public CommunitySearchMode Mode { get; private set; }
		public CommunitySearchSort Sort { get; private set; }
		public Order Order { get; private set; }

		public CommunitySearchSource(
			CommunitySearchPagePayloadContent searchOption
			, HohoemaApp app
			, PageManager pageManager
			)
		{
			HohoemaApp = app;
			PageManager = pageManager;
			SearchKeyword = searchOption.Keyword;
			Mode = searchOption.Mode;
			Sort = searchOption.Sort;
			Order = searchOption.Order;
		}

		public async Task<int> ResetSource()
		{
			try
			{
				FirstResponse = await HohoemaApp.ContentProvider.SearchCommunity(
					SearchKeyword
					, 1
					, Sort
					, Order
					, Mode
					);

				if (FirstResponse != null)
				{
					TotalCount = FirstResponse.TotalCount;
				}
			}
			catch { }
			

			return (int)TotalCount;
		}

		public async Task<IAsyncEnumerable<CommunityInfoControlViewModel>> GetPagedItems(int head, int count)
		{
			CommunitySearchResponse res = head == 0 ? FirstResponse : null;

			if (res == null)
			{
				var page = (uint)((head + count) / OneTimeLoadCount);
				res = await HohoemaApp.ContentProvider.SearchCommunity(
					SearchKeyword
					, page
					, Sort
					, Order
					, Mode
					);
			}

			if (res == null)
			{
				return AsyncEnumerable.Empty<CommunityInfoControlViewModel>();
			}

			if (false == res.IsStatusOK)
			{
				return AsyncEnumerable.Empty<CommunityInfoControlViewModel>();
			}

            return res.Communities.Select(x => new CommunityInfoControlViewModel(x, PageManager)).ToAsyncEnumerable();
		}

	}

	public class CommunityInfoControlViewModel : HohoemaListingPageItemBase, Interfaces.ICommunity
    {
		public string Name { get; private set; }
		public string ShortDescription { get; private set; }
		public string UpdateDate { get; private set; }
		public string IconUrl { get; private set; }
		public uint Level { get; private set; }
		public uint MemberCount { get; private set; }
		public uint VideoCount { get; private set; }

		public string CommunityId { get; private set; }

		public PageManager PageManager { get; private set; }

        public string Id => CommunityId;

        public CommunityInfoControlViewModel(Mntone.Nico2.Searches.Community.NicoCommynity commu, PageManager pageManager)
		{
			PageManager = pageManager;
			CommunityId = commu.Id;
            Name = commu.Name;
            ShortDescription = commu.ShortDescription;
            UpdateDate = commu.DateTime;
            IconUrl = commu.IconUrl.AbsoluteUri;

            Level = commu.Level;
			MemberCount = commu.MemberCount;
			VideoCount = commu.VideoCount;

            Label = commu.Name;
            Description = commu.ShortDescription;
            AddImageUrl(commu.IconUrl.OriginalString);
        }

        

	}
}
