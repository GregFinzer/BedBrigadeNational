using BedBrigade.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Data.Services
{
    public interface IHeaderMessageService
    {
        Task<ServiceResponse<string>> GetManageEntitiesLegendText(string entitiesName);
    }
}
