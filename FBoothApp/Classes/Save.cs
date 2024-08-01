using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FBoothApp
{
    class SavePhoto
    {
        public static string CurrentSessionPath { get; private set; }

        public int PhotoNumber { get; set; }
        public string FolderDirectory { get; set; }
        public string PhotoName { get; set; }
        public string PhotoDirectory { get; set; }
        public Guid BookingID { get; set; }



        public SavePhoto(int numb, Guid bookingID)
        {
            PhotoNumber = numb;
            BookingID = bookingID;
            FolderDirectory = CurrentSessionDirectory();
            PhotoName = ActualPhotoName();
            PhotoDirectory = NewPhotoDirectory();

        }
        public bool checkIfExsit(string fileName)
        {
            string currFile = Path.Combine(FolderDirectory, fileName);
            return File.Exists(currFile);
        }
        public string CurrentSessionDirectory()
        {
            if (CurrentSessionPath == null)
            {
                string p1 = Environment.CurrentDirectory;
                string p2 = Actual.DateNow();
                string sessionFolder = $"Session_{DateTime.Now.ToString("HH_mm")}";
                var path = Path.Combine(p1, p2, BookingID.ToString(), sessionFolder);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                CurrentSessionPath = path;
            }
            return CurrentSessionPath;
        }

        public string ActualPhotoName()
        {
            int number = PhotoNumber;
            string photoName = PhotoNaming(number);
            while (checkIfExsit(photoName) == true)
            {
                number++;
                photoName = PhotoNaming(number);
            }
            return photoName;
        }

        public int PhotoNumberJustTaken()
        {
            int number = PhotoNumber;
            string photoName = PhotoNaming(number);
            while (checkIfExsit(photoName) == true)
            {
                number++;
                photoName = PhotoNaming(number);
            }
            number--;
            return number;

        }
        public string PhotoNaming(int number)
        {
            string temp = "IMG_" + number + ".jpg";
            return temp;
        }
        public string NewPhotoDirectory()
        {
            string p1 = FolderDirectory;
            string p2 = PhotoName;
            return Path.Combine(p1, p2);
        }
    }
    class SavePrints
    {
        public int PrintNumber { get; set; }
        public string PrintsFolderDirectory { get; set; }
        public string PrintName { get; set; }
        public string PrintDirectory { get; set; }
        public Guid BookingID { get; set; }

        public SavePrints(int numb, Guid bookingID)
        {
            PrintNumber = numb;
            PrintsFolderDirectory = CurrentPrintsDirectory();
            PrintName = ActualPrintName();
            PrintDirectory = NewPrintDirectory();
            BookingID = bookingID;
        }
        public bool checkIfExsit(string fileName)
        {
            string currFile = Path.Combine(PrintsFolderDirectory, fileName);
            return File.Exists(currFile);
        }

        //chon folder de in
        public string CurrentPrintsDirectory()
        {
            string sessionFolder = new SavePhoto(PrintNumber, BookingID).CurrentSessionDirectory();
            string printsFolder = Path.Combine(sessionFolder, "prints");

            if (!Directory.Exists(printsFolder))
            {
                Directory.CreateDirectory(printsFolder);
            }

            return printsFolder;
        }

        public string ActualPrintName()
        {
            int number = PrintNumber;
            string printName = PrintNaming(number);
            while (checkIfExsit(printName) == true)
            {
                number++;
                printName = PrintNaming(number);
            }

            return printName;

        }
        public string PrintNaming(int number)
        {
            string temp = "print_" + number + ".jpg";
            return temp;
        }
        public string NewPrintDirectory()
        {

            string p1 = PrintsFolderDirectory;
            string p2 = PrintName;
            return Path.Combine(p1, p2);

            //return Path.Combine(PrintsFolderDirectory, PrintName);
        }

    }
}
