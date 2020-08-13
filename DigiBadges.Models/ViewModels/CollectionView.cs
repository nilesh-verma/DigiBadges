using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models.ViewModels
{
    public class CollectionView
    {
        public IEnumerable<BadgeCollections> BadgeCollections { get; set; }
        public IEnumerable<EarnerBadgeDetails> EarnerBadgeDetails { get; set; }
        public IEnumerable<BackPack> BackPacks { get; set; }
        public IEnumerable<Badge> Badge { get; set; }
        public IEnumerable<Issuers> Issuers { get; set; }

        public IEnumerable<BadgeImage> BI{get;set;}

    }
}
