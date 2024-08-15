using FBoothApp.Entity.Reponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity
{
    public class BookingServiceResponse
    {
        public Guid BookingServiceID { get; set; }
        public string ServiceName { get; set; }
        public short Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal SubTotal { get; set; }
        public Guid ServiceID { get; set; }
        public ServiceResponse Service { get; set; }
    }
}
