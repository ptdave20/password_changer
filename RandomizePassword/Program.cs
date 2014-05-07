/*
 * This application will take a username and randomize the password, and email it to the select email address.
 * You must define the password size, then the username, followed by any emails you wish to send it to.
 * This will use the current logged in user for authentication to active directory.
 * 
 * Written by David Marchbanks
 * 
 * Requires Microsoft.Net Framework 4.0
 * */

using System;
using System.Collections.Generic;
using System.Text;
using System.DirectoryServices;
using System.Net.Mail;
using System.Net;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.DirectoryServices.ActiveDirectory;

namespace RandomizePassword {
    class Program {
        static String SMTP_FROM = "DoNotReply@yourdomain.com";  //ReplyTo Address
        static String SMTP_HOST = "your.smtp.server";           // Your SMTP Server host
        static String SMTP_USER = "user";                       // Your SMTP Server username ( MAY NEED \\DOMAIN\\USERNAME )
        static String SMTP_PASS = "pass";                       // Your SMTP Server password

        static String AD_DOMAIN = "your.domain";                // Your Active Directory Domain
        static String AD_USER = "user";                         // Your Active Directory User account ( NOT THE ONE BEING CHANGED! )
        static String AD_PASS = "pass";                         // Your Active Directory password

        static String AUTO_BCC = "";                            // E-Mail to automatically blind carbon-copy
        static String ERROR_CAUGHT_EMAIL = "";                  // If we catch an error, email this person


        static bool errorCaught = false;
        static bool finished = false;
        static int tryCount = 0;

        static void Main(string[] args) {
            String username=String.Empty;
            List<String> mailto = new List<String>();
            String smtp = String.Empty;
            int size = 10;

            if (args.Length < 3) {
                Console.WriteLine("RandomizePassword  [password size]  [username]  [email address ...] ");
                return;
            }

            

            username = args[1];
            for(int i=2; i<args.Length; ++i)
                mailto.Add(args[i]);

            size = Int32.Parse(args[0]);

            // This is the allowed characters to be used in a password, be as mean or as nice as you want
            char[] allowedChars = new char[] { 
                'a','b','c','d','e','f','g','h','i','j','k','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
                'A','C','D','E','F','G','H','I','J','K','L','M','N','P','Q','R','S','T','U','V','W','X','Y','Z',
                '!','@','#','$','%','1','2','3','4','5','6','7','8','9','0'
            };

            StringBuilder sb = new StringBuilder();


            /*
             * While not actually random, this will generate a digit for the thread to sleep
             * There is a odd issue of passwords being duplicated across accounts. This should
             * resolve the issue by preventing the system from getting the same seed.
             * */
            int randomWaitLength = 0;
            randomWaitLength += args.Length;
            randomWaitLength += size;
            randomWaitLength += mailto.Count;


            /* We don't exit if we have any errors, keep trying */
            #region !FINISHED

            while (!finished && tryCount < 100) {
                tryCount++;
                sb = new StringBuilder();
                foreach (String x in mailto) {
                    randomWaitLength += x.Length;
                    for (int i = 0; i < x.Length; ++i)
                        randomWaitLength += x.ToCharArray()[i];
                }

                System.Threading.Thread.Sleep(randomWaitLength);

                Random rand = new Random();
                int index;
                for (int i = 0; i < size; ++i) {
                    index = rand.Next(allowedChars.Length);
                    sb.Append(allowedChars[index]);
                }
                String firstname = "", lastname = "";

                try {
                    PrincipalContext dContext = new PrincipalContext(ContextType.Domain, AD_DOMAIN, AD_USER, AD_PASS);

                    using (UserPrincipal user = UserPrincipal.FindByIdentity(dContext, username)) {
                        user.SetPassword(sb.ToString());
                        firstname = user.GivenName;
                        lastname = user.Surname;
                        user.Save();
                    }
                } catch (PasswordException ex) {
                    Console.WriteLine("Error in setting password,\n" + ex.Message + "\nClosing.");
                    errorCaught = true;
                    finished = false;
                    continue;
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message + "\nClosing.");
                    errorCaught = true;
                    finished = false;
                    continue;
                }


                String msg = "The login for " + firstname + " " + lastname + "<br />\n" +
                            "Username: " + username + "<br />\n" +
                            "Password: " + sb.ToString() + "<br />\n";
                String subject = "Password update for : " + username;

                SmtpClient s = new SmtpClient(SMTP_HOST);
                s.Credentials = new NetworkCredential(SMTP_USER, SMTP_PASS);
                s.DeliveryMethod = SmtpDeliveryMethod.Network;
                MailMessage m = new MailMessage();
                m.Subject = subject;
                m.Body = msg;
                m.From = new MailAddress(SMTP_FROM);
                m.Bcc.Add(AUTO_BCC);
                m.IsBodyHtml = true;
                foreach (string to in mailto) {
                    m.To.Add(to);
                }
                s.Send(m);

                finished = true;
            }
            #endregion

            /* If a error was caught, e-mail Dave */
            #region ERRORCAUGHT
            if (errorCaught) {
                SmtpClient s = new SmtpClient(SMTP_HOST);
                s.Credentials = new NetworkCredential(SMTP_USER, SMTP_PASS);
                s.DeliveryMethod = SmtpDeliveryMethod.Network;
                MailMessage m = new MailMessage();
                m.Subject = "Trouble with account : " + username;
                m.Body = "There was trouble setting the password to account " + username + ". Tried " + tryCount + " time(s).";
                m.From = new MailAddress(SMTP_FROM);
                m.IsBodyHtml = true;
                m.To.Add(ERROR_CAUGHT_EMAIL);
                s.Send(m);
            }
            #endregion
        }


    }
}
