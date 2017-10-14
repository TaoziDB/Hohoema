﻿using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
    public class SettingsSidePaneContentViewModel : SidePaneContentViewModelBase
    {

        // Video Settings
        public static List<ValueWithAvairability<NicoVideoQuality>> VideoPlayingQualityList { get; } = new []
        {
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Dmc_High),
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Dmc_Midium),
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Dmc_Low),
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Dmc_Mobile),
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Original),
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Low),
        }.ToList();
        public ReactiveProperty<ValueWithAvairability<NicoVideoQuality>> VideoPlayingQuality { get; private set; }
        public ReactiveProperty<bool> IsLowLatency { get; private set; }

        public ReactiveProperty<double> VideoPlaybackRate { get; private set; }
        public ReactiveCommand<double?> SetPlaybackRateCommand { get; private set; }

        // Live Settings
        public static List<ValueWithAvairability<string>> LivePlayingQualityList { get; } = new[]
        {
            new ValueWithAvairability<string>("high"),
            new ValueWithAvairability<string>("normal"),
            new ValueWithAvairability<string>("low"),
            new ValueWithAvairability<string>("super_low"),
        }.ToList();
        public ReactiveProperty<ValueWithAvairability<string>> LiveVideoPlayingQuality { get; private set; }
        private bool _IsLeoPlayerLive;
        public bool IsLeoPlayerLive
        {
            get { return _IsLeoPlayerLive; }
            set { SetProperty(ref _IsLeoPlayerLive, value); }
        }

        // Player Settings
        public ReactiveProperty<bool> IsForceLandscapeDefault { get; private set; }

        public ReactiveProperty<bool> IsKeepDisplayInPlayback { get; private set; }
        public ReactiveProperty<double> ScrollVolumeFrequency { get; private set; }

        public ReactiveProperty<double> AutoHideDelayTime { get; private set; }

        public DelegateCommand ResetDefaultPlaybackRateCommand { get; private set; }




        public ReactiveProperty<bool> IsDefaultCommentWithAnonymous { get; private set; }
        public ReactiveProperty<uint> CommentRenderingFPS { get; private set; }
        public ReactiveProperty<double> CommentDisplayDuration { get; private set; }
        public ReactiveProperty<double> CommentFontScale { get; private set; }
        public ReactiveProperty<Color> CommentColor { get; private set; }
        public ReactiveProperty<bool> IsPauseWithCommentWriting { get; private set; }

        public static List<CommentOpacityKind> CommentOpacityList { get; private set; }
        public ReactiveProperty<CommentOpacityKind> CommentOpacity { get; private set; }


        public static List<Color> CommentColorList { get; private set; }
        public static List<uint> CommentRenderringFPSList { get; private set; }


        public ReactiveProperty<bool> IsEnableOwnerCommentCommand { get; private set; }
        public ReactiveProperty<bool> IsEnableUserCommentCommand { get; private set; }
        public ReactiveProperty<bool> IsEnableAnonymousCommentCommand { get; private set; }


        public static List<PlaylistEndAction> PlaylistEndActionList { get; private set; }
        public ReactiveProperty<PlaylistEndAction> PlaylistEndAction { get; private set; }


        // NG Comments

        public ReactiveProperty<bool> NGCommentUserIdEnable { get; private set; }
        public ReadOnlyReactiveCollection<RemovableListItem<string>> NGCommentUserIds { get; private set; }

        public ReactiveProperty<bool> NGCommentKeywordEnable { get; private set; }
        public ReactiveProperty<string> NGCommentKeywords { get; private set; }
        public ReadOnlyReactiveProperty<string> NGCommentKeywordError { get; private set; }

        public List<NGCommentScore> NGCommentScoreTypes { get; private set; }
        public ReactiveProperty<NGCommentScore> SelectedNGCommentScore { get; private set; }


        public ReactiveProperty<bool> CommentGlassMowerEnable { get; private set; }



        private NGSettings _NGSettings;

        private PlayerSettings _PlayerSettings;
        private PlaylistSettings _PlaylistSettings;

        static SettingsSidePaneContentViewModel()
        {
            CommentRenderringFPSList = new List<uint>()
            {
                5, 10, 15, 24, 30, 45, 60, 75, 90, 120
            };

            CommentColorList = new List<Color>()
            {
                Colors.WhiteSmoke,
                Colors.Black,
            };

            PlaylistEndActionList = new List<Models.PlaylistEndAction>()
            {
                Models.PlaylistEndAction.None,
                Models.PlaylistEndAction.ChangeIntoSplit,
                Models.PlaylistEndAction.CloseIfPlayWithCurrentWindow
            };

            CommentOpacityList = new List<CommentOpacityKind>()
            {
                CommentOpacityKind.NoSukesuke,
                CommentOpacityKind.BitSukesuke,
                CommentOpacityKind.MoreSukesuke
            };
        }

        public SettingsSidePaneContentViewModel(HohoemaUserSettings settings)
        {
            _NGSettings = settings.NGSettings;
            _PlayerSettings = settings.PlayerSettings;
            _PlaylistSettings = settings.PlaylistSettings;

            VideoPlayingQuality = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultQuality, 
                convert: x => VideoPlayingQualityList.First(y => y.Value == x),
                convertBack: x => x.Value,
                raiseEventScheduler: CurrentWindowContextScheduler,
                mode: ReactivePropertyMode.DistinctUntilChanged
                );

            VideoPlaybackRate = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.PlaybackRate);
            SetPlaybackRateCommand = VideoPlaybackRate.Select(
                rate => rate != 1.0
                )
                .ToReactiveCommand<double?>(CurrentWindowContextScheduler);

            SetPlaybackRateCommand.Subscribe(
                (rate) => VideoPlaybackRate.Value = rate.HasValue ? rate.Value : 1.0
                );
            


            LiveVideoPlayingQuality = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultLiveQuality,
                convert: x => LivePlayingQualityList.FirstOrDefault(y => y.Value == x),
                convertBack: x => x.Value,
                raiseEventScheduler: CurrentWindowContextScheduler,
                mode: ReactivePropertyMode.DistinctUntilChanged
                );

            IsLowLatency = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.LiveWatchWithLowLatency, CurrentWindowContextScheduler, mode: ReactivePropertyMode.DistinctUntilChanged);

            IsKeepDisplayInPlayback = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsKeepDisplayInPlayback, CurrentWindowContextScheduler);
            ScrollVolumeFrequency = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.SoundVolumeChangeFrequency, CurrentWindowContextScheduler);
            IsForceLandscapeDefault = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsForceLandscape, CurrentWindowContextScheduler);

            AutoHideDelayTime = _PlayerSettings.ToReactivePropertyAsSynchronized(x =>
                x.AutoHidePlayerControlUIPreventTime
                , x => x.TotalSeconds
                , x => TimeSpan.FromSeconds(x)
                , CurrentWindowContextScheduler
                );

            PlaylistEndAction = _PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.PlaylistEndAction, CurrentWindowContextScheduler);

            // NG Comment User Id



            // Comment Display 
            CommentColor = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentColor, CurrentWindowContextScheduler);
            IsPauseWithCommentWriting = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting, CurrentWindowContextScheduler);
            CommentRenderingFPS = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentRenderingFPS, CurrentWindowContextScheduler);
            CommentDisplayDuration = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentDisplayDuration, x => x.TotalSeconds, x => TimeSpan.FromSeconds(x), CurrentWindowContextScheduler);
            CommentFontScale = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentFontScale, CurrentWindowContextScheduler);
            IsDefaultCommentWithAnonymous = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsDefaultCommentWithAnonymous, CurrentWindowContextScheduler);
            CommentOpacity = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentOpacity, CurrentWindowContextScheduler);

            IsEnableOwnerCommentCommand = new ReactiveProperty<bool>(CurrentWindowContextScheduler, _PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.Owner));
            IsEnableUserCommentCommand = new ReactiveProperty<bool>(CurrentWindowContextScheduler, _PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.User));
            IsEnableAnonymousCommentCommand = new ReactiveProperty<bool>(CurrentWindowContextScheduler, _PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.Anonymous));

            IsEnableOwnerCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.Owner));
            IsEnableUserCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.User));
            IsEnableAnonymousCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.Anonymous));



            // NG Comment

            NGCommentUserIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentUserIdEnable, CurrentWindowContextScheduler);
            NGCommentUserIds = _NGSettings.NGCommentUserIds
                .ToReadOnlyReactiveCollection(x =>
                    RemovableSettingsListItemHelper.UserIdInfoToRemovableListItemVM(x, OnRemoveNGCommentUserIdFromList),
                    CurrentWindowContextScheduler
                    );

            NGCommentKeywordEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentKeywordEnable, CurrentWindowContextScheduler);
            NGCommentKeywords = new ReactiveProperty<string>(CurrentWindowContextScheduler, string.Empty);

            NGCommentKeywordError = NGCommentKeywords
                .Select(x =>
                {
                    var keywords = x.Split('\r');
                    var invalidRegex = keywords.FirstOrDefault(keyword =>
                    {
                        Regex regex = null;
                        try
                        {
                            regex = new Regex(keyword);
                        }
                        catch { }
                        return regex == null;
                    });

                    if (invalidRegex == null)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return $"Error in \"{invalidRegex}\"";
                    }
                })
                .ToReadOnlyReactiveProperty(eventScheduler: CurrentWindowContextScheduler);

            NGCommentScoreTypes = ((IEnumerable<NGCommentScore>)Enum.GetValues(typeof(NGCommentScore))).ToList();

            SelectedNGCommentScore = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentScoreType, CurrentWindowContextScheduler);



            CommentGlassMowerEnable = _PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.CommentGlassMowerEnable, CurrentWindowContextScheduler);


        }



        public void SetupAvairableVideoQualities(IList<NicoVideoQuality> qualities)
        {
            foreach (var i in VideoPlayingQualityList)
            {
                i.IsAvairable = qualities.Any(x => x == i.Value);
            }
        }
        public void SetupAvairableLiveQualities(IList<string> qualities)
        {
            if (qualities == null) { return; }

            foreach (var i in LivePlayingQualityList)
            {
                i.IsAvairable = qualities.Any(x => x == i.Value);
            }
        }



        private void SetCommentCommandPermission(bool isEnable, CommentCommandPermissionType type)
        {
            if (isEnable)
            {
                _PlayerSettings.CommentCommandPermission |= type;
            }
            else
            {
                _PlayerSettings.CommentCommandPermission = _PlayerSettings.CommentCommandPermission & ~type;
            }
        }

        // TODO: Dispose
        protected override void OnDispose()
        {

        }


        public override Task OnEnter()
        {
            NGCommentKeywords.Value = string.Join("\r", _NGSettings.NGCommentKeywords.Select(x => x.Keyword)) + "\r";

            return base.OnEnter();
        }

        public override void OnLeave()
        {
            // NG Comments
            _NGSettings.NGCommentKeywords.Clear();
            foreach (var ngKeyword in NGCommentKeywords.Value.Split('\r'))
            {
                if (!string.IsNullOrWhiteSpace(ngKeyword))
                {
                    _NGSettings.NGCommentKeywords.Add(new NGKeyword() { Keyword = ngKeyword });
                }
            }
            _NGSettings.Save().ConfigureAwait(false);

            base.OnLeave();
        }
        private void OnRemoveNGCommentUserIdFromList(string userId)
        {
            var removeTarget = _NGSettings.NGCommentUserIds.First(x => x.UserId == userId);
            _NGSettings.NGCommentUserIds.Remove(removeTarget);
        }

    }


    public class ValueWithAvairability<T> : BindableBase
    {
        public ValueWithAvairability(T value, bool isAvairable = true)
        {
            Value = value;
            IsAvairable = isAvairable;
        }
        public T Value { get; set; }

        private bool _IsAvairable;
        public bool IsAvairable
        {
            get { return _IsAvairable; }
            set { SetProperty(ref _IsAvairable, value); }
        }
    }

}
