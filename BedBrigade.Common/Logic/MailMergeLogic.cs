using BedBrigade.Common.Enums;
using BedBrigade.Common.Exceptions;
using BedBrigade.Common.Models;
using System.Text;
using System.Web;

namespace BedBrigade.Common.Logic;

public class MailMergeLogic : IMailMergeLogic
{
    const string mergeDelimiter = "%%";

    private List<string> _validMergeFields = new List<string>()
    {
        "%%BedRequest.NumberOfBedsWaitingSum%%",
        "%%Donation.FullName%%",
        "%%Donation.TotalAmount%%",
        "%%Donation.Year%%",
        "%%ItemizedDonations%%",
        "%%Volunteer.FirstName%%",
        "%%Volunteer.LastName%%",
        "%%Volunteer.Email%%",
        "%%Volunteer.Phone%%",
        "%%Volunteer.AttendChurch%%",
        "%%Volunteer.OtherLanguagesSpoken%%",
        "%%Volunteer.OrganizationOrGroup%%",
        "%%Schedule.GroupName%%",
        "%%Schedule.EventName%%",
        "%%Schedule.EventType%%",
        "%%Schedule.EventNote%%",
        "%%Schedule.EventDateScheduled%%",
        "%%Schedule.EventDurationHours%%",
        "%%Schedule.Address%%",
        "%%Schedule.City%%",
        "%%Schedule.State%%",
        "%%Schedule.PostalCode%%",
        "%%Schedule.OrganizerName%%",
        "%%Schedule.OrganizerEmail%%",
        "%%Schedule.OrganizerPhone%%",
        "%%BedRequest.FirstName%%",
        "%%BedRequest.NumberOfBeds%%",
        "%%BedRequest.GenderAge%%",
        "%%User.FirstName%%",
        "%%User.LastName%%",
        "%%User.Role%%",
        "%%User.Email%%",
        "%%User.Phone%%",
        "%%Location.LocationId%%",
        "%%Location.Name%%",
        "%%Location.BuildAddress%%",
        "%%Location.BuildCity%%",
        "%%Location.BuildState%%",
        "%%Location.BuildPostalCode%%",
        "%%Location.MailingAddress%%",
        "%%Location.MailingCity%%",
        "%%Location.MailingState%%",
        "%%Location.MailingPostalCode%%",
        "%%Location.Route%%",
        "%%BaseUrl%%",
        "%%NewsletterNameForQuery%%",
        "%%EmailForQuery%%",
        "%%CustomMessage%%",
        "%%SignUp.VehicleType%%",
        "%%SignUp.SignUpNote%%",
        "%%SignUp.NumberOfVolunteers%%",
        "%%ContactUs.FirstName%%",
        "%%ContactUs.LastName%%",
        "%%ContactUs.Phone%%",
        "%%ContactUs.Email%%",
        "%%ContactUs.Message%%",
    };

    public void CheckForInvalidMergeFields(string template)
    {
        StringBuilder sb = new StringBuilder();
        var invalidMergeFields = GetListOfInvalidMergeFields(template);
        if (invalidMergeFields.Count > 0)
        {
            throw new MailMergeException("Invalid merge fields: " + string.Join(", ", invalidMergeFields));
        }
    }

    private List<string> GetListOfInvalidMergeFields(string emailTemplate)
    {
        List<string> invalidMergeFields = new List<string>();
        List<string> mergeFields = GetMergeFields(emailTemplate);
        foreach (string mergeField in mergeFields)
        {
            if (!IsValidMergeField(mergeField))
            {
                invalidMergeFields.Add(mergeField);
            }
        }
        return invalidMergeFields;
    }

    private List<string> GetMergeFields(string emailTemplate)
    {
        List<string> mergeFields = new List<string>();
        int startIndex = 0;
        while (startIndex >= 0)
        {
            startIndex = emailTemplate.IndexOf(mergeDelimiter, startIndex);
            if (startIndex >= 0)
            {
                int endIndex = emailTemplate.IndexOf(mergeDelimiter, startIndex + mergeDelimiter.Length);
                if (endIndex >= 0)
                {
                    string mergeField = emailTemplate.Substring(startIndex, endIndex - startIndex + mergeDelimiter.Length);
                    mergeFields.Add(mergeField);
                    startIndex = endIndex + 1;
                }
            }
        }
        return mergeFields;
    }

    private bool IsValidMergeField(string mergeField)
    {
        return _validMergeFields.Contains(mergeField);
    }

    public StringBuilder ReplaceBaseUrl(StringBuilder sb, string baseUrl)
    {
        baseUrl = baseUrl.TrimEnd('/');
        sb = sb.Replace("%%BaseUrl%%", baseUrl);
        return sb;
    }

    public StringBuilder ReplaceNewsletterNameForQuery(StringBuilder sb, string newsletterName)
    {
        sb = sb.Replace("%%NewsletterNameForQuery%%", HttpUtility.UrlEncode(newsletterName));
        return sb;
    }

    public string ReplaceEmailForQuery(string body, string email)
    {
        
        body = body.Replace("%%EmailForQuery%%", HttpUtility.UrlEncode(email));
        return body;
    }

    public StringBuilder ReplaceVolunteerFields(Volunteer volunteer, Schedule schedule, StringBuilder sb)
    {
        sb = sb.Replace("%%Volunteer.FirstName%%", volunteer.FirstName);
        sb = sb.Replace("%%Volunteer.LastName%%", volunteer.LastName);
        sb = sb.Replace("%%Volunteer.Email%%", volunteer.Email);
        sb = sb.Replace("%%Volunteer.Phone%%", volunteer.Phone.FormatPhoneNumber());
        sb = sb.Replace("%%Volunteer.AttendChurch%%", volunteer.AttendChurch ? "Yes" : "No");
        sb = sb.Replace("%%Volunteer.OtherLanguagesSpoken%%", volunteer.OtherLanguagesSpoken);
        sb = sb.Replace("%%Volunteer.OrganizationOrGroup%%", volunteer.OrganizationOrGroup);

        return sb;
    }

    public StringBuilder ReplaceSignUpFields(Schedule schedule, SignUp signUp, StringBuilder sb)
    {
        if (schedule.EventType == EventType.Delivery)
        {
            sb = sb.Replace("%%SignUp.VehicleType%%", "Vehicle Type: " + signUp.VehicleType.ToString());
        }
        else
        {
            sb = sb.Replace("%%SignUp.VehicleType%%", string.Empty);
        }

        sb = sb.Replace("%%SignUp.NumberOfVolunteers%%", signUp.NumberOfVolunteers.ToString());
        sb = sb.Replace("%%SignUp.SignUpNote%%", signUp.SignUpNote ?? string.Empty);
        return sb;
    }

    public StringBuilder ReplaceScheduleFields(Schedule entity, StringBuilder sb)
    {
        sb = sb.Replace("%%Schedule.GroupName%%", entity.GroupName);
        sb = sb.Replace("%%Schedule.EventName%%", entity.EventName);

        if (entity.EventName != StringUtil.InsertSpaces(entity.EventType.ToString()) && entity.EventType != EventType.Other)
        {
            sb = sb.Replace("%%Schedule.EventType%%", StringUtil.InsertSpaces(entity.EventType.ToString()));
        }
        else
        {
            sb = sb.Replace("%%Schedule.EventType%%", String.Empty);
        }

        sb = sb.Replace("%%Schedule.EventNote%%", entity.EventNote);
        sb = sb.Replace("%%Schedule.EventDateScheduled%%", entity.EventDateScheduled.ToString("MM/dd/yyyy h:mm tt"));
        sb = sb.Replace("%%Schedule.EventDurationHours%%", entity.EventDurationHours + " hours");
        sb = sb.Replace("%%Schedule.Address%%", entity.Address);
        sb = sb.Replace("%%Schedule.City%%", entity.City);
        sb = sb.Replace("%%Schedule.State%%", entity.State);
        sb = sb.Replace("%%Schedule.PostalCode%%", entity.PostalCode);
        sb = sb.Replace("%%Schedule.OrganizerName%%", entity.OrganizerName);
        sb = sb.Replace("%%Schedule.OrganizerEmail%%", entity.OrganizerEmail);
        sb = sb.Replace("%%Schedule.OrganizerPhone%%", entity.OrganizerPhone.FormatPhoneNumber());
        return sb;
    }

    public StringBuilder ReplaceContactUsFields(ContactUs contactUs, StringBuilder sb)
    {
        sb = sb.Replace("%%ContactUs.FirstName%%", contactUs.FirstName);
        sb = sb.Replace("%%ContactUs.LastName%%", contactUs.LastName);
        sb = sb.Replace("%%ContactUs.Phone%%", contactUs.Phone.FormatPhoneNumber());
        sb = sb.Replace("%%ContactUs.Email%%", contactUs.Email);
        sb = sb.Replace("%%ContactUs.Message%%", contactUs.Message);
        return sb;
    }

    public StringBuilder ReplaceBedRequestFields(BedRequest entity, StringBuilder sb)
    {
        sb = sb.Replace("%%BedRequest.FirstName%%", entity.FirstName);
        sb = sb.Replace("%%BedRequest.NumberOfBeds%%", entity.NumberOfBeds.ToString());
        sb = sb.Replace("%%BedRequest.GenderAge%%", entity.GenderAge);
        return sb;
    }

    public StringBuilder ReplaceUserFields(User user, StringBuilder sb)
    {
        sb = sb.Replace("%%User.FirstName%%", user.FirstName);
        sb = sb.Replace("%%User.LastName%%", user.LastName);
        sb = sb.Replace("%%User.Role%%", user.Role);
        sb = sb.Replace("%%User.Email%%", user.Email);
        sb = sb.Replace("%%User.Phone%%", user.Phone.FormatPhoneNumber());
        return sb;
    }

    public StringBuilder ReplaceLocationFields(Location location, StringBuilder sb)
    {
        sb = sb.Replace("%%Location.LocationId%%", location.LocationId.ToString());
        sb = sb.Replace("%%Location.Name%%", location.Name);

        sb = sb.Replace("%%Location.BuildAddress%%", location.BuildAddress);
        sb = sb.Replace("%%Location.BuildCity%%", location.BuildCity);
        sb = sb.Replace("%%Location.BuildState%%", location.BuildState);
        sb = sb.Replace("%%Location.BuildPostalCode%%", location.BuildPostalCode);

        sb = sb.Replace("%%Location.MailingAddress%%", location.MailingAddress);
        sb = sb.Replace("%%Location.MailingCity%%", location.MailingCity);
        sb = sb.Replace("%%Location.MailingState%%", location.MailingState);
        sb = sb.Replace("%%Location.MailingPostalCode%%", location.MailingPostalCode);

        sb = sb.Replace("%%Location.Route%%", location.Route);
        return sb;
    }
}

