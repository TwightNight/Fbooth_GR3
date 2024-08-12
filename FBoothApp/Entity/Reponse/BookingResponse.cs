using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity
{
    public class BookingResponse
    {
        public Guid BookingID { get; set; }
        public long ValidateCode { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsCancelled { get; set; }
        public Guid BoothID { get; set; }
        public Guid CustomerID { get; set; }
        public List<BookingServiceResponse> BookingServices { get; set; } = new List<BookingServiceResponse>();
    }
}
