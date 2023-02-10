namespace MessageService
{
    public interface ISmsSender
    {
        string SendSms(string from, string to, string body);
    }
}