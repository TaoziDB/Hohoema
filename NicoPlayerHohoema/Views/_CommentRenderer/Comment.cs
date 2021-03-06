﻿using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.ViewModels;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using System.Text.RegularExpressions;

namespace NicoPlayerHohoema.Views
{
	public class Comment : BindableBase
	{
		public const float default_fontSize = fontSize_mid;

		public const float fontSize_mid = 1.0f;
		public const float fontSize_small = 0.75f;
		public const float fontSize_big = 1.25f;


		public string CommentText { get; set; }

		public uint CommentId { get; set; }

		public string UserId { get; set; }

		public long VideoPosition { get; set; }
		public long EndPosition { get; set; }

		public Color? Color { get; set; }

		public Color RealColor { get; set; }
		public Color BackColor { get; set; }

		public uint FontSize { get; set; } = 24;
		public float FontScale { get; set; } = default_fontSize;

		private VerticalAlignment? _VAlign;
		public VerticalAlignment? VAlign
		{
			get
			{
				return _VAlign;
			}
			set
			{
				_VAlign = value;
			}
		}

		public HorizontalAlignment? HAlign { get; set; }

		public bool IsOwnerComment { get; set; }
        
        public bool IsOperationCommand { get; set; }

		public bool IsLoginUserComment { get; set; } = false;




        static Uri ParseLinkFromHtml(string text)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(text);

            var root = doc.DocumentNode;
            var anchorNode = root.Descendants("a").FirstOrDefault();
            if (anchorNode != null)
            {
                if (anchorNode.Attributes.Contains("href"))
                {
                    return  new Uri(anchorNode.Attributes["href"].Value);
                }
            }

            return null;
        }

        bool? _IsLink;
        public bool IsLink
        {
            get
            {
                ResetLink();

                return _IsLink.Value;
            }
        }

        Uri _Link;
        public Uri Link
        {
            get
            {
                ResetLink();

                return _Link;
            }
        }

        private void ResetLink()
        {
            if (!_IsLink.HasValue)
            {
                if (Uri.IsWellFormedUriString(CommentText, UriKind.Absolute))
                {
                    _Link = new Uri(CommentText);
                }
                else
                {
                    _Link = ParseLinkFromHtml(CommentText);
                }

                _IsLink = _Link != null;
            }
        }


		private bool _IsVisible = true;
		public bool IsVisible
		{
			get { return _IsVisible; }
			set { SetProperty(ref _IsVisible, value); }
		}

		public bool IsAnonimity { get; set; } = false;

		public bool IsScrolling
		{
			get
			{
				return !(VAlign.HasValue || HAlign.HasValue);
			}
		}

		public TextWrapping TextWrapping
		{
			get
			{
				if (IsOwnerComment && VAlign.HasValue)
				{
					return TextWrapping.Wrap;
				}
				else
				{
					return TextWrapping.NoWrap;
				}
			}
		}


		public double TextBGOffset { get; set; }


        NGSettings NGSettings { get; }
        public Comment(VideoPlayerPageViewModel videoPlayerPageVM, NGSettings ngsettings)
		{
			_VideoPlayerPageViewModel = videoPlayerPageVM;
            NGSettings = ngsettings;
            TextBGOffset = 1.0;
		}

		public Comment(NGSettings ngsettings)
		{
//			_VideoPlayerPageViewModel = videoPlayerPageVM;
			TextBGOffset = 1.0;
		}


		

		public void ApplyCommands(IEnumerable<CommandType> commandList)
		{
			if (commandList == null || commandList.Any(x => x == CommandType.all))
			{
				commandList = Enumerable.Empty<CommandType>();
			}

			foreach (var command in commandList)
			{
				switch (command)
				{
					case CommandType.small:
						this.FontScale = fontSize_small;
						break;
					case CommandType.big:
						this.FontScale = fontSize_big;
						break;
					case CommandType.medium:
						this.FontScale = fontSize_mid;
						break;
					case CommandType.ue:
						this.VAlign = VerticalAlignment.Top;
						break;
					case CommandType.shita:
						this.VAlign = VerticalAlignment.Bottom;
						break;
					case CommandType.naka:
						this.VAlign = VerticalAlignment.Center;
						break;
					case CommandType.white:
						this.Color = ColorExtention.HexStringToColor("FFFFFF");
						break;
					case CommandType.red:
						this.Color = ColorExtention.HexStringToColor("FF0000");
						break;
					case CommandType.pink:
						this.Color = ColorExtention.HexStringToColor("FF8080");
						break;
					case CommandType.orange:
						this.Color = ColorExtention.HexStringToColor("FFC000");
						break;
					case CommandType.yellow:
						this.Color = ColorExtention.HexStringToColor("FFFF00");
						break;
					case CommandType.green:
						this.Color = ColorExtention.HexStringToColor("00FF00");
						break;
					case CommandType.cyan:
						this.Color = ColorExtention.HexStringToColor("00FFFF");
						break;
					case CommandType.blue:
						this.Color = ColorExtention.HexStringToColor("0000FF");
						break;
					case CommandType.purple:
						this.Color = ColorExtention.HexStringToColor("C000FF");
						break;
					case CommandType.black:
						this.Color = ColorExtention.HexStringToColor("000000");
						break;
					case CommandType.white2:
						this.Color = ColorExtention.HexStringToColor("CCCC99");
						break;
					case CommandType.niconicowhite:
						this.Color = ColorExtention.HexStringToColor("CCCC99");
						break;
					case CommandType.red2:
						this.Color = ColorExtention.HexStringToColor("CC0033");
						break;
					case CommandType.truered:
						this.Color = ColorExtention.HexStringToColor("CC0033");
						break;
					case CommandType.pink2:
						this.Color = ColorExtention.HexStringToColor("FF33CC");
						break;
					case CommandType.orange2:
						this.Color = ColorExtention.HexStringToColor("FF6600");
						break;
					case CommandType.passionorange:
						this.Color = ColorExtention.HexStringToColor("FF6600");
						break;
					case CommandType.yellow2:
						this.Color = ColorExtention.HexStringToColor("999900");
						break;
					case CommandType.madyellow:
						this.Color = ColorExtention.HexStringToColor("999900");
						break;
					case CommandType.green2:
						this.Color = ColorExtention.HexStringToColor("00CC66");
						break;
					case CommandType.elementalgreen:
						this.Color = ColorExtention.HexStringToColor("00CC66");
						break;
					case CommandType.cyan2:
						this.Color = ColorExtention.HexStringToColor("00CCCC");
						break;
					case CommandType.blue2:
						this.Color = ColorExtention.HexStringToColor("3399FF");
						break;
					case CommandType.marineblue:
						this.Color = ColorExtention.HexStringToColor("3399FF");
						break;
					case CommandType.purple2:
						this.Color = ColorExtention.HexStringToColor("6633CC");
						break;
					case CommandType.nobleviolet:
						this.Color = ColorExtention.HexStringToColor("6633CC");
						break;
					case CommandType.black2:
						this.Color = ColorExtention.HexStringToColor("666666");
						break;
					case CommandType.full:
						break;
					case CommandType._184:
						this.IsAnonimity = true;
						break;
					case CommandType.invisible:
						this.IsVisible = false;
						break;
					case CommandType.all:
						// Note: 事前に判定しているのでここでは評価しない
						break;
					case CommandType.from_button:
						break;
					case CommandType.is_button:
						break;
					case CommandType._live:

						break;
					default:
						break;
				}
			}

			// TODO: 投稿者のコメント表示時間を伸ばす？（3秒→５秒）
			// usまたはshitaが指定されている場合に限る？

			// 　→　投コメ解説をみやすくしたい

			if (this.IsOwnerComment && this.VAlign.HasValue)
			{
				var displayTime = Math.Max(3.0f, this.CommentText.Count() * 0.3f); // 4文字で1秒？ 年齢層的に読みが遅いかもしれないのでやや長めに
				var displayTimeVideoLength = (uint)(displayTime * 100);
				this.EndPosition = this.VideoPosition + displayTimeVideoLength;
			}


		}

        public void ApplyCommands(IEnumerable<string> commandList)
        {
            if (commandList == null)
            {
                return;
            }

            foreach (var command in commandList)
            {
                switch (command)
                {
                    case "small":
                        this.FontScale = fontSize_small;
                        break;
                    case "big":
                        this.FontScale = fontSize_big;
                        break;
                    case "medium":
                        this.FontScale = fontSize_mid;
                        break;
                    case "ue":
                        this.VAlign = VerticalAlignment.Top;
                        break;
                    case "shita":
                        this.VAlign = VerticalAlignment.Bottom;
                        break;
                    case "naka":
                        this.VAlign = VerticalAlignment.Center;
                        break;
                    case "white":
                        this.Color = ColorExtention.HexStringToColor("FFFFFF");
                        break;
                    case "red":
                        this.Color = ColorExtention.HexStringToColor("FF0000");
                        break;
                    case "pink":
                        this.Color = ColorExtention.HexStringToColor("FF8080");
                        break;
                    case "orange":
                        this.Color = ColorExtention.HexStringToColor("FFC000");
                        break;
                    case "yellow":
                        this.Color = ColorExtention.HexStringToColor("FFFF00");
                        break;
                    case "green":
                        this.Color = ColorExtention.HexStringToColor("00FF00");
                        break;
                    case "cyan":
                        this.Color = ColorExtention.HexStringToColor("00FFFF");
                        break;
                    case "blue":
                        this.Color = ColorExtention.HexStringToColor("0000FF");
                        break;
                    case "purple":
                        this.Color = ColorExtention.HexStringToColor("C000FF");
                        break;
                    case "black":
                        this.Color = ColorExtention.HexStringToColor("000000");
                        break;
                    case "white2":
                        this.Color = ColorExtention.HexStringToColor("CCCC99");
                        break;
                    case "niconicowhite":
                        this.Color = ColorExtention.HexStringToColor("CCCC99");
                        break;
                    case "red2":
                        this.Color = ColorExtention.HexStringToColor("CC0033");
                        break;
                    case "truered":
                        this.Color = ColorExtention.HexStringToColor("CC0033");
                        break;
                    case "pink2":
                        this.Color = ColorExtention.HexStringToColor("FF33CC");
                        break;
                    case "orange2":
                        this.Color = ColorExtention.HexStringToColor("FF6600");
                        break;
                    case "passionorange":
                        this.Color = ColorExtention.HexStringToColor("FF6600");
                        break;
                    case "yellow2":
                        this.Color = ColorExtention.HexStringToColor("999900");
                        break;
                    case "madyellow":
                        this.Color = ColorExtention.HexStringToColor("999900");
                        break;
                    case "green2":
                        this.Color = ColorExtention.HexStringToColor("00CC66");
                        break;
                    case "elementalgreen":
                        this.Color = ColorExtention.HexStringToColor("00CC66");
                        break;
                    case "cyan2":
                        this.Color = ColorExtention.HexStringToColor("00CCCC");
                        break;
                    case "blue2":
                        this.Color = ColorExtention.HexStringToColor("3399FF");
                        break;
                    case "marineblue":
                        this.Color = ColorExtention.HexStringToColor("3399FF");
                        break;
                    case "purple2":
                        this.Color = ColorExtention.HexStringToColor("6633CC");
                        break;
                    case "nobleviolet":
                        this.Color = ColorExtention.HexStringToColor("6633CC");
                        break;
                    case "black2":
                        this.Color = ColorExtention.HexStringToColor("666666");
                        break;
                    case "full":
                        break;
                    case "_184":
                        this.IsAnonimity = true;
                        break;
                    case "invisible":
                        this.IsVisible = false;
                        break;
                    case "all":
                        // Note": 事前に判定しているのでここでは評価しない
                        break;
                    case "from_button":
                        break;
                    case "is_button":
                        break;
                    case "_live":

                        break;
                    default:
                        if (command.StartsWith("#"))
                        {
                            this.Color = ColorExtention.HexStringToColor(command.Remove(0, 1));
                        }
                        break;
                }
            }

            // TODO: 投稿者のコメント表示時間を伸ばす？（3秒→５秒）
            // usまたはshitaが指定されている場合に限る？

            // 　→　投コメ解説をみやすくしたい

            if (this.IsOwnerComment && this.VAlign.HasValue)
            {
                var displayTime = Math.Max(3.0f, this.CommentText.Count() * 0.3f); // 4文字で1秒？ 年齢層的に読みが遅いかもしれないのでやや長めに
                var displayTimeVideoLength = (uint)(displayTime * 100);
                this.EndPosition = this.VideoPosition + displayTimeVideoLength;
            }


        }


        public bool CheckIsNGComment()
        {
            if (NGSettings == null || CommentText == null)
            {
                return false;
            }

            return NGSettings.IsNGComment(CommentText) != null;
        }





		private VideoPlayerPageViewModel _VideoPlayerPageViewModel;
	}
}
