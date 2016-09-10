﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.Models;
using System.Diagnostics;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Prism.Commands;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Threading;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedVideoListPageViewModel : HohoemaVideoListingPageViewModelBase<FeedVideoInfoControlViewModel>
	{
		public FeedGroup FeedGroup { get; private set; }


		public FeedVideoListPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService)
			: base(hohoemaApp, pageManager, mylistDialogService, isRequireSignIn: true)
		{
			AllMarkAsReadCommand = new DelegateCommand(async () =>
			{
				FeedGroup.ForceMarkAsRead();

				await HohoemaApp.FeedManager.SaveOne(FeedGroup);

				AllMarkAsReadCommand.RaiseCanExecuteChanged();
			}
			, () =>
			{
				return (FeedGroup?.GetUnreadItemCount() ?? 0 ) > 0;
			});


			SelectedItemsMarkAsReadCommand = SelectedItems.ToCollectionChanged()
				.Select(x => SelectedItems.Count(y => y.IsUnread.Value) > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			SelectedItemsMarkAsReadCommand.Subscribe(async _ =>
			{
				foreach (var item in SelectedItems)
				{
					await HohoemaApp.FeedManager.MarkAsRead(item.VideoId);
					await HohoemaApp.FeedManager.MarkAsRead(item.RawVideoId);
				}

				ClearSelection();
			})
			.AddTo(_CompositeDisposable);

			OpenFeedGroupPageCommand = new DelegateCommand(() => 
			{
				if (FeedGroup != null)
				{
					PageManager.OpenPage(HohoemaPageType.FeedGroup, FeedGroup.Id);
				}
			});
		}

		protected override Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is Guid)
			{
				var feedGroupId = (Guid)e.Parameter;

				FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(feedGroupId);
			}

			if (FeedGroup == null)
			{
				// 削除済み？
				PageManager.OpenPage(HohoemaPageType.FeedGroupManage);
			}
			else
			{
				UpdateTitle(FeedGroup.Label);

				AllMarkAsReadCommand.RaiseCanExecuteChanged();
			}

			return Task.CompletedTask;
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (FeedGroup?.IsNeedRefresh ?? false)
			{
				return true;
			}
			return base.CheckNeedUpdateOnNavigateTo(mode);
		}

		protected override IIncrementalSource<FeedVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new FeedVideoIncrementalSource(FeedGroup, HohoemaApp, PageManager);
		}


		public DelegateCommand AllMarkAsReadCommand { get; private set; }
		public ReactiveCommand SelectedItemsMarkAsReadCommand { get; private set; }

		public DelegateCommand OpenFeedGroupPageCommand { get; private set; }
	}


	public class FeedVideoIncrementalSource : HohoemaVideoPreloadingIncrementalSourceBase<FeedVideoInfoControlViewModel>
	{
		FeedManager _FavFeedManager;
		NiconicoMediaManager _NiconicoMediaManager;
		PageManager _PageManager;
		FeedGroup _FeedGroup;


		public FeedVideoIncrementalSource(FeedGroup feedGroup, HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, "Feed_" + feedGroup.Label)
		{
			_FeedGroup = feedGroup;
			_FavFeedManager = hohoemaApp.FeedManager;
			_NiconicoMediaManager = hohoemaApp.MediaManager;
			_PageManager = pageManager;
		}



		#region Implements HohoemaPreloadingIncrementalSourceBase		


		protected override Task<IEnumerable<string>> PreloadVideoIds(int start, int count)
		{
			var items = _FeedGroup.FeedItems.Skip(start).Take(count).Select(x => x.VideoId);
			return Task.FromResult(items);
		}


		protected override async Task<int> ResetSourceImpl()
		{
			await _FeedGroup.Refresh();

			return _FeedGroup.FeedItems.Count;
		}


		protected override FeedVideoInfoControlViewModel NicoVideoToTemplatedItem(NicoVideo itemSource, int index)
		{
			var currentItem = _FeedGroup.FeedItems.ElementAtOrDefault(index);
			return new FeedVideoInfoControlViewModel(currentItem, _FeedGroup, itemSource, _PageManager);
		}

		#endregion
	}
}
