﻿using Mntone.Nico2.Users.Fav;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public abstract class FavInfoGroupWithFavData : FavInfoGroupBaseTemplate<FavData>
	{

		public FavInfoGroupWithFavData(HohoemaApp hohoemaApp)
			: base(hohoemaApp)
		{
		}
		protected override FavInfo ConvertToFavInfo(FavData source)
		{
			return new FavInfo()
			{
				Id = source.ItemId,
				FavoriteItemType = FavoriteItemType,
				Name = source.Title,
			};
		}

		protected override string FavSourceToItemId(FavData source)
		{
			return source.ItemId;
		}

	}
}