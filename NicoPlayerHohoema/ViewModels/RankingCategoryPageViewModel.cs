﻿using Mntone.Nico2;
using Mntone.Nico2.Searches.Video;
using Mntone.Nico2.Videos.Ranking;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class RankingCategoryPageViewModel : HohoemaVideoListingPageViewModelBase<RankedVideoInfoControlViewModel>
	{
		public RankingCategoryPageViewModel(HohoemaApp hohoemaApp, EventAggregator ea, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService)
			: base(hohoemaApp, pageManager, mylistDialogService)
		{
			ContentFinder = HohoemaApp.ContentFinder;
			_EventAggregator = ea;

			//			RankingSettings = hohoemaApp.UserSettings.RankingSettings;
			IsFailedRefreshRanking = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);
			CanChangeRankingParameter = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);



			// ランキングの対象
			RankingTargetItems = new List<RankingTargetListItem>()
			{
				new RankingTargetListItem(RankingTarget.view),
				new RankingTargetListItem(RankingTarget.res),
				new RankingTargetListItem(RankingTarget.mylist)
			};

			SelectedRankingTarget = new ReactiveProperty<RankingTargetListItem>(RankingTargetItems[0])
				.AddTo(_CompositeDisposable);


			// ランキングの集計期間
			RankingTimeSpanItems = new List<RankingTimeSpanListItem>()
			{
				new RankingTimeSpanListItem(RankingTimeSpan.hourly),
				new RankingTimeSpanListItem(RankingTimeSpan.daily),
				new RankingTimeSpanListItem(RankingTimeSpan.weekly),
				new RankingTimeSpanListItem(RankingTimeSpan.monthly),
				new RankingTimeSpanListItem(RankingTimeSpan.total),
			};

			SelectedRankingTimeSpan = new ReactiveProperty<RankingTimeSpanListItem>(RankingTimeSpanItems[0])
				.AddTo(_CompositeDisposable);

			Observable.CombineLatest(
				SelectedRankingTarget.ToUnit(),
				SelectedRankingTimeSpan.ToUnit()
				)
				.SubscribeOnUIDispatcher()
				.Subscribe(x => 
				{
					if (RequireCategoryInfo!= null)
					{
						ResetList();
					}
				})
				.AddTo(_CompositeDisposable);

		}



		internal void ShowVideoInfomation(string videoUrl)
		{
			PageManager.OpenPage(HohoemaPageType.VideoInfomation, videoUrl);
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				RequireCategoryInfo = RankingCategoryInfo.FromParameterString(e.Parameter as string);
			}
			else
			{
				RequireCategoryInfo = null;
			}

			base.OnNavigatedTo(e, viewModelState);
		}

		
		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			RankingSettings.Save().ConfigureAwait(false);

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}

		#region Implement HohoemaVideListViewModelBase

		protected override IIncrementalSource<RankedVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			IsFailedRefreshRanking.Value = false;

			var categoryInfo = RequireCategoryInfo != null ? RequireCategoryInfo : CategoryInfo;

			IIncrementalSource <RankedVideoInfoControlViewModel> source = null;
			try
			{
				switch (categoryInfo.RankingSource)
				{
					case RankingSource.CategoryRanking:
						RankingCategory = (RankingCategory)Enum.Parse(typeof(RankingCategory), categoryInfo.Parameter);
						var target = SelectedRankingTarget.Value.TargetType;
						var timeSpan = SelectedRankingTimeSpan.Value.TimeSpan;
						source = new CategoryRankingLoadingSource(HohoemaApp, PageManager, RankingCategory, target, timeSpan);

						CanChangeRankingParameter.Value = true;
						break;
					case RankingSource.SearchWithMostPopular:

						source = new CustomRankingLoadingSource(HohoemaApp, PageManager, categoryInfo.Parameter);

						CanChangeRankingParameter.Value = false;
						break;
					default:
						throw new NotImplementedException();
				}
			}
			catch
			{
				IsFailedRefreshRanking.Value = true;
			}


			return source;
		}


		protected override void PostResetList()
		{
			if (RequireCategoryInfo != null)
			{
				CategoryInfo = RequireCategoryInfo;
				RequireCategoryInfo = null;

				if (CategoryInfo.RankingSource == RankingSource.CategoryRanking)
				{
					RankingCategory category;
					if (Enum.TryParse(CategoryInfo.Parameter, out category))
					{
						var text = RankingCategoryExtention.ToCultulizedText(category);
						UpdateTitle($"{text} のランキング ");
					}
					else
					{
						UpdateTitle($"{CategoryInfo.Parameter} のランキング");
					}
				}
				else
				{
					UpdateTitle($"{CategoryInfo.Parameter} のランキング");
				}
			}
		}

		protected override uint IncrementalLoadCount
		{
			get
			{
				// 検索ベースランキングの場合は30個ずつ
				// （ニコ動の検索一回あたりの取得件数が30固定のため）
				return CanChangeRankingParameter.Value ? 20u : 30u;
			}
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (RequireCategoryInfo != null)
			{
				return !RequireCategoryInfo.Equals(CategoryInfo)
					|| mode != NavigationMode.Back;
			}
			else
			{
				return mode != NavigationMode.Back;
			}
		}

		#endregion

		


		public ReactiveProperty<bool> IsFailedRefreshRanking { get; private set; }

		public ReactiveProperty<bool> CanChangeRankingParameter { get; private set; }


		public RankingCategoryInfo RequireCategoryInfo { get; private set; }
		public RankingCategoryInfo CategoryInfo { get; private set; }

		public RankingCategory RankingCategory { get; private set; }

		public List<RankingTargetListItem> RankingTargetItems { get; private set; }
		public ReactiveProperty<RankingTargetListItem> SelectedRankingTarget { get; private set; }

		public List<RankingTimeSpanListItem> RankingTimeSpanItems { get; private set; }
		public ReactiveProperty<RankingTimeSpanListItem> SelectedRankingTimeSpan { get; private set; }

		private NiconicoContentFinder ContentFinder;
		private EventAggregator _EventAggregator;
		public RankingSettings RankingSettings
		{
			get
			{
				return HohoemaApp.UserSettings.RankingSettings;
			}
		}
	}


	public class CategoryRankingLoadingSource : IIncrementalSource<RankedVideoInfoControlViewModel>
	{
		public CategoryRankingLoadingSource(HohoemaApp app, PageManager pageManager, RankingCategory category, RankingTarget target, RankingTimeSpan timeSpan)
		{
			_HohoemaApp = app;
			_PageManager = pageManager;
			_Category = category;
			_Target = target;
			_TimeSpan = timeSpan;
		}


		public async Task<int> ResetSource()
		{
			RankingRss = await NiconicoRanking.GetRankingData(_Target, _TimeSpan, _Category);

			return RankingRss.Channel.Items.Count;
		}


		public async Task<IEnumerable<RankedVideoInfoControlViewModel>> GetPagedItems(uint position, uint pageSize)
		{
			while(_HohoemaApp.MediaManager == null)
			{
				await Task.Delay(100);
			}

			var contentFinder = _HohoemaApp.ContentFinder;
			var mediaManager = _HohoemaApp.MediaManager;


			var head = (int)(position);
			var tail = head + pageSize;

			List<RankedVideoInfoControlViewModel> items = new List<RankedVideoInfoControlViewModel>();
			for (int i = head; i < tail; ++i)
			{
				var rank = i;

				if (rank > RankingRss.Channel.Items.Count)
				{
					break;
				}

				var item = RankingRss.Channel.Items[rank-1];
				var nicoVideo = await mediaManager.GetNicoVideo(item.GetVideoId());


				var vm = new RankedVideoInfoControlViewModel(
					(uint)(rank)
					, nicoVideo
					, _PageManager
				);
				await vm.LoadThumbnail();

				items.Add(vm);
			}

			return items;			
		}


		NiconicoRankingRss RankingRss;
		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		RankingCategory _Category;
		RankingTarget _Target;
		RankingTimeSpan _TimeSpan;
	}



	public class CustomRankingLoadingSource : IIncrementalSource<RankedVideoInfoControlViewModel>
	{
		public CustomRankingLoadingSource(HohoemaApp app, PageManager pageManager, string parameter)
		{
			_HohoemaApp = app;
			_PageManager = pageManager;
			_Parameter = parameter;
		}

		public async Task<int> ResetSource()
		{
			var contentFinder = _HohoemaApp.ContentFinder;

			var res = await contentFinder.GetKeywordSearch(_Parameter, 0, 1, Sort.Popurarity).ConfigureAwait(false);

			return res.TotalCount;
		}


		public async Task<IEnumerable<RankedVideoInfoControlViewModel>> GetPagedItems(uint head, uint pageSize)
		{
			var contentFinder = _HohoemaApp.ContentFinder;
			var mediaManager = _HohoemaApp.MediaManager;

			var response = await _HohoemaApp.ContentFinder.GetKeywordSearch(_Parameter, head, pageSize, Sort.Popurarity);


			List<RankedVideoInfoControlViewModel> items = new List<RankedVideoInfoControlViewModel>();

			var count = response.VideoInfoItems.Count();
			for (int i = 0; i < count; ++i)
			{
				var item = response.VideoInfoItems[i];
				var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(item.Video.Id);

				var videoInfoVM = new RankedVideoInfoControlViewModel(
					(uint)(i + 1)
					, nicoVideo
					, _PageManager
					);

				items.Add(videoInfoVM);
			}

			foreach (var item in items)
			{
				await item.LoadThumbnail().ConfigureAwait(false);
			}

			return items;
		}

		
		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		string _Parameter;

	}





	public class RankedVideoInfoControlViewModel : VideoInfoControlViewModel
	{
		public RankedVideoInfoControlViewModel(uint rank, NicoVideo nicoVideo, PageManager pageManager)
			: base(nicoVideo, pageManager)
		{
			Rank = rank;
		}



		public uint Rank { get; private set; }
	}

	public class RankingTargetListItem : BindableBase
	{
		public RankingTargetListItem(RankingTarget target)
		{
			TargetType = target;
			Label = target.ToCultulizedText();
		}

		public string Label { get; private set; }

		public RankingTarget TargetType { get; private set; }
	}


	public class RankingTimeSpanListItem : BindableBase
	{
		public RankingTimeSpanListItem(RankingTimeSpan rankingTimeSpan)
		{
			TimeSpan = rankingTimeSpan;
			Label = rankingTimeSpan.ToCultulizedText();
		}

		public string Label { get; private set; }

		public RankingTimeSpan TimeSpan { get; private set; }
	}



	public class RankingCategoryListItem : SelectableItem<RankingCategoryInfo>
	{
		public RankingCategoryListItem(RankingCategoryInfo categoryInfo, Action<RankingCategoryInfo> selectedAction)
			: base(categoryInfo, selectedAction)
		{
			Label = categoryInfo.DisplayLabel;
		}

		public string Label { get; private set; }
	}

}
