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
    class Printing
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
                    DefaultPageSettings = { Margins = new Margins(0, 0, 0, 0) },
                    PrinterSettings = { PrinterName = actualPrinter, Copies = actualNumberOfCopies }
                };

                pd.PrintPage += (sndr, args) =>
                {
                    using (Image i = Image.FromFile(printPath))
                    {
                        i.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        Rectangle m = args.MarginBounds;

                        if ((double)i.Width / i.Height > (double)m.Width / m.Height)
                        {
                            m.Height = (int)((double)i.Height / i.Width * m.Width);
                        }
                        else
                        {
                            m.Width = (int)((double)i.Width / i.Height * m.Height);
                        }

                        pd.DefaultPageSettings.Landscape = m.Height > m.Width;
                        m.Y = (pd.DefaultPageSettings.PaperSize.Height - m.Height) / 2;
                        m.X = (pd.DefaultPageSettings.PaperSize.Width - m.Width) / 2;

                        args.Graphics.DrawImage(i, m);
                    }
                };

                Debug.WriteLine("Tên máy in: " + actualPrinter);
                Debug.WriteLine("Số lượng bản in: " + actualNumberOfCopies);

                if (!pd.PrinterSettings.IsValid)
                {
                    throw new InvalidPrinterException(pd.PrinterSettings);
                }

                for (int i = 0; i < actualNumberOfCopies; i++)
                {
                    pd.Print();
                }

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
