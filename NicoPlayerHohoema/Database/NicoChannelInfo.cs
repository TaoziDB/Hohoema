﻿using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Database
{
    public sealed class NicoChannelInfo
    {
        /// <summary>
        /// ex) ch1234568 
        /// </summary>
        [BsonId]
        public string RawId { get; set; }

        public string Id { get; set; }

        public string Name{ get; set; }

        public string ThumbnailUrl { get; set; }

        public List<Mntone.Nico2.Channels.Video.ChannelVideoInfo> Videos { get; set; } = new List<Mntone.Nico2.Channels.Video.ChannelVideoInfo>();

        public DateTime LastUpdate { get; set; }
    }

    public static class NicoChannelInfoDb
    {
        public static NicoChannelInfo GetFromRawId(string rawId)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db
                    .Query<NicoChannelInfo>()
                    .Where(x => x.RawId == rawId)
                    .Include(x => x.Videos)
                    .FirstOrDefault();
            }
        }

        public static NicoChannelInfo GetFromId(string id)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db
                    .Query<NicoChannelInfo>()
                    .Where(x => x.Id == id)
                    .Include(x => x.Videos)
                    .FirstOrDefault();
            }
        }


        public static bool AddOrUpdate(NicoChannelInfo info)
        {
            info.LastUpdate = DateTime.Now;

            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db.Upsert<NicoChannelInfo>(info);
            }
        }
    }
}
