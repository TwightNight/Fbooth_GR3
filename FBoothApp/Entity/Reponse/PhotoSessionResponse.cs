using FBoothApp.Entity.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity
{
    public class PhotoSessionResponse
    {
        public Guid PhotoSessionID { get; set; }
        public string SessionName { get; set; }
        public int SessionIndex { get; set; }
        public int? TotalPhotoTaken { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public Guid LayoutID { get; set; }
        public Guid BookingID { get; set; }
        public PhotoSessionStatus Status { get; set; }
    }
}
