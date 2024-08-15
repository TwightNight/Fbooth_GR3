using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Printing;
using System.Drawing;
using System.IO;

namespace FBoothApp
{
    class PrintingServices
    {
        static public void Print(string printPath, string actualPrinter, short actualNumberOfCopies)
        {
            try
            {
                if (!File.Exists(printPath))
                {
                    throw new FileNotFoundException("File không tồn tại: " + printPath);
                }

                PrintDocument pd = new PrintDocument
                {
                    PrintController = new StandardPrintController(),
                    DefaultPageSettings = { Margins = new Margins(100, 100, 100, 100) }, // Set margin here
                    PrinterSettings = { PrinterName = actualPrinter, Copies = actualNumberOfCopies }
                };

                pd.PrintPage += (sndr, args) =>
                {
                    using (Image i = Image.FromFile(printPath))
                    {
                        // Calculate image dimensions preserving aspect ratio
                        Rectangle printArea = args.MarginBounds;
                        float aspectRatio = (float)i.Width / i.Height;
                        if (aspectRatio > (float)printArea.Width / printArea.Height)
                        {
                            printArea.Height = (int)(printArea.Width / aspectRatio);
                        }
                        else
                        {
                            printArea.Width = (int)(printArea.Height * aspectRatio);
                        }

                        // Center the image on the page
                        printArea.X = (args.PageBounds.Width - printArea.Width) / 2;
                        printArea.Y = (args.PageBounds.Height - printArea.Height) / 2;

                        // Draw the image
                        args.Graphics.DrawImage(i, printArea);
                    }
                };

                Debug.WriteLine("Tên máy in: " + actualPrinter);
                Debug.WriteLine("Số lượng bản in: " + actualNumberOfCopies);

                if (!pd.PrinterSettings.IsValid)
                {
                    throw new InvalidPrinterException(pd.PrinterSettings);
                }

                pd.Print();  // In số lượng bản in đã thiết lập trong PrinterSettings

                pd.Dispose();
            }
            catch (FileNotFoundException ex)
            {
                Debug.WriteLine("Lỗi file không tồn tại: " + ex.Message);
                Console.WriteLine("Lỗi file không tồn tại: " + ex.Message);
            }
            catch (InvalidPrinterException ex)
            {
                Debug.WriteLine("Lỗi máy in không hợp lệ: " + ex.Message);
                Console.WriteLine("Lỗi máy in không hợp lệ: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi chung: " + ex.Message);
                Console.WriteLine("Lỗi chung: " + ex.Message);
            }
        }


        static public string ActualPrinter(string actualForeground, string firstprinter, string secondprinter)
        {
            if ((actualForeground == "foreground_3") || (actualForeground == "foregrund_4_paski"))
            {
                return secondprinter;
            }
            else return firstprinter;
        }
    }
}
