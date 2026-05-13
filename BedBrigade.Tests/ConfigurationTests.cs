using BedBrigade.Common.Models;

namespace BedBrigade.Tests;

[TestFixture]
public class ConfigurationTests
{
    [Test]
    public void GridDisplayValue_WhenEncrypted_ReturnsMaskedPlaceholder()
    {
        Configuration configuration = new Configuration
        {
            Encrypted = true,
            ConfigurationValue = "@KS@encrypted-value"
        };

        Assert.That(configuration.GridDisplayValue, Is.EqualTo("••••••••"));
    }

    [Test]
    public void GridDisplayValue_WhenNotEncrypted_ReturnsStoredValue()
    {
        const string value = "plain-text-value";
        Configuration configuration = new Configuration
        {
            Encrypted = false,
            ConfigurationValue = value
        };

        Assert.That(configuration.GridDisplayValue, Is.EqualTo(value));
    }
}

