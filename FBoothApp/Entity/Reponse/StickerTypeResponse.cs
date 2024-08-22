using FBoothApp.Entity.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity.Reponse
{
    public class StickerTypeResponse
    {
        public Guid StickerTypeID { get; set; }
        public string StickerTypeName { get; set; }
        public string RepresentImageURL { get; set; }
        public string CouldID { get; set; }
        public StatusUse Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
        public virtual ICollection<StickerResponse> Stickers { get; set; }
    }
}
