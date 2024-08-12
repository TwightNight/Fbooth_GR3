using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity
{
    public class Layout
    {
        public Guid LayoutID { get; set; }
        public string LayoutURL { get; set; }
        public string CouldID { get; set; }
        public string LayoutCode { get; set; }
        public string Status { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int PhotoSlot { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModified { get; set; }
        public List<PhotoBox> PhotoBoxes { get; set; }
        public List<Background> Backgrounds { get; set; }
    }
}
