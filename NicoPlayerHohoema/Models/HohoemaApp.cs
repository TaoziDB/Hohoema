﻿using Mntone.Nico2;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class HohoemaApp : BindableBase
	{
		public HohoemaApp()
		{
			UserSettings = new HohoemaUserSettings();
			NiconicoPlayer = new NiconicoPlayer(this);
			NiconicoContext = new NiconicoContext();
		}

		public async Task<NiconicoSignInStatus> SignInFromUserSettings()
		{
			if (UserSettings.AccontSettings.IsValidMailOreTelephone && UserSettings.AccontSettings.IsValidPassword)
			{
				return await SignIn(UserSettings.AccontSettings.MailOrTelephone, UserSettings.AccontSettings.Password);
			}
			else
			{
				return NiconicoSignInStatus.Failed;
			}
		}


		public async Task<NiconicoSignInStatus> SignIn(string mailOrTelephone, string password)
		{
			NiconicoContext = new NiconicoContext(new NiconicoAuthenticationToken(mailOrTelephone, password));

			NiconicoContext.AdditionalUserAgent = HohoemaUserAgent;

			return await NiconicoContext.SignInAsync();
		}

		public async Task<NiconicoSignInStatus> SignOut()
		{
			if (NiconicoContext == null)
			{
				return NiconicoSignInStatus.Success;
			}

			return await NiconicoContext.SignOutOffAsync();
		}

		public async Task<NiconicoSignInStatus> CheckSignedInStatus()
		{
			return await NiconicoContext.GetIsSignedInAsync();
		}

		public HohoemaUserSettings UserSettings { get; private set; }


		private NiconicoContext _NiconicoContext;
		public NiconicoContext NiconicoContext
		{
			get { return _NiconicoContext; }
			set { SetProperty(ref _NiconicoContext, value); }
		}

		public NiconicoPlayer NiconicoPlayer { get; private set; }

		public const string HohoemaUserAgent = "Hohoema_UWP";

		
	}
	
}
