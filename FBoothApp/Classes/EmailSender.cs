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

        public async void SendEmail(int photoNumber, int numberOfPhotosToSendViaEmail, string emailClientAddress, string printedPhotoPath)
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

                await Task.Run(() =>
                {
                    MailMessage mail = new MailMessage();
                    SmtpClient SmtpServer = new SmtpClient(smtpServerName);

                    mail.From = new MailAddress(emailHostAddress);
                    mail.To.Add(emailClientAddress);
                    mail.Subject = "Your Photos from FBooth";

                    StringBuilder sbBody = new StringBuilder();
                    sbBody.AppendLine("<html>");
                    sbBody.AppendLine("<body>");
                    sbBody.AppendLine("<h1>Hi,</h1>");
                    sbBody.AppendLine("<p>Thank you for using our photo booth! Here are your photos:</p>");

                    var instance = new SavePhoto(photoNumber);
                    for (int i = 0; i < numberOfPhotosToSendViaEmail; i++)
                    {
                        photoNumber = (instance.PhotoNumberJustTaken() - i);
                        Debug.WriteLine("photo number is: " + photoNumber);
                        string photoName = instance.photoNaming(photoNumber);
                        string photoDirectoryPath = Path.Combine(Actual.FilePath(), photoName);
                        Debug.WriteLine(photoDirectoryPath);

                        // Embed the image
                        string contentId = Guid.NewGuid().ToString();
                        Attachment attachment = new Attachment(photoDirectoryPath);
                        attachment.ContentDisposition.Inline = true;
                        attachment.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                        attachment.ContentId = contentId;
                        attachment.ContentType.MediaType = "image/jpeg";
                        attachment.ContentType.Name = Path.GetFileName(photoDirectoryPath);
                        mail.Attachments.Add(attachment);

                        // Add the image to the body
                        sbBody.AppendLine($"<img src=\"cid:{contentId}\" alt=\"Photo\" style=\"width:100%; max-width:600px;\"/>");
                    }

                    if (!string.IsNullOrEmpty(printedPhotoPath) && File.Exists(printedPhotoPath))
                    {
                        string printedContentId = Guid.NewGuid().ToString();
                        Attachment printedAttachment = new Attachment(printedPhotoPath);
                        printedAttachment.ContentDisposition.Inline = true;
                        printedAttachment.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                        printedAttachment.ContentId = printedContentId;
                        printedAttachment.ContentType.MediaType = "image/jpeg";
                        printedAttachment.ContentType.Name = Path.GetFileName(printedPhotoPath);
                        mail.Attachments.Add(printedAttachment);

                        // Add the printed photo to the body
                        sbBody.AppendLine($"<h2>Your Printed Photo:</h2>");
                        sbBody.AppendLine($"<img src=\"cid:{printedContentId}\" alt=\"Printed Photo\" style=\"width:100%; max-width:600px;\"/>");
                    }

                    sbBody.AppendLine("<p>Best regards,</p>");
                    sbBody.AppendLine("<p>FBooth Team</p>");
                    sbBody.AppendLine("</body>");
                    sbBody.AppendLine("</html>");

                    mail.Body = sbBody.ToString();
                    mail.IsBodyHtml = true;

                    SmtpServer.Credentials = new NetworkCredential(emailHostAddress, emailHostPassword);
                    SmtpServer.Port = int.Parse(smtpPortNumber);
                    SmtpServer.EnableSsl = true;

                    SmtpServer.Send(mail);
                });
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
                Debug.WriteLine("LoadDefaultValues exception");
            }
            catch (NullReferenceException e)
            {
                Debug.WriteLine("missing settings in menusettings.xml");
            }



        }
    }
}

