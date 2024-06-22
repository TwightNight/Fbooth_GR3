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
        public string LayoutCode { get; set; }
        public string LayoutURL { get; set; }
        public int Lenght { get; set; }
        public int Width { get; set; }
        public short PhotoSlot { get; set; }
    }
}
