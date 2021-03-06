﻿using NicoPlayerHohoema.ViewModels;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Prism.Unity.Windows;
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Models;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Prism.Events;
using NicoPlayerHohoema.Events;
using Prism.Windows.Navigation;
using Prism.Windows.AppModel;
using Prism.Windows.Mvvm;
//using BackgroundAudioShared;
using Windows.Media;
using Windows.Storage;
using System.Text;
using NicoPlayerHohoema.Helpers;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.DataTransfer;
using Mntone.Nico2;
using Prism.Commands;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.UI;

namespace NicoPlayerHohoema
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Prism.Unity.Windows.PrismUnityApplication
    {
		public HohoemaViewManager PlayerWindow { get; private set; }

		private bool _IsPreLaunch;

		public const string ACTIVATION_WITH_ERROR = "error";

        public SplashScreen SplashScreen { get; private set; }
        const bool _DEBUG_XBOX_RESOURCE = true;

        internal const string IS_COMPLETE_INTRODUCTION = "is_first_launch";

        public void PublishInAppNotification(InAppNotificationPayload payload)
        {
            var notificationEvent = EventAggregator.GetEvent<InAppNotificationEvent>();
            notificationEvent.Publish(payload);
        }

        public void DismissInAppNotification()
        {
            var notificationDismissEvent = EventAggregator.GetEvent<InAppNotificationDismissEvent>();
            notificationDismissEvent.Publish(0);
        }



        static App()
		{
		}

		/// <summary>
		/// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
		///最初の行であるため、main() または WinMain() と論理的に等価です。
		/// </summary>
		public App()
        {
			UnhandledException += PrismUnityApplication_UnhandledException;

			this.Resuming += App_Resuming;
            //			this.Suspending += App_Suspending;
            this.EnteredBackground += App_EnteredBackground;
            this.LeavingBackground += App_LeavingBackground;
            Windows.System.MemoryManager.AppMemoryUsageLimitChanging += MemoryManager_AppMemoryUsageLimitChanging;

            this.RequiresPointerMode = Windows.UI.Xaml.ApplicationRequiresPointerMode.WhenRequested;

            RequestedTheme = GetTheme();

            Microsoft.Toolkit.Uwp.UI.ImageCache.Instance.CacheDuration = TimeSpan.FromDays(7);
            Microsoft.Toolkit.Uwp.UI.ImageCache.Instance.MaxMemoryCacheCount = 200;
            Microsoft.Toolkit.Uwp.UI.ImageCache.Instance.RetryCount = 3;


            this.InitializeComponent();

        }

        private async void CoreWindow_Activated(CoreWindow sender, WindowActivatedEventArgs args)
        {
            await CheckClipboard();
        }

        private void MemoryManager_AppMemoryUsageLimitChanging(object sender, Windows.System.AppMemoryUsageLimitChangingEventArgs e)
        {
            Debug.WriteLine($"Memory Limit: {e.OldLimit} -> {e.NewLimit}");
        }

        private void App_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            Debug.WriteLine("Leave BG");
        }

        private void App_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            Debug.WriteLine("Enter BG");
        }

      
        /*
		private async void App_Suspending(object sender, SuspendingEventArgs e)
		{
			
			var deferral = e.SuspendingOperation.GetDeferral();
			var hohoemaApp = Container.Resolve<HohoemaApp>();
			await hohoemaApp.OnSuspending();

			deferral.Complete();
		}
		*/

        const string ThemeTypeKey = "Theme";

		public static void SetTheme(ApplicationTheme theme)
		{
			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(ThemeTypeKey))
			{
				ApplicationData.Current.LocalSettings.Values[ThemeTypeKey] = theme.ToString();
			}
			else
			{
				ApplicationData.Current.LocalSettings.Values.Add(ThemeTypeKey, theme.ToString());
			}
		}

		public static ApplicationTheme GetTheme()
		{
			try
			{
				if (ApplicationData.Current.LocalSettings.Values.ContainsKey(ThemeTypeKey))
				{
					return (ApplicationTheme)Enum.Parse(typeof(ApplicationTheme), (string)ApplicationData.Current.LocalSettings.Values[ThemeTypeKey]);
				}
			}
			catch { }

			return ApplicationTheme.Dark;
		}

		protected override async Task OnSuspendingApplicationAsync()
		{
			if (_IsPreLaunch) { return; }

			var hohoemaApp = Container.Resolve<HohoemaApp>();
			
			// Note: ここで呼び出すとスレッドロックが発生するので
			// HohoemaViewModelBaseのNavigationFrom内でサスペンドの処理を行っています

//			await hohoemaApp.OnSuspending().ConfigureAwait(false);
//			await HohoemaApp.SyncToRoamingData().ConfigureAwait(false);

			
			await base.OnSuspendingApplicationAsync();
		}
		

		private async void App_Resuming(object sender, object e)
		{
			if (_IsPreLaunch) { return; }

			//			var backgroundTask = MediaBackgroundTask.Create();
			//			Container.RegisterInstance(backgroundTask);
			
			var hohoemaApp = Container.Resolve<HohoemaApp>();

			try
			{
				hohoemaApp.Resumed();
			}
			catch
			{
				Debug.WriteLine("アプリモデルの復帰処理でエラーを検出しました。");
				throw;
			}

			try
			{
//				await CheckVideoCacheFolderState();
			}
			catch
			{
				Debug.WriteLine("キャッシュフォルダチェックに失敗しました。");
			}


		}

        protected override Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
        {
            SplashScreen = args.SplashScreen;
#if DEBUG
            DebugSettings.IsBindingTracingEnabled = true;
#endif
            _IsPreLaunch = args.PrelaunchActivated;

            return Task.CompletedTask;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (args.PreviousExecutionState != ApplicationExecutionState.Running
                )
            {
                if (Helpers.DeviceTypeHelper.IsXbox)
                {
                    Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetDesiredBoundsMode
                        (Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);
                }
                else
                {
                    // モバイルで利用している場合に、ナビゲーションバーなどがページに被さらないように指定
                    ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
                }

                Window.Current.CoreWindow.Activated += CoreWindow_Activated;
            }

            base.OnLaunched(args);
        }

        
        protected override async Task OnActivateApplicationAsync(IActivatedEventArgs args)
		{
            var pageManager = Container.Resolve<PageManager>();
            var hohoemaApp = Container.Resolve<HohoemaApp>();

            try
            {
                if (!hohoemaApp.IsLoggedIn && AccountManager.HasPrimaryAccount())
                {
                    await hohoemaApp.SignInWithPrimaryAccount();
                }
            }
            catch { }

            // ログインしていない場合、
            bool isNeedNavigationDefault = !hohoemaApp.IsLoggedIn;

            try
            {
                if (args.Kind == ActivationKind.ToastNotification)
                {
                    //Get the pre-defined arguments and user inputs from the eventargs;
                    var toastArgs = args as IActivatedEventArgs as ToastNotificationActivatedEventArgs;
                    var arguments = toastArgs.Argument;


                    if (arguments == ACTIVATION_WITH_ERROR)
                    {
                        await ShowErrorLog().ConfigureAwait(false);
                    }
                    else if (arguments.StartsWith("cache_cancel"))
                    {
                        var query = arguments.Split('?')[1];
                        var decode = new WwwFormUrlDecoder(query);

                        var videoId = decode.GetFirstValueByName("id");
                        var quality = (NicoVideoQuality)Enum.Parse(typeof(NicoVideoQuality), decode.GetFirstValueByName("quality"));

                        await hohoemaApp.CacheManager.CancelCacheRequest(videoId, quality);
                    }
                    else
                    {
                        var nicoContentId = Helpers.NicoVideoExtention.UrlToVideoId(arguments);

                        if (Mntone.Nico2.NiconicoRegex.IsVideoId(nicoContentId))
                        {
                            await PlayVideoFromExternal(nicoContentId);
                        }
                        else if (Mntone.Nico2.NiconicoRegex.IsLiveId(nicoContentId))
                        {
                            await PlayLiveVideoFromExternal(nicoContentId);
                        }
                    }
                }
                else if (args.Kind == ActivationKind.Protocol)
                {
                    var param = (args as IActivatedEventArgs) as ProtocolActivatedEventArgs;
                    var uri = param.Uri;
                    var maybeNicoContentId = new string(uri.OriginalString.Skip("niconico://".Length).TakeWhile(x => x != '?' && x != '/').ToArray());


                    if (Mntone.Nico2.NiconicoRegex.IsVideoId(maybeNicoContentId)
                        || maybeNicoContentId.All(x => x >= '0' && x <= '9'))
                    {
                        await PlayVideoFromExternal(maybeNicoContentId);
                    }
                    else if (Mntone.Nico2.NiconicoRegex.IsLiveId(maybeNicoContentId))
                    {
                        await PlayLiveVideoFromExternal(maybeNicoContentId);
                    }
                }
                else
                {
                    if (hohoemaApp.IsLoggedIn)
                    {
                        pageManager.OpenStartupPage();
                    }
                    else
                    {
                        pageManager.OpenPage(HohoemaPageType.RankingCategoryList);
                    }
                }
            }
            catch
            {
                /*
                if (!pageManager.NavigationService.CanGoBack())
                {
                    if (!hohoemaApp.IsLoggedIn && AccountManager.HasPrimaryAccount())
                    {
                        await hohoemaApp.SignInWithPrimaryAccount();

                        pageManager.OpenStartupPage();
                    }
                    else
                    {
                        pageManager.OpenPage(HohoemaPageType.Login);
                    }
                }
                */
            }
			


			await base.OnActivateApplicationAsync(args);
		}

        private async Task PlayVideoFromExternal(string videoId, string videoTitle = null, NicoVideoQuality? quality = null)
        {
            var hohoemaApp = Container.Resolve<HohoemaApp>();
            var pageManager = Container.Resolve<PageManager>();

            // TODO: ログインが必要な動画かをチェックしてログインダイアログを出す

            hohoemaApp.Playlist.PlayVideo(videoId, videoTitle, quality);
        }
        private async Task PlayLiveVideoFromExternal(string videoId)
        {
            // TODO: ログインが必要な生放送かをチェックしてログインダイアログを出す


            var pageManager = Container.Resolve<PageManager>();
            var hohoemaApp = Container.Resolve<HohoemaApp>();

            hohoemaApp.Playlist.PlayLiveVideo(videoId);
        }

        protected override void OnActivated(IActivatedEventArgs args)
		{

            base.OnActivated(args);
		}

		public async Task<string> GetMostRecentErrorText()
		{
			var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("error", CreationCollisionOption.OpenIfExists);
			var errorFiles = await folder.GetItemsAsync();

			var errorFile = errorFiles
				.Where(x => x.Name.StartsWith("hohoema_error"))
				.OrderBy(x => x.DateCreated)
				.LastOrDefault()
				as StorageFile;

			if (errorFile != null)
			{
				return await FileIO.ReadTextAsync(errorFile);
			}
			else
			{
				return null;
			}
		}
		private async Task ShowErrorLog()
		{
			var text = await GetMostRecentErrorText();

			if (text != null)
			{ 
				var contentDialog = new ContentDialog();
				contentDialog.Title = "Hohoemaで発生したエラー詳細";
				contentDialog.PrimaryButtonText = "OK";
				contentDialog.Content = new TextBox()
				{
					Text = text,
					IsReadOnly = true,
					TextWrapping = TextWrapping.Wrap,
				};

				await contentDialog.ShowAsync().AsTask();
			}
		}

		/// <summary>
		/// 動画キャッシュ保存先フォルダをチェックします
		/// 選択済みだがフォルダが見つからない場合に、トースト通知を行います。
		/// </summary>
		/// <returns></returns>
		public async Task CheckVideoCacheFolderState()
		{
			var hohoemaApp = Container.Resolve<HohoemaApp>();
			var cacheFolderState = await hohoemaApp.GetVideoCacheFolderState();

			if (cacheFolderState == CacheFolderAccessState.SelectedButNotExist)
			{
				var toastService = Container.Resolve<Views.Service.ToastNotificationService>();
				toastService.ShowText(
					"キャッシュが利用できません"
					, "キャッシュ保存先フォルダが見つかりません。（ここをタップで設定画面を表示）"
					, duration: Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
					, toastActivatedAction: async () =>
					{
						await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
						{
							var pm = Container.Resolve<PageManager>();
							pm.OpenPage(HohoemaPageType.CacheManagement);
						});
					});
			}
		}


        public bool IsTitleBarCustomized { get; } = Helpers.DeviceTypeHelper.IsDesktop && Helpers.InputCapabilityHelper.IsMouseCapable;
        protected override async Task OnInitializeAsync(IActivatedEventArgs args)
		{
#if DEBUG
            if (_DEBUG_XBOX_RESOURCE)
#else
                if (Helpers.DeviceTypeHelper.IsXbox)
#endif
            {
                this.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri("ms-appx:///Styles/TVSafeColor.xaml")
                });
                this.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri("ms-appx:///Styles/TVStyle.xaml")
                });
            }



            Resources.Add("TitleBarCustomized", IsTitleBarCustomized);
            Resources.Add("TitleBarDummyHeight", IsTitleBarCustomized ? 32.0 : 0.0);

            if (IsTitleBarCustomized)
            {

                var coreApp = CoreApplication.GetCurrentView();
                coreApp.TitleBar.ExtendViewIntoTitleBar = true;

                var appView = ApplicationView.GetForCurrentView();
                appView.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appView.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                appView.TitleBar.ButtonInactiveForegroundColor = Colors.Transparent;

                if (RequestedTheme == ApplicationTheme.Light)
                {
                    appView.TitleBar.ButtonForegroundColor = Colors.Black;
                    appView.TitleBar.ButtonHoverBackgroundColor = Colors.DarkGray;
                    appView.TitleBar.ButtonHoverForegroundColor = Colors.Black;
                }
            }


            Microsoft.Toolkit.Uwp.UI.ImageCache.Instance.CacheDuration = TimeSpan.FromHours(24);

			// TwitterAPIの初期化
//			await TwitterHelper.Initialize();

			await RegisterTypes();

#if DEBUG
            Views.UINavigationManager.Pressed += UINavigationManager_Pressed;
#endif

            // ウィンドウサイズの保存と復元
            if (Helpers.DeviceTypeHelper.IsDesktop)
            {
                var localObjectStorageHelper = Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                if (localObjectStorageHelper.KeyExists(HohoemaViewManager.primary_view_size))
                {
                    var view = ApplicationView.GetForCurrentView();
                    MainViewId = view.Id;
                    _PrevWindowSize = localObjectStorageHelper.Read<Size>(HohoemaViewManager.primary_view_size);
                    view.TryResizeView(_PrevWindowSize);
                    ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
                }
            }


            var localStorge = Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();


            var pageManager = Container.Resolve<PageManager>();
            var hohoemaApp = Container.Resolve<HohoemaApp>();

            try
            {
                await hohoemaApp.InitializeAsync().ConfigureAwait(false);
            }
            catch
            {
                Debug.WriteLine("HohoemaAppの初期化に失敗");
            }


#if false
            try
            {
                if (localStorge.Read(IS_COMPLETE_INTRODUCTION, false) == false)
                {
                    // アプリのイントロダクションを開始
                    pageManager.OpenIntroductionPage();
                }
                else
                {
                    pageManager.OpenStartupPage();
                }
            }
            catch
            {
                Debug.WriteLine("イントロダクションまたはスタートアップのページ表示に失敗");
                pageManager.OpenPage(HohoemaPageType.RankingCategoryList);
            }
#else
            try
            {
                pageManager.OpenStartupPage();
            }
            catch
            {
                Debug.WriteLine("スタートアップのページ表示に失敗");
                pageManager.OpenPage(HohoemaPageType.RankingCategoryList);
            }
#endif



            try
            {
                // ログインを試行
                if (!hohoemaApp.IsLoggedIn && AccountManager.HasPrimaryAccount())
                {
                    // サインイン処理の待ちを初期化内でしないことで初期画面表示を早める
                    hohoemaApp.SignInWithPrimaryAccount();
                }
            }
            catch
            {
                Debug.WriteLine("ログイン処理に失敗");
            }

            await base.OnInitializeAsync(args);
		}


        private int MainViewId = -1;

        private void View_VisibleBoundsChanged(ApplicationView sender, object args)
        {
            if (MainViewId == sender.Id)
            {
                var localObjectStorageHelper = Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                _PrevWindowSize = localObjectStorageHelper.Read<Size>(HohoemaViewManager.primary_view_size);
                localObjectStorageHelper.Save(HohoemaViewManager.primary_view_size, new Size(sender.VisibleBounds.Width, sender.VisibleBounds.Height));

                Debug.WriteLine("MainView VisibleBoundsChanged : " + sender.VisibleBounds.ToString());
            }
        }

        protected override IDeviceGestureService OnCreateDeviceGestureService()
        {
            var dgs = base.OnCreateDeviceGestureService();

            // TitleBarCustomized に合わせて
            dgs.UseTitleBarBackButton = !IsTitleBarCustomized;
            return dgs;
        }

        private async void UINavigationManager_Pressed(Views.UINavigationManager sender, Views.UINavigationButtons buttons)
        {
            await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
            {
                if (buttons == Views.UINavigationButtons.Up ||
                buttons == Views.UINavigationButtons.Down ||
                buttons == Views.UINavigationButtons.Right ||
                buttons == Views.UINavigationButtons.Left
                )
                {
                    var focused = FocusManager.GetFocusedElement();
                    Debug.WriteLine("現在のフォーカス:" + focused?.ToString());
                }
            });
        }

        /*
        private void Context_DoneDownload(NicoVideoDownloadContext sender, NiconicoDownloadEventArgs args)
		{
			var hohoemaApp = Container.Resolve<HohoemaApp>();
			var toastService = Container.Resolve<Views.Service.ToastNotificationService>();
			var pageManager = Container.Resolve<PageManager>();

			try
			{
				var videoData = Models.Db.VideoInfoDb.Get(args.RawVideoId);

				if (videoData != null)
				{
					toastService.ShowText(
						videoData.Title,
						$"キャッシュが完了、このメッセージをタップして再生開始",
						toastActivatedAction: () =>
						{
                            hohoemaApp.Playlist.DefaultPlaylist.AddVideo(args.RawVideoId, "", args.Quality);
						}
						);
				}
			}
			catch { }
		}
        */


		private async Task RegisterTypes()
		{

            // Service
            var dialogService = new Services.HohoemaDialogService();
            Container.RegisterInstance(dialogService);

            Container.RegisterInstance(new Views.Service.ToastNotificationService());



            // Models
            var secondaryViewMan = new HohoemaViewManager();
            var hohoemaApp = await HohoemaApp.Create(EventAggregator, secondaryViewMan, dialogService);
            Container.RegisterInstance(secondaryViewMan);
            Container.RegisterInstance(hohoemaApp);
			Container.RegisterInstance(new PageManager(hohoemaApp, NavigationService, hohoemaApp.UserSettings.AppearanceSettings, hohoemaApp.Playlist, secondaryViewMan, dialogService));
            Container.RegisterInstance(hohoemaApp.ContentProvider);
            Container.RegisterInstance(hohoemaApp.Playlist);
            Container.RegisterInstance(hohoemaApp.OtherOwneredMylistManager);
            Container.RegisterInstance(hohoemaApp.FeedManager);
            Container.RegisterInstance(hohoemaApp.CacheManager);
            Container.RegisterInstance(hohoemaApp.UserSettings);
            Container.RegisterInstance(new Models.Niconico.Live.NicoLiveSubscriber(hohoemaApp));

            Container.RegisterInstance(new Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper());
#if DEBUG
            //			BackgroundUpdater.MaxTaskSlotCount = 1;
#endif
            // TODO: プレイヤーウィンドウ上で管理する
            //			var backgroundTask = MediaBackgroundTask.Create();
            //			Container.RegisterInstance(backgroundTask);


            // ViewModels
            Container.RegisterType<ViewModels.RankingCategoryListPageViewModel>(new ContainerControlledLifetimeManager());
            /*
            Container.RegisterType<ViewModels.SearchPageViewModel>(new ContainerControlledLifetimeManager());
            Container.RegisterType<ViewModels.MenuNavigatePageBaseViewModel>(new ContainerControlledLifetimeManager());
            Container.RegisterType<ViewModels.WatchHistoryPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.UserVideoPageViewModel>(new ContainerControlledLifetimeManager());
            Container.RegisterType<ViewModels.MylistPageViewModel>(new ContainerControlledLifetimeManager());
            */
            /*
                        Container.RegisterType<ViewModels.SearchPageViewModel>(new ContainerControlledLifetimeManager());
                        //			Container.RegisterType<ViewModels.UserVideoPageViewModel>(new ContainerControlledLifetimeManager());
                        Container.RegisterType<ViewModels.FeedVideoListPageViewModel>(new ContainerControlledLifetimeManager());
                        Container.RegisterType<ViewModels.UserMylistPageViewModel>(new ContainerControlledLifetimeManager());
                        Container.RegisterType<ViewModels.CacheManagementPageViewModel>(new ContainerControlledLifetimeManager());
            //			Container.RegisterType<ViewModels.PortalPageViewModel>(new ContainerControlledLifetimeManager());
            */

            Resources.Add("IsXbox", Helpers.DeviceTypeHelper.IsXbox);
            Resources.Add("IsMobile", Helpers.DeviceTypeHelper.IsMobile);

            Resources.Add("IsCacheEnabled", hohoemaApp.UserSettings.CacheSettings.IsEnableCache);
            Resources.Add("IsTVModeEnabled", Helpers.DeviceTypeHelper.IsXbox || hohoemaApp.UserSettings.AppearanceSettings.IsForceTVModeEnable);

            //			return Task.CompletedTask;
        }


        protected override void ConfigureViewModelLocator()
        {

            ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType => 
            {
                var pageToken = viewType.Name;

                if (pageToken.EndsWith("_TV"))
                {
                    pageToken = pageToken.Remove(pageToken.IndexOf("_TV"));
                }

                var assemblyQualifiedAppType = viewType.AssemblyQualifiedName;

                var pageNameWithParameter = assemblyQualifiedAppType.Replace(viewType.FullName, "NicoPlayerHohoema.ViewModels.{0}ViewModel");

                var viewModelFullName = string.Format(CultureInfo.InvariantCulture, pageNameWithParameter, pageToken);
                var viewModelType = Type.GetType(viewModelFullName);

                if (viewModelType == null)
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.InvariantCulture, pageToken, this.GetType().Namespace + ".ViewModels"),
                        "pageToken");
                }

                return viewModelType;

            });

            base.ConfigureViewModelLocator();
        }

        protected override Type GetPageType(string pageToken)
        {
            var hohoemaApp = Container.Resolve<HohoemaApp>();
            var isForceTVModeEnable = hohoemaApp?.UserSettings?.AppearanceSettings.IsForceTVModeEnable ?? false;

            if (isForceTVModeEnable || Helpers.DeviceTypeHelper.IsXbox)
            {
                // pageTokenに対応するXbox表示用のページの型を取得
                try
                {
                    var assemblyQualifiedAppType = this.GetType().AssemblyQualifiedName;

                    var pageNameWithParameter = assemblyQualifiedAppType.Replace(this.GetType().FullName, this.GetType().Namespace + ".Views.{0}Page_TV");

                    var viewFullName = string.Format(CultureInfo.InvariantCulture, pageNameWithParameter, pageToken);
                    var viewType = Type.GetType(viewFullName);

                    if (viewType == null)
                    {
                        return base.GetPageType(pageToken);
//                        throw new ArgumentException(
 //                           string.Format(CultureInfo.InvariantCulture, pageToken, this.GetType().Namespace + ".Views"),
  //                          "pageToken");
                    }

                    return viewType;
                }
                catch { }
            }

            return base.GetPageType(pageToken);
        }


        protected override void OnWindowCreated(WindowCreatedEventArgs args)
		{
			base.OnWindowCreated(args);

            //			var mainWindowView = ApplicationView.GetForCurrentView();
            //			mainWindowView.Consolidated += MainWindowView_Consolidated;

            var view = ApplicationView.GetForCurrentView();
            view.VisibleBoundsChanged += View_VisibleBoundsChanged;
            view.Consolidated += View_Consolidated;
        }

        private Size _PrevWindowSize;
        private void View_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            if (sender.Id == MainViewId)
            {
                var localObjectStorageHelper = Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                if (_PrevWindowSize != default(Size))
                {
                    localObjectStorageHelper.Save(HohoemaViewManager.primary_view_size, _PrevWindowSize);
                }
                MainViewId = -1;
            }
        }

        protected override UIElement CreateShell(Frame rootFrame)
        {
            rootFrame.CacheSize = 5;

            rootFrame.Navigating += RootFrame_Navigating;
            rootFrame.NavigationFailed += RootFrame_NavigationFailed;
            
            var menuPageBase = new Views.MenuNavigatePageBase();
            menuPageBase.Content = rootFrame;

#if DEBUG
            menuPageBase.FocusEngaged += Container_FocusEngaged;
#endif
            var grid = new Grid();

            grid.Children.Add(menuPageBase);

            var hohoemaInAppNotification = new Views.HohoemaInAppNotification();
            hohoemaInAppNotification.VerticalAlignment = VerticalAlignment.Bottom;
            grid.Children.Add(hohoemaInAppNotification);


            return grid;
		}

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Debug.WriteLine("Page navigation failed!!");
            Debug.WriteLine(e.SourcePageType.AssemblyQualifiedName);
            Debug.WriteLine(e.Exception.ToString());

            WriteErrorFile(e.Exception, e.SourcePageType?.AssemblyQualifiedName).ConfigureAwait(false);
        }

        private void Container_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            Debug.WriteLine("focus engagad: " + args.OriginalSource.ToString());
        }

        private void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
		{
            try
            {
                var playlist = Container.Resolve<HohoemaPlaylist>();
                // プレイヤーをメインウィンドウでウィンドウいっぱいに表示しているときだけ
                // バックキーの動作をUIの表示/非表示切り替えに割り当てる
                if (playlist.IsDisplayMainViewPlayer && playlist.PlayerDisplayType == PlayerDisplayType.PrimaryView)
                {
                    playlist.IsDisplayPlayerControlUI = !playlist.IsDisplayPlayerControlUI;
                    e.Cancel = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                WriteErrorFile(ex).ConfigureAwait(false);
            }

        }


		private async void PrismUnityApplication_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			e.Handled = true;

			Debug.Write(e.Message);

            if (IsDebugModeEnabled)
            {
                await WriteErrorFile(e.Exception);
            }

            //			ShowErrorToast();
        }

		public async Task WriteErrorFile(Exception e, string pageName = null)
		{
            var pageManager = Container.Resolve<PageManager>();
            if (pageName == null)
            {
                pageName = pageManager.CurrentPageType.ToString();
            }
            try
            {
				var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("error", CreationCollisionOption.OpenIfExists);
				var errorFile = await folder.CreateFileAsync($"hohoema_{pageName}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.txt", CreationCollisionOption.OpenIfExists);

				var version = Package.Current.Id.Version;
				var versionText = $"{version.Major}.{version.Minor}.{version.Build}";
				var stringBuilder = new StringBuilder();
				stringBuilder.AppendLine($"Hohoema {versionText}");
				stringBuilder.AppendLine("開いていたページ:" + pageName);
				stringBuilder.AppendLine("");
				stringBuilder.AppendLine("= = = = = = = = = = = = = = = =");
				stringBuilder.AppendLine("");
				stringBuilder.AppendLine(e.ToString());

				await FileIO.WriteTextAsync(errorFile, stringBuilder.ToString());
			}
			catch { }
		}

		public void ShowErrorToast()
		{
			var toast = Container.Resolve<Views.Service.ToastNotificationService>();
			toast.ShowText("Hohoema実行中に不明なエラーが発生しました"
				, "ここをタップすると再起動できます。"
				, Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
				, luanchContent: ACTIVATION_WITH_ERROR
				);
		}


#region Clipboard Support 

        private static readonly TimeSpan DefaultNotificationShowDuration = TimeSpan.FromSeconds(20);

        private AsyncLock _ClipboardProcessLock = new AsyncLock();
        private string prevContent = string.Empty;
        private async Task CheckClipboard()
        {
            using (var releaser = await _ClipboardProcessLock.LockAsync())
            {
                DataPackageView dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.WebLink))
                {
                    var uri = await dataPackageView.GetWebLinkAsync();
                    if (uri.OriginalString == prevContent) { return; }
                    await ExtractNicoContentId_And_SubmitSuggestion(uri);
                    prevContent = uri.OriginalString;
                }
                else if (dataPackageView.Contains(StandardDataFormats.Text))
                {
                    string text = await dataPackageView.GetTextAsync();
                    if (prevContent == text) { return; }
                    try
                    {
                        if (Uri.TryCreate(text, UriKind.Absolute, out var uri))
                        {
                            await ExtractNicoContentId_And_SubmitSuggestion(uri);
                        }
                        else
                        {
                            await ExtractNicoContentId_And_SubmitSuggestion(text);
                        }
                    }
                    catch
                    {
                        
                    }
                    prevContent = text;
                }
            }
        }




        private async Task ExtractNicoContentId_And_SubmitSuggestion(string contentId)
        {
            if (NiconicoRegex.IsVideoId(contentId))
            {
                SubmitVideoContentSuggestion(contentId);
            }
            else if (NiconicoRegex.IsLiveId(contentId))
            {
                await SubmitLiveContentSuggestion(contentId);
            }
        }

        static readonly Regex NicoContentRegex = new Regex("http:\\/\\/([\\w\\W]*?)\\/((\\w*)\\/)?([\\w-]*)");
        private async Task ExtractNicoContentId_And_SubmitSuggestion(Uri url)
        {
            var match = NicoContentRegex.Match(url.OriginalString);
            if (match.Success)
            {
                var hostNameGroup = match.Groups[1];
                var contentTypeGroup = match.Groups[3];
                var contentIdGroup = match.Groups[4];

                var contentId = contentIdGroup.Value;
                if (NiconicoRegex.IsVideoId(contentId))
                {
                    SubmitVideoContentSuggestion(contentId);
                }
                else if (NiconicoRegex.IsLiveId(contentId))
                {
                    await SubmitLiveContentSuggestion(contentId);
                }
                else if (contentTypeGroup.Success)
                {
                    var contentType = contentTypeGroup.Value;

                    switch (contentType)
                    {
                        case "mylist":
                            await SubmitMylistContentSuggestion(contentId);
                            break;
                        case "community":
                            await SubmitCommunityContentSuggestion(contentId);
                            break;
                        case "user":
                            await SubmitUserSuggestion(contentId);
                            break;

                    }
                }
                else if (hostNameGroup.Success)
                {
                    var hostName = hostNameGroup.Value;

                    if (hostName == "ch.nicovideo.jp")
                    {
                        var channelId = contentId;

                        // TODO: クリップボードから受け取ったチャンネルIdを開く
                    }
                }
            }
        }

        private async void SubmitVideoContentSuggestion(string videoId)
        {
            var contentProvider = App.Current.Container.Resolve<NiconicoContentProvider>();
            var nicoVideo = await contentProvider.GetNicoVideoInfo(videoId);

            if (nicoVideo.IsDeleted || string.IsNullOrEmpty(nicoVideo.Title)) { return; }

            PublishInAppNotification(new InAppNotificationPayload()
            {
                Content = $"{nicoVideo.Title} をお探しですか？",
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "再生",
                            Command = new DelegateCommand(() =>
                            {
                                var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();
                                hohoemaApp.Playlist.PlayVideo(nicoVideo.RawVideoId, nicoVideo.Title);

                                DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = "あとで見る",
                            Command = new DelegateCommand(() =>
                            {
                                var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();
                                hohoemaApp.Playlist.DefaultPlaylist.AddVideo(nicoVideo.RawVideoId, nicoVideo.Title);

                                DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = "動画情報を開く",
                            Command = new DelegateCommand(() =>
                            {
                                var pageManager = App.Current.Container.Resolve<PageManager>();
                                pageManager.OpenPage(HohoemaPageType.VideoInfomation, videoId);

                                DismissInAppNotification();
                            })
                        },
                    }
            });
        }


        private async Task SubmitLiveContentSuggestion(string liveId)
        {
            var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();

            var liveDesc = await hohoemaApp.NiconicoContext.Live.GetPlayerStatusAsync(liveId);

            if (liveDesc == null) { return; }

            var payload = new InAppNotificationPayload()
            {
                Content = $"{liveDesc.Program.Title} をお探しですか？",
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "視聴開始",
                            Command = new DelegateCommand(() =>
                            {
                                hohoemaApp.Playlist.PlayLiveVideo(liveId, liveDesc.Program.Title);

                                DismissInAppNotification();
                            })
                        },
                        
                    }
            };

            if (liveDesc.Program.IsCommunity)
            {
                payload.Commands.Add(new InAppNotificationCommand()
                {
                    Label = "コミュニティを開く",
                    Command = new DelegateCommand(() =>
                    {
                        var pageManager = App.Current.Container.Resolve<PageManager>();
                        pageManager.OpenPage(HohoemaPageType.Community, liveDesc.Program.CommunityId);

                        DismissInAppNotification();
                    })
                });
            }

            PublishInAppNotification(payload);
        }


        private async Task SubmitMylistContentSuggestion(string mylistId)
        {
            var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();

            Mntone.Nico2.Mylist.MylistGroup.MylistGroupDetailResponse mylistDetail = null;
            try
            {
                mylistDetail = await hohoemaApp.ContentProvider.GetMylistGroupDetail(mylistId);
            }
            catch { }

            if (mylistDetail == null || !mylistDetail.IsOK) { return; }

            var mylistGroup = mylistDetail.MylistGroup;
            PublishInAppNotification(new InAppNotificationPayload()
            {
                Content = $"{mylistGroup.Name} をお探しですか？",
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "マイリストを開く",
                            Command = new DelegateCommand(() =>
                            {
                                var pageManager = App.Current.Container.Resolve<PageManager>();
                                pageManager.OpenPage(HohoemaPageType.Mylist, new MylistPagePayload(mylistId).ToParameterString());

                                DismissInAppNotification();
                            })
                        },
                    }
            });
        }


        private async Task SubmitCommunityContentSuggestion(string communityId)
        {
            var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();

            Mntone.Nico2.Communities.Detail.CommunityDetailResponse communityDetail = null;
            try
            {
                communityDetail = await hohoemaApp.ContentProvider.GetCommunityDetail(communityId);
            }
            catch { }

            if (communityDetail == null || !communityDetail.IsStatusOK) { return; }

            var communityInfo = communityDetail.CommunitySammary.CommunityDetail;
            PublishInAppNotification(new InAppNotificationPayload()
            {
                Content = $"{communityInfo.Name} をお探しですか？",
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "コミュニティを開く",
                            Command = new DelegateCommand(() =>
                            {
                                var pageManager = App.Current.Container.Resolve<PageManager>();
                                pageManager.OpenPage(HohoemaPageType.Community, communityId);

                                DismissInAppNotification();
                            })
                        },
                    }
            });
        }

        private async Task SubmitUserSuggestion(string userId)
        {
            var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();

            var user = await hohoemaApp.ContentProvider.GetUserInfo(userId);

            if (user == null) { return; }

            PublishInAppNotification(new InAppNotificationPayload()
            {
                Content = $"{user.Nickname} をお探しですか？",
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "ユーザー情報を開く",
                            Command = new DelegateCommand(() =>
                            {
                                var pageManager = App.Current.Container.Resolve<PageManager>();
                                pageManager.OpenPage(HohoemaPageType.UserInfo, userId);

                                DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = "動画一覧を開く",
                            Command = new DelegateCommand(() =>
                            {
                                var pageManager = App.Current.Container.Resolve<PageManager>();
                                pageManager.OpenPage(HohoemaPageType.UserVideo, userId);

                                DismissInAppNotification();
                            })
                        },
                    }
            });
        }


#endregion



        const string DEBUG_MODE_ENABLED_KEY = "Hohoema_DebugModeEnabled";
        public bool IsDebugModeEnabled
        {
            get
            {
                var enabled = ApplicationData.Current.LocalSettings.Values[DEBUG_MODE_ENABLED_KEY];
                if (enabled != null)
                {
                    return (bool)enabled;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                ApplicationData.Current.LocalSettings.Values[DEBUG_MODE_ENABLED_KEY] = value;
                if (value)
                {
                    PublishInAppNotification(new InAppNotificationPayload()
                    {
                        Content = "デバッグモードが有効になりました。\n問題発生時にエラーログを出力します。\nエラーログのフォルダは「設定」の「エラーログ」から確認できます。"
                    });
                }
            }
        }


        
    }




    
}
