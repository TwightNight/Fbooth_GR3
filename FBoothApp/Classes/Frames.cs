using FBoothApp.Classes.Entity;
using FBoothApp.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Classes
{
    public class Frames
    {
        private Frames(List<Infrastructure.FrameType> list)
        {
            FrameTypes = list;
        }
        public List<Infrastructure.FrameType> FrameTypes { get; }
        private static Frames instance = null;
        public static Frames Instance(List<Infrastructure.FrameType> list)
        {
            if (instance == null) { instance = new Frames(list); }
            return instance;
        }
        public bool LoadTypeImage(string folderPath, string id)
        {
            Infrastructure.FrameType type = FrameTypes.Where(x => x.Code.Equals(id)).FirstOrDefault();
            if (type != null)
            {
                type.BackgroundImages = UtilClass.LoadImagesFromFolder(folderPath);
                return true;
            }
            else
            {
                return false;
            }
        }
        public Infrastructure.FrameType GetType(string id)
        {
            return FrameTypes.Where(x => x.Code.Equals(id)).FirstOrDefault();
        }
    }
}
