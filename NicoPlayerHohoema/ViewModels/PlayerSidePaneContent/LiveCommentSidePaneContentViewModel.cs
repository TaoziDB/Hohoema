﻿using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Views;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
	public class LiveCommentSidePaneContentViewModel : SidePaneContentViewModelBase
	{

		public LiveCommentSidePaneContentViewModel(HohoemaUserSettings settings, Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView comments)
		{
			UserSettings = settings;
			Comments = comments;
			IsCommentListScrollWithVideo = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
				.AddTo(_CompositeDisposable);

            NGUsers = new ReadOnlyObservableCollection<NGUserIdInfo>(UserSettings.NGSettings.NGLiveCommentUserIds);
            IsNGCommentUserIdEnabled = UserSettings.NGSettings.ToReactivePropertyAsSynchronized(x => x.IsNGLiveCommentUserEnable, CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);
        }


		public void UpdatePlayPosition(uint videoPosition)
		{
			if (IsCommentListScrollWithVideo.Value)
			{
				// 表示位置の更新
			}
		}

		public ReactiveProperty<bool> IsCommentListScrollWithVideo { get; private set; }

		public HohoemaUserSettings UserSettings { get; private set; }
        public ReactiveProperty<bool> IsNGCommentUserIdEnabled { get; private set; }


        public ReadOnlyObservableCollection<Models.NGUserIdInfo> NGUsers { get; }

        /*
        ReadOnlyObservableCollection<Comment> _Comments;

        public ReadOnlyObservableCollection<Comment> Comments
        {
            get { return _Comments; }
            set { SetProperty(ref _Comments, value); }
        }
        */


        Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView _Comments;

        public Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView Comments
        {
            get { return _Comments; }
            set { SetProperty(ref _Comments, value); }
        }
    }
}
