using FBoothApp.Entity.Enum;
using System;

namespace FBoothApp.Entity.Reponse
{
    public class ServiceResponse
    {
        public Guid ServiceID { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDescription { get; set; }
        public string Unit { get; set; }
        public decimal ServicePrice { get; set; }
        public string ServiceIamgeURL { get; set; }
        public string CouldID { get; set; }
        public ServiceType ServiceType { get; set; }
        public StatusUse Status { get; set; }
    }
}
