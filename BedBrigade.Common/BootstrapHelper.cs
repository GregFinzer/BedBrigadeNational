using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualBasic;
using System.Diagnostics.Metrics;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace BedBrigade.Common
{
    public static class BootstrapHelper
    {
        public struct BootstrapMessage
        {
            public string AlertStyle;
            public string FontIcon;
            public string Title;
            public string MainMessage;
            public string AdditionalMessage;
            public bool ShowTitle;
            public string FontSize;
            public bool Closeable;

        }

        public static MarkupString GetBootstrapMessage(string strAlertType, string strMainMessage="", string strAdditionalMessage ="", bool bShowTitle = true, string strFontSize = "medium", bool bCloseable = false)
        {
            // strAlertType: note, info, success, error, warning
            // Creates a TextInfo based on the "en-US" culture.
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
            var strTitle = myTI.ToTitleCase(strAlertType);

            BootstrapMessage myMessage = new BootstrapMessage();
            myMessage.FontSize = strFontSize;
            myMessage.MainMessage = strMainMessage;
            myMessage.AdditionalMessage = strAdditionalMessage;
            myMessage.ShowTitle = bShowTitle;
            myMessage.Closeable = bCloseable;

            var strFontIcon = String.Empty;
            if (strAlertType == "error")
            {
                strAlertType = "danger";
            }

            var strAlertStyle = strAlertType;
           
            // type of message

            switch (strAlertType)
            {
                case "warning":
                default:
                    strFontIcon = "<i class='fa fa-exclamation-triangle'></i>&nbsp;";
                    break;
                case "info":
                case "note":
                    bShowTitle = false;
                    strFontIcon = "<i class='fa fa-info-circle'></i>&nbsp;";
                    break;
                case "help":
                    strAlertStyle = "info";
                    bShowTitle = false;
                    strFontIcon = "<i class='fa fa-question-circle'></i>&nbsp;";
                    break;
                case "error":
                case "danger":
                    strFontIcon = "<i class='fa fa-exclamation-circle'></i>&nbsp;";
                    break;               
                case "success":
                    strFontIcon = "<i class='fa fa-check-circle'></i>&nbsp;";
                    break;
          
            }


            myMessage.AlertStyle = strAlertStyle;
            myMessage.FontIcon = strFontIcon;
            myMessage.Title = "<strong>" + strTitle + "!</strong>&nbsp;";


            string strHtml = GetBootstrapMessageHtml(myMessage);

            return (MarkupString)strHtml;

        } // GetBottstrapMessage

        public static string GetBootstrapMessageHtml(BootstrapMessage myMessage)
        {
            
            var strHtml = String.Empty;
            var sbHtml = new StringBuilder();

            sbHtml.Append("<div class='alert alert-" + myMessage.AlertStyle + "'");


            if (myMessage.FontSize == "compact")
            {
                sbHtml.Append(" style='padding-top: 2px; padding-bottom: 2px; font-size: small;'");
            }
            else
            {
                sbHtml.Append(" style ='font-size: " + myMessage.FontSize + "'");
            }
            sbHtml.Append(" >");

            if (myMessage.Closeable)
            {
                sbHtml.Append("<a href='#' class='close' data-dismiss='alert' aria-label='close'>&times;</a>");
            }

            sbHtml.Append(myMessage.FontIcon);

            if (myMessage.ShowTitle)
            {
                sbHtml.Append(myMessage.Title);
            }

            if (myMessage.MainMessage.Length > 0)
            {
                sbHtml.Append(myMessage.MainMessage);
            }


            if (myMessage.AdditionalMessage.Length > 0)
            {
                if (myMessage.MainMessage.Length > 0)
                {
                    sbHtml.Append("<br/>");
                }
                sbHtml.Append(myMessage.AdditionalMessage);
            }


            sbHtml.Append("</div>");

            strHtml = sbHtml.ToString();
            return (strHtml);

        } //GetBootstrapMessageHtml

        public static MarkupString GetBootstrapJumbotron(string strMainTitle = "", string strSubTitle = "", string strMessage = "")
        {
            var strHtml = String.Empty;
            var sbHtml = new StringBuilder();
            int partCount = 0;

            sbHtml.Append("<div class='bg-light p-5 rounded-lg m-3'>");
            
            if (strMainTitle.Trim().Length > 0)
            {
                sbHtml.Append("<h1 class='display-4'>");
                sbHtml.Append(strMainTitle);
                sbHtml.Append("</h1>");
                partCount++;
            }
            if (strSubTitle.Trim().Length > 0)
            {
                sbHtml.Append("<p class='lead'>");
                sbHtml.Append(strSubTitle);
                sbHtml.Append("</p>");
                partCount++;
            }

            if (partCount>0)
            {
                 sbHtml.Append("<hr class='my-4'>");
            }

            if (strMessage.Trim().Length > 0)
            {
                sbHtml.Append("<p>");
                sbHtml.Append(strMessage);
                sbHtml.Append("</p>");
            }

            sbHtml.Append("</div>");            
            
            strHtml = sbHtml.ToString();

            return (MarkupString)strHtml;
        } // Jumbotron Message

       


    } // class
} // namespace
