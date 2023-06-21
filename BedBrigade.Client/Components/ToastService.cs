namespace BedBrigade.Client.Components
{
    public class ToastService
    {
        public event Action<ToastOptions> ToastInstance;

        public void Open(ToastOptions options)
        {
            // Invoke ToastComponent to update and show the toast with options 
            this.ToastInstance.Invoke(options);
        }
    }
}
