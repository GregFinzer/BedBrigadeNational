
namespace BedBrigade.Common.Exceptions;

public class MailMergeException : Exception
{
    public MailMergeException()
    {
    }

    public MailMergeException(string message)
        : base(message)
    {
    }

    public MailMergeException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

