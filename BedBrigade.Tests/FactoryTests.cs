using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedBrigade.Common.Logic;
using KellermanSoftware.NetCachingLibrary.CacheProviders;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class FactoryTests
    {
        [Test]
        public void CanCreateAddressParser()
        {
            LibraryFactory.CreateAddressParser();
        }

        [Test]
        public void CanCreateQualityLogic()
        {
            LibraryFactory.CreateQualityLogic();
        }

        [Test]
        public void CanCreateSmartConfig()
        {
            LibraryFactory.CreateSmartConfig(new MemoryCacheProvider());
        }
    }
}
