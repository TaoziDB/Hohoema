﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Microsoft.Toolkit.Uwp.UI;
using Mntone.Nico2.Channels.Video;
using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.Models;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
    public sealed class RelatedVideosSidePaneContentViewModel : SidePaneContentViewModelBase
    {
        string CurrentVideoId { get; }
        public List<VideoInfoControlViewModel> Videos { get; private set; }

        public VideoInfoControlViewModel CurrentVideo { get; private set; }

        Models.VideoRelatedInfomation _VideoViewerHelpInfo;

        public NicoVideo Video { get; }

        private string _JumpVideoId { get; }

        public RelatedVideosSidePaneContentViewModel(NicoVideo video, string jumpVideoId)
        {
            Video = video;
            _JumpVideoId = jumpVideoId;
            CurrentVideoId = video.RawVideoId;
            _VideoViewerHelpInfo = video.GetVideoRelatedInfomationWithVideoDescription(); ;

            HasVideoDescription = _VideoViewerHelpInfo != null;

            var _ = InitializeRelatedVideos();
        }

        public bool HasVideoDescription { get; private set; }
        public ObservableCollection<VideoInfoControlViewModel> OtherVideos { get; } = new ObservableCollection<VideoInfoControlViewModel>();
        public VideoInfoControlViewModel NextVideo { get; private set; }

        public VideoInfoControlViewModel JumpVideo { get; private set; }

        public ObservableCollection<MylistGroupListItem> Mylists { get; } = new ObservableCollection<MylistGroupListItem>();

        public AsyncLock _InitializeLock = new AsyncLock();

        private bool _IsInitialized = false;
        const double _SeriesVideosTitleSimilarityValue = 0.7;

        public async Task InitializeRelatedVideos()
        {
            if (!HasVideoDescription) { return; }

            using (var releaser = await _InitializeLock.LockAsync())
            {
                if (_IsInitialized) { return; }

                var hohoemaApp = App.Current.Container.Resolve<Models.HohoemaApp>();

                // ニコスクリプトで指定されたジャンプ先動画
                if (_JumpVideoId != null)
                {
                    var video = await hohoemaApp.ContentProvider.GetNicoVideoInfo(_JumpVideoId, requireLatest: true);
                    if (video != null)
                    {
                        JumpVideo = new VideoInfoControlViewModel(video, eventScheduler: CurrentWindowContextScheduler);
                        RaisePropertyChanged(nameof(JumpVideo));
                    }
                }

                // 再生中アイテムのタイトルと投稿者説明文に含まれる動画IDの動画タイトルを比較して
                // タイトル文字列が近似する動画をシリーズ動画として取り込む
                // 違うっぽい動画も投稿者が提示したい動画として確保
                var sourceVideo = Database.NicoVideoDb.Get(CurrentVideoId);
                var videoIds = _VideoViewerHelpInfo.GetVideoIds();
                List<Database.NicoVideo> seriesVideos = new List<Database.NicoVideo>();
                seriesVideos.Add(sourceVideo);
                foreach (var id in videoIds)
                {
                    var video = await hohoemaApp.ContentProvider.GetNicoVideoInfo(id, requireLatest: true);

                    var titleSimilarity = sourceVideo.Title.CalculateSimilarity(video.Title);
                    if (titleSimilarity > _SeriesVideosTitleSimilarityValue)
                    {
                        seriesVideos.Add(video);
                    }
                    else
                    {
                        OtherVideos.Add(new VideoInfoControlViewModel(video, requireLatest: false, eventScheduler: CurrentWindowContextScheduler));
                    }
                }


                // シリーズ動画として集めたアイテムを投稿日が新しいものが最後尾になるよう並び替え
                // シリーズ動画に番兵として仕込んだ現在再生中のアイテムの位置と動画数を比べて
                // 動画数が上回っていた場合は次動画が最後尾にあると決め打ちで取得する
                var orderedSeriesVideos = seriesVideos.OrderBy(x => x.PostedAt).ToList();
                var currentVideoIndex = orderedSeriesVideos.IndexOf(sourceVideo);
                if (orderedSeriesVideos.Count - 1 > currentVideoIndex)
                {
                    var nextVideo = orderedSeriesVideos.Last();
                    NextVideo = new VideoInfoControlViewModel(nextVideo, requireLatest: false, eventScheduler: CurrentWindowContextScheduler);
                    orderedSeriesVideos.Remove(nextVideo);

                    RaisePropertyChanged(nameof(NextVideo));
                }

                // 次動画を除いてシリーズ動画っぽいアイテムを投稿者が提示したい動画として優先表示されるようにする
                orderedSeriesVideos.Remove(sourceVideo);
                orderedSeriesVideos.Reverse();
                foreach (var video in orderedSeriesVideos)
                {
                    OtherVideos.Insert(0, new VideoInfoControlViewModel(video, requireLatest: false, eventScheduler: CurrentWindowContextScheduler));
                }

                RaisePropertyChanged(nameof(OtherVideos));


                // チャンネル動画で次動画が見つからなかった場合は
                // チャンネル動画一覧から次動画をサジェストする
                if (sourceVideo.Owner.UserType == Mntone.Nico2.Videos.Thumbnail.UserType.Channel 
                    && NextVideo == null
                    )
                {
                    // DBからチャンネル情報を取得
                    var db_channelInfo = Database.NicoChannelInfoDb.GetFromRawId(sourceVideo.Owner.OwnerId);
                    if (db_channelInfo == null)
                    {
                        db_channelInfo = new Database.NicoChannelInfo()
                        {
                            RawId = sourceVideo.Owner.OwnerId,
                            ThumbnailUrl = sourceVideo.Owner.IconUrl,
                            Name = sourceVideo.Owner.ScreenName
                        };
                    }

                    // チャンネル動画の一覧を取得する
                    // ページアクセスが必要なので先頭ページを取って
                    // 全体の分量を把握してから全ページ取得を行う
                    List<ChannelVideoInfo> channelVideos = new List<ChannelVideoInfo>();
                    var channelVideosFirstPage = await hohoemaApp.ContentProvider.GetChannelVideo(sourceVideo.Owner.OwnerId, 0);
                    var uncheckedCount = channelVideosFirstPage.TotalCount - channelVideosFirstPage.Videos.Count;
                    if (channelVideosFirstPage.TotalCount != 0)
                    {
                        channelVideos.AddRange(channelVideosFirstPage.Videos);

                        var uncheckedPageCount = (int)Math.Ceiling((double)uncheckedCount / 20); /* チャンネル動画１ページ = 20 動画 */
                        foreach (var page in Enumerable.Range(1, uncheckedPageCount))
                        {
                            var channelVideoInfo = await hohoemaApp.ContentProvider.GetChannelVideo(sourceVideo.Owner.OwnerId, page);
                            channelVideos.AddRange(channelVideoInfo.Videos);
                        }

                        db_channelInfo.Videos = channelVideos;
                    }

                    Database.NicoChannelInfoDb.AddOrUpdate(db_channelInfo);


                    var collectionView = new AdvancedCollectionView(db_channelInfo.Videos);
                    collectionView.SortDescriptions.Add(new SortDescription(nameof(ChannelVideoInfo.PostedAt), SortDirection.Ascending));
                    collectionView.SortDescriptions.Add(new SortDescription(nameof(ChannelVideoInfo.ItemId), SortDirection.Ascending));
                    collectionView.RefreshSorting();

                    var item = collectionView.FirstOrDefault(x => (x as ChannelVideoInfo).Title == sourceVideo.Title);
                    var pos = collectionView.IndexOf(item);
                    if (pos >= 0)
                    {
                        var nextVideo = collectionView.ElementAtOrDefault(pos + 1) as ChannelVideoInfo;
                        if (nextVideo != null)
                        {
                            NextVideo = new ChannelVideoListItemViewModel(nextVideo.ItemId, nextVideo.IsRequirePayment);
                            NextVideo.SetTitle(nextVideo.Title);

                            RaisePropertyChanged(nameof(NextVideo));
                        }
                    }
                }

                // マイリスト
                var pageManager = App.Current.Container.Resolve<Models.PageManager>();
                var relatedMylistIds = _VideoViewerHelpInfo.GetMylistIds();
                foreach (var mylistId in relatedMylistIds)
                {
                    var mylistDetails = await hohoemaApp.ContentProvider.GetMylistGroupDetail(mylistId);
                    if (mylistDetails.IsOK)
                    {
                        Mylists.Add(new MylistGroupListItem(mylistDetails.MylistGroup, pageManager));
                    }
                }

                RaisePropertyChanged(nameof(Mylists));

                var videos = await Video.GetRelatedVideos();
                Videos = videos.Select(x => new VideoInfoControlViewModel(x, requireLatest: false, eventScheduler:CurrentWindowContextScheduler)).ToList();
                CurrentVideo = Videos.FirstOrDefault(x => x.RawVideoId == CurrentVideoId);

                RaisePropertyChanged(nameof(Videos));
                RaisePropertyChanged(nameof(CurrentVideo));


                _IsInitialized = true;
            }
        }
    }
}
