using Syncfusion.Blazor.Notifications;

namespace BedBrigade.Client.Components
{
    public class ToastOptions
    {
        public string Title { get; set; }
        public string CssClass { get; set; }
        public string Icon { get; set; }
        public string Content { get; set; }
        public SfToast ToastObj { get; set; }
        public int Timeout { get; set; } = 6000; 
    }
}
