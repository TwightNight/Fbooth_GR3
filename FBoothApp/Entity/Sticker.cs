using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity
{
    public class Sticker
    {
        public string StickerID { get; set; }
        public string StickerCode { get; set; } 
        public string StickerURL { get; set; } 
        public string CouldID { get; set; } 
        public int stickerHeight { get; set; }
        public int stickerWidth { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
