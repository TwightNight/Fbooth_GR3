using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity
{
    public class PhotoBox
    {
        public int BoxHeight { get; set; }
        public int BoxWidth { get; set; }
        public int CoordinatesX { get; set; }
        public int CoordinatesY { get; set; }
        public Guid LayoutID { get; set; }
    }
}
