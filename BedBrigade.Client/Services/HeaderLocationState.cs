namespace BedBrigade.Client.Services
{
    public class HeaderLocationState : IHeaderLocationState
    {
        private string _location;
        public string Location
        {
            get => _location;
            set
            {
                if (_location != value)
                {
                    _location = value;
                    NotifyStateChanged();
                }
            }
        }

        public event Action OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
