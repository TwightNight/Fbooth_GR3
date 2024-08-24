using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FBoothApp
{
    class MenuData
    {
        public string firstPrinter { get; set; }
        public string timeBetweenPhotos { get; set; }
        public string secondPrinter { get; set; }
        public string SmtpServerName { get; set; }
        public string SmtpPortNumber { get; set; }

        public string EmailHostAddress { get; set; }
        public string EmailHostPassword { get; set; }

        public string BoothID { get; set; }

        public  void FillValues( string actprint, string timephotos,
            string nsecondprinter, string emailHostAddress, string emailHostPassword, 
            string emailSmtpServerName, string emailSmtpPortNumber, string boothID)
        {
            firstPrinter = actprint;
            timeBetweenPhotos = timephotos;
            secondPrinter = nsecondprinter;
            EmailHostAddress = emailHostAddress;
            EmailHostPassword = emailHostPassword;
            SmtpServerName = emailSmtpServerName;
            SmtpPortNumber = emailSmtpPortNumber;
            BoothID = boothID;
            SaveToXml();
        }
        public void SaveToXml()
        {
            using (XmlWriter writer = XmlWriter.Create("menusettings.xml"))
            {
                writer.WriteStartElement("Setting");
                writer.WriteElementString("actualPrinter", firstPrinter);
                writer.WriteElementString("secondPrinter", secondPrinter);
                writer.WriteElementString("timeBetweenPhotos", timeBetweenPhotos);
                writer.WriteElementString("EmailHostAddress", EmailHostAddress);
                writer.WriteElementString("EmailHostPassword", EmailHostPassword);
                writer.WriteElementString("SmtpServerName", SmtpServerName);
                writer.WriteElementString("SmtpPortNumber", SmtpPortNumber);
                writer.WriteElementString("BoothID", BoothID);
                writer.WriteEndElement();
                writer.Flush();
            }
        }
    }
}
