using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.SpeakIt.Tests
{
    [TestFixture]
    public class CreateTests
    {
        private SpeakItLogic _logic = new SpeakItLogic();

        [Test, Ignore("Unignore to create resource strings")]
        public void CreateLocalizationStringsTest()
        {
            CreateParms parms = new CreateParms();
            parms.TargetDirectory = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Components");
            parms.WildcardPattern = "*.razor";
            parms.ExcludeDirectories = TestHelper.ExcludeDirectories;
            parms.ExcludeFiles = TestHelper.ExcludeFiles;
            parms.ResourceFilePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Resources", "en-US.yml");
            _logic.CreateLocalizationStrings(parms);
        }
    }
}
