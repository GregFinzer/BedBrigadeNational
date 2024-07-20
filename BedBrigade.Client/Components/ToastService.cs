namespace BedBrigade.Client.Components
{
    //https://www.syncfusion.com/forums/173239/how-to-keep-toast-visible-when-navigating-to-another-page
    public class ToastService
    {
        public event Action<ToastOptions> ToastInstance;

        public void Open(ToastOptions options)
        {
            // Invoke ToastComponent to update and show the toast with options 
            this.ToastInstance.Invoke(options);
        }

        public void Success(string title, string content)
        {
            Open(new ToastOptions
            {
                Title = title,
                Content = content,
                CssClass = "e-toast-success",
                Icon = "e-success toast-icons"
            });
        }

        public void Error(string title, string content)
        {
            Open(new ToastOptions
            {
                Title = title,
                Content = content,
                CssClass = "e-toast-danger",
                Icon = "e-error toast-icons"
            });
        }
    }
}
