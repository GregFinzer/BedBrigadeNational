﻿namespace BedBrigade.Client.Services
{
    public interface IHeaderLocationState
    {
        string Location { get; set; }
        event Action OnChange;
    }
}
