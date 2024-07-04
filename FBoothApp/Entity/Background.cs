using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity
{
    public class Background
    {
        public Guid BackgroundID { get; set; }
        public string BackgroundCode { get; set; } 
        public string BackgroundURL { get; set; } 
        public string CouldID { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModified { get; set; }
        public Guid LayoutID { get; set; }
    }
}
