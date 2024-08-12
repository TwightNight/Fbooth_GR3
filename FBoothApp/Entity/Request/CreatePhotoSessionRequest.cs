using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity
{
    public class CreatePhotoSessionRequest
    {
        public string SessionName { get; set; }
        public DateTime StartTime { get; set; }
        public Guid LayoutID { get; set; }
        public Guid BookingID { get; set; }
    }
}
