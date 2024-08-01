using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EOSDigital.API;
using EOSDigital.SDK;
using ImageSource = EOSDigital.SDK.ImageSource;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Size = EOSDigital.SDK.Size;


namespace FBoothApp.Classes
{
    class GetImageThumbnail
    {
        public string GetThumbnailPathForIndex(int photoNumber, Guid BookingID)
        {
            var save = new SavePhoto(photoNumber, BookingID);
            string photoName = save.PhotoNaming(photoNumber);
            return Path.Combine(save.FolderDirectory, photoName);
        }

        public string GetLatestThumbnailPath(Guid BookingID)
        {
            var save = new SavePhoto(1, BookingID);
            int latestPhotoNumber = save.PhotoNumberJustTaken();
            string latestPhotoName = save.PhotoNaming(latestPhotoNumber);
            return Path.Combine(save.FolderDirectory, latestPhotoName);
        }
    }
}
