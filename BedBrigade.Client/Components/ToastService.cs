namespace BedBrigade.Client.Components
{
    //https://www.syncfusion.com/forums/173239/how-to-keep-toast-visible-when-navigating-to-another-page
    public class ToastService
    {
        public event Action<ToastOptions>? ToastInstance;

        public void Open(ToastOptions options)
        {
            // Invoke ToastComponent to update and show the toast with options 
            ToastInstance?.Invoke(options);
        }

        public void Success(string title, string content)
        {
            Open(new ToastOptions
            {
                Title = title,
                Content = content,
                CssClass = "e-toast-success custom-toast",
                Icon = "e-success toast-icons"
            });
        }

        public void Warning(string title, string content)
        {
            Open(new ToastOptions
            {
                Title = title,
                Content = content,
                CssClass = "e-toast-warning custom-toast",
                Icon = "e-success toast-icons"
            });
        }

        public void Error(string title, string content)
        {
            Open(new ToastOptions
            {
                Title = title,
                Content = content,
                CssClass = "e-toast-danger custom-toast",
                Icon = "e-error toast-icons",
                Timeout = 30000
            });
        }
    }
}
