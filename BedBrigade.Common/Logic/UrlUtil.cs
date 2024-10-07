using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace BedBrigade.Common.Logic
{
    public struct UrlContent
    {
        public int PositioningCount=0;
        public bool IsParameterizedUrl;
        public int QueryCount;
        public string AcceptedLocationRoute;
        public bool isLocationRoute;
        public string AcceptedPageName;
        public string FullUrl;
        public string SorryPageUrl;
        public string ErrorMessage;

        public UrlContent()
        {
            PositioningCount = 0;
            IsParameterizedUrl = false;
            QueryCount = 0;
            AcceptedLocationRoute = String.Empty;
            isLocationRoute = false;
            AcceptedPageName = String.Empty;
            FullUrl = String.Empty;
            SorryPageUrl = String.Empty;
            ErrorMessage = String.Empty;
        }
    } // struct


    public static class UrlUtil
    {
        public static UrlContent ValidateUrlContent(string url, bool IsLocationRoute = false)
        {
            var queryParams = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(); ;
            UrlContent ValidatedUrl = new UrlContent();
            ValidatedUrl.FullUrl = url.ToString();
            ValidatedUrl.isLocationRoute = IsLocationRoute;

            if (string.IsNullOrEmpty(url))
            {
                ValidatedUrl.ErrorMessage = "URL cannot be null or empty";
                return (ValidatedUrl);
            }

            Uri uri = new Uri(url, UriKind.RelativeOrAbsolute);

            // Get Positioning (Path) parameters
            string[] pathSegments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            ValidatedUrl.PositioningCount = pathSegments.Length;

            // Get Query parameters
            var query = uri.Query;
            
            if (!string.IsNullOrEmpty(query))
            {
                queryParams = QueryHelpers.ParseQuery(query);
                ValidatedUrl.QueryCount = queryParams.Count;
            }
            // Parameterized URL ?

            if (ValidatedUrl.PositioningCount == 0 && ValidatedUrl.QueryCount == 0)
            { // no parameters
                ValidatedUrl.IsParameterizedUrl = false;
                return (ValidatedUrl);
            }
            else
            {
                ValidatedUrl.IsParameterizedUrl = true;
            }

            // Validate Positioning Parameters Values

            CheckPositioningParameters(ref ValidatedUrl, IsLocationRoute, pathSegments);

            if (ValidatedUrl.QueryCount > 0)
            {
                switch (ValidatedUrl.QueryCount)
                {
                    case 1: // single query parameter - location - NOTE: no location validation at this time
                        if (queryParams.TryGetValue("location", out var locationValue))
                        {
                            if (!String.IsNullOrEmpty(locationValue))
                            {
                                ValidatedUrl.AcceptedLocationRoute = StringUtil.IsNull(locationValue,"");
                            }
                            ValidatedUrl.AcceptedPageName = GetPageNameWithoutQuery(uri);
                        }
                                              
                        break;
                    default:
                        ValidatedUrl.ErrorMessage = "URL contains unknown query parameters";
                        break;
                }
            } // query parameters

            // set sorry page URL depend of context
            SetSorryUrl(ref ValidatedUrl);
           
            return (ValidatedUrl);

        }//ValidateUrlContent

        public static void CheckPositioningParameters(ref UrlContent ValidatedUrl, bool IsLocationRoute, string[] pathSegments)
        {
            // Validate Positioning Parameters Values

            if (ValidatedUrl.PositioningCount > 0)
            {

                switch (ValidatedUrl.PositioningCount)
                {
                    case 1: // single parameter: location or page
                        if (IsLocationRoute)
                        {
                            ValidatedUrl.AcceptedLocationRoute = pathSegments[0];
                        }
                        else
                        {
                            ValidatedUrl.AcceptedPageName = pathSegments[0];
                        }
                        break;

                    case 2: // dual parameters: location/page

                        ValidatedUrl.AcceptedLocationRoute = pathSegments[0];
                        ValidatedUrl.AcceptedPageName = pathSegments[1];
                        break;

                    default:
                        // unknown parameters
                        ValidatedUrl.ErrorMessage = "URL contains unknown positioning parameters";
                        break;
                }
            } // Positioning Count > 0
        } // Check Positioning


        public static void SetSorryUrl(ref UrlContent ValidatedUrl)
        {
            var SorryUrl = String.Empty;
            // national context - location could be ignored
            if (ValidatedUrl.AcceptedLocationRoute.Equals("National", StringComparison.CurrentCultureIgnoreCase))
            {
                SorryUrl = $"/Sorry/{ValidatedUrl.AcceptedPageName}";
            }
            else // location context
            {
                if (ValidatedUrl.AcceptedLocationRoute.Length > 0)
                {
                    SorryUrl = $"/{ValidatedUrl.AcceptedLocationRoute}/Sorry";
                }
                else
                {
                    SorryUrl = "/Sorry";
                }

                if (!String.IsNullOrEmpty(ValidatedUrl.AcceptedPageName))
                {
                    SorryUrl += $"/{ValidatedUrl.AcceptedPageName}";
                }
            }

            ValidatedUrl.SorryPageUrl = SorryUrl;
        } // Create Sorry Url


        public static string GetPageNameWithoutQuery(Uri uri)
        {
            // Get the absolute path and split by '/'
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Get the last segment, which may include query parameters
            var lastSegment = segments.LastOrDefault();

            // Check if last segment is null or empty, and then strip off any query parameters if present
            if (!string.IsNullOrEmpty(lastSegment))
            {
                var indexOfQuery = lastSegment.IndexOf("?");
                if (indexOfQuery >= 0)
                {
                    lastSegment = lastSegment.Substring(0, indexOfQuery);
                }
            }

            return lastSegment;

        } // Get Page Banem without query
        


    } // class
} // namespace
