using FBoothApp.Entity.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity.Request
{
    public class CreatePhotoRequest
    {
        public PhotoVersion Version { get; set; }
        public Guid PhotoSessionID { get; set; }
        public Guid? BackgroundID { get; set; }
        public Dictionary<Guid, int> StickerList { get; set; } = new Dictionary<Guid, int>();
    }
}
