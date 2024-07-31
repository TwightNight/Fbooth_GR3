using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity
{
    public class CheckinRequest
    {
        public Guid BoothID { get; set; }
        public long Code { get; set; }
    }
}
