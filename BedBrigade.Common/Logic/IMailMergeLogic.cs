using BedBrigade.Common.Models;
using System.Text;

namespace BedBrigade.Common.Logic;

public interface IMailMergeLogic
{
    void CheckForInvalidMergeFields(string template);
    StringBuilder ReplaceLocationFields(Location location, StringBuilder sb);
    StringBuilder ReplaceUserFields(User user, StringBuilder sb);
    StringBuilder ReplaceBedRequestFields(BedRequest entity, StringBuilder sb);
    StringBuilder ReplaceScheduleFields(Schedule entity, StringBuilder sb);
    StringBuilder ReplaceVolunteerFields(Volunteer volunteer, Schedule schedule, StringBuilder sb);
    StringBuilder ReplaceBaseUrl(StringBuilder sb, string baseUrl);
    StringBuilder ReplaceNewsletterNameForQuery(StringBuilder sb, string newsletterName);
    string ReplaceEmailForQuery(string body, string email);
    StringBuilder ReplaceSignUpFields(Schedule schedule, SignUp signUp, StringBuilder sb);
}

