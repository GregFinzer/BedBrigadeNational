
using BedBrigade.Data.Data.Seeding;

namespace BedBrigade.Client.Services
{
    public class LocationState : ILocationState
    {
        private string _location = SeedConstants.SeedNationalName;
        public string Location
        {
            get => _location;
            set
            {
                if (_location.ToLower() != value.ToLower())
                {
                    _location = value;
                    NotifyStateChangedAsync();
                }
            }
        }

        public event Func<Task> OnChange;

        public async Task NotifyStateChangedAsync()
        {
            if (OnChange != null)
            {
                foreach (Func<Task> handler in OnChange.GetInvocationList())
                {
                    await handler.Invoke();
                }
            }
        }
    }
}