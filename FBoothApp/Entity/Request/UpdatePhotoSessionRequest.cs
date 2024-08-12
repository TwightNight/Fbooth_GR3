using FBoothApp.Entity.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity.Request
{
    public class UpdatePhotoSessionRequest
    {
        public int? TotalPhotoTaken { get; set; }
        public PhotoSessionStatus? Status { get; set; }
    }
}
