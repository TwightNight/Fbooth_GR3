using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using EOSDigital.SDK;
using System.Net.Mime;
using System.Collections.Generic;
using QRCoder;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;



namespace FBoothApp.Classes
{
    class EmailSender
    {
        private string smtpServerName;
        private string smtpPortNumber;
        private string emailHostAddress;
        private string emailHostPassword;

        private XDocument settings = new XDocument();
        private string currentDirectory = Environment.CurrentDirectory;

        public async void SendEmail(string emailClientAddress, List<string> photoPaths)
        {
            try
            {
                // Validate email address
                if (!IsValidEmail(emailClientAddress))
                {
                    Report.Error("Wrong e-mail format \nPlease enter your e-mail correctly\nexample@mail.com", true);
                    return;
                }

                LoadValues();

                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(smtpServerName)
                {
                    Credentials = new NetworkCredential(emailHostAddress, emailHostPassword),
                    Port = int.Parse(smtpPortNumber),
                    EnableSsl = true
                };

                mail.From = new MailAddress(emailHostAddress);
                mail.To.Add(emailClientAddress);
                mail.Subject = "Your Photos from FBooth";

                StringBuilder sbBody = new StringBuilder();
                sbBody.AppendLine("<html>");
                sbBody.AppendLine("<head>");
                sbBody.AppendLine("<style type=\"text/css\">");
                sbBody.AppendLine("body { font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; }");
                sbBody.AppendLine(".container { width: 100%; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; border: 1px solid #dddddd; border-radius: 10px; }");
                sbBody.AppendLine(".header { text-align: center; padding: 20px; background-color: #379eff; border-top-left-radius: 10px; border-top-right-radius: 10px; }");
                sbBody.AppendLine(".header img { width: 100px; height: auto; }");
                sbBody.AppendLine(".content { padding: 20px; text-align: center; }");
                sbBody.AppendLine(".content h1 { font-size: 24px; color: #333333; }");
                sbBody.AppendLine(".content p { font-size: 16px; color: #00366a; }");
                sbBody.AppendLine(".content img { width: 100%; max-width: 500px; margin: 20px 0; border: 1px solid #dddddd; border-radius: 10px; }");
                sbBody.AppendLine(".footer { padding: 20px; text-align: center; font-size: 12px; color: #999999; background-color: #f5f5f5; border-bottom-left-radius: 10px; border-bottom-right-radius: 10px; }");
                sbBody.AppendLine("</style>");
                sbBody.AppendLine("</head>");
                sbBody.AppendLine("<body>");
                sbBody.AppendLine("<div class=\"container\">");

                // Header with logo
                sbBody.AppendLine("<div class=\"header\">");
                sbBody.AppendLine("<img src=\"https://res.cloudinary.com/dfxvccyje/image/upload/v1721217361/Logo/FboothLogo.png\" alt=\"FBooth Logo\">");
                sbBody.AppendLine("</div>");

                // Greeting message
                sbBody.AppendLine("<div class=\"content\">");
                sbBody.AppendLine("<h1>Hi,</h1>");
                sbBody.AppendLine("<p>Thank you for using our photo booth! Here are your photos:</p>");

                // Embed images
                foreach (var photoPath in photoPaths)
                {
                    if (!string.IsNullOrEmpty(photoPath) && File.Exists(photoPath))
                    {
                        string contentId = Guid.NewGuid().ToString();
                        Attachment attachment = new Attachment(photoPath);
                        attachment.ContentDisposition.Inline = true;
                        attachment.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                        attachment.ContentId = contentId;
                        attachment.ContentType.MediaType = "image/jpeg";
                        attachment.ContentType.Name = Path.GetFileName(photoPath);
                        mail.Attachments.Add(attachment);

                        sbBody.AppendLine($"<img src=\"cid:{contentId}\" alt=\"Photo\">");
                    }
                }

                sbBody.AppendLine("<p>Best regards,</p>");
                sbBody.AppendLine("<p>FBooth Team</p>");
                sbBody.AppendLine("</div>");

                // Footer section
                sbBody.AppendLine("<div class=\"footer\">");
                sbBody.AppendLine("<p>&copy; 2023 FBooth. All rights reserved.</p>");
                sbBody.AppendLine("</div>");

                sbBody.AppendLine("</div>");
                sbBody.AppendLine("</body>");
                sbBody.AppendLine("</html>");

                mail.Body = sbBody.ToString();
                mail.IsBodyHtml = true;

                await SmtpServer.SendMailAsync(mail);
            }
            catch (FormatException ex)
            {
                Report.Error("Wrong e-mail format \nPlease enter your e-mail correctly\nexample@mail.com", true);
                Debug.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                Report.Error("An error occurred while sending the email.", true);
                Debug.WriteLine(ex.ToString());
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public void LoadValues()
        {
            try
            {
                settings = XDocument.Load(Path.Combine(currentDirectory, "menusettings.xml"));
                settings.Root.Elements("setting");
                emailHostAddress = settings.Root.Element("EmailHostAddress").Value;
                emailHostPassword = settings.Root.Element("EmailHostPassword").Value;
                smtpServerName = settings.Root.Element("SmtpServerName").Value;
                smtpPortNumber = settings.Root.Element("SmtpPortNumber").Value;
            }
            catch (XmlException e)
            {
                Debug.WriteLine("LoadDefaultValues exception: " + e.Message);
            }
            catch (NullReferenceException e)
            {
                Debug.WriteLine("Missing settings in menusettings.xml: " + e.Message);
            }
        }
    }
}

