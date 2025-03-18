# Localization
TLDR; The localization process for the project involves using resource files and the aksoftware.localization.multilanguages package for translating static strings and dynamic content. Automated tests ensure that all strings are localized correctly, and any updates to pages trigger content to be queued for translation, with detailed steps provided for handling validation messages, dropdowns, and seeded content.


## Overview
Localization also known as language translation is one of the more complicated pieces of the project.  There are three major pieces which perform the language translation.

1.  We are using resource files to contain the translations into different languages for static strings.  See the BedBrigade.Client\Resources folder.  We are using the NuGet package <a href="https://www.nuget.org/packages/aksoftware.localization.multilanguages">aksoftware.localization.multilanguages</a> with this <a href="https://github.com/aksoftware98/multilanguages">GitHub Repo</a>.  The advantage to using this instead of regular resx files is that there is a language translator: https://akmultilanguages.azurewebsites.net
2.  Greg developed the SpeakItLogic.cs with several complicated functionalities.
    a.  It has the ability to automatically modify both the razor file and the associated razor.cs file and create key value pairs in the en-US.yml.  
    b.  It tests to ensure strings inside all razor files are localized. 
    c.  It tests to make sure there are no duplicate values in the key value pairs.
    d.  It tests to ensure any keys that are in use in the razor files can be found in the resource file.
    e.  It tests to ensure all keys are in use in the razor files.
3. Dynamic Translation.  Whenever the user changes the text on any Page using the Manage Pages on the Administration Menu; any content that is changed will be queued to be translated.  If a user adds a new page, the content will also be queued to be translated.  See TranslationProcessorDataService.QueueContentTranslation and TranslationBackgroundService

## Failing Tests
* **VerifyAllSourceCodeFilesAreLocalized** - If this test is failing it means that there are new strings in your razor file or in your model file Required Attribute that need to be localized. Follow the instructions below on "How to localize a new string in a Razor file"
* **VerifyNoDuplicateKeys** - If this test is failing it means that there are new strings that need to be localized and if they were to be created automatically, two different values would be created with the same key.  You may have to create the key value pairs manually in BedBrigade.Client\Resources\en-US.yml and then modify your razor file manually.
* **VerifyAllKeysCanBeFound** - If this test is failing it means that you manually typed in a key in your razor file, and it does not exist in the BedBrigade.Client\Resources\en-US.yml file, or you deleted a key value pair in the en-US.yml file that was in use.  Keys are case sensitive.  Correct your typo or add the key to the en-US.yml file.
* **VerifyNoUnusedKeys** - If this test is failing, it means that you have keys in your BedBrigade.Client\Resources\en-US.yml file that are not being used in your razor files.  Most likely you deleted some code in a Razor file or you deleted the entire .razor file.  Remove the key value pair from the en-US.yml file.

## About the en-US.yml
There are three different styles of localization keys in the en-US.yml
* **Regular Key** - Example:  *AboutUs*.  This will be a Pascal cased key that originally was from an HTML element like ```<h1>About Us</h1>```
* **Dynamic Key** - Example: *DynamicApril*.  This is a Pascal cased key that has the word Dynamic in front of the word.  This translation is used in a code behind instead of an HTML  element.  When VerifyNoUnusedKeys is run it does not look for usage in code behind because we allow partial translations.  So if there is text like April 2025 then it will still translate the April.  
* **Data Annotation Key** - Example: *RequiredAmount*.  This is a Pascal cased key that has the Data Annotation name and then the property name.  All other data annotations are supported such as MaxLength, Email, Phone, etc.

## How to localize a new string in a Razor file
1.  Perform a commit to Git to your feature branch so that you can revert any changes if needed.
2.  In the project BedBrigade.SpeakIt.Tests remove the Ignore attribute in  CreateLocalizationStringsTest and run it.  It is okay to run it multiple times.  This code will create new key value pairs as needed in the en-US.yml file and it will also change your .razor file and .razor.cs file to use the localization.
3.  Go to your razor file and your razor.cs files and ensure you are happy with the modifications.  It might be that the key value pair already exists and further steps are not needed.  Check and see if BedBrigade.Client\Resources\en-US.yml was modified locally.  If you don't like the name of the key, manually change the key in your razor and in the en-US.yml.
4.  Go to this site and upload the changed en-US.yml https://akmultilanguages.azurewebsites.net/TranslateApplication
5.  Translate to es-MX
6.  Download the file and replace the existing es-MX file in BedBrigade.Client\Resources

After it has been run you will observe that the HTML in your your razor file will be changed from something like this:

```html
    <label for="firstname">First Name</label>
```

To this:

```html
    <label for="firstname">@_lc.Keys["FirstName"]</label>
```

Your razor.cs file will have the language container service injected:

```csharp
[Inject] private ILanguageContainerService _lc { get; set;
```

It will also have a call to initialize the localized component in either the OnInitialized or OnInitializedAsync method:

```csharp
protected override void OnInitialized()
{
    _lc.InitLocalizedComponent(this);
}
```

## How to localize validation messages
1.  Perform a commit to Git to your feature branch so that you can revert any changes if needed.
2. Use data attributes as you normally would on your model.

    ```C#
    [Required(ErrorMessage = "Email Address is required")]
    [MaxLength(255, ErrorMessage = "Email Address has a maximum length of 255 characters")]
    public String Email { get; set; } = string.Empty;
    ```
3.  In the project BedBrigade.SpeakIt.Tests remove the Ignore attribute in  CreateLocalizationStringsTest and run it.  It is okay to run it multiple times.  This code will create new key value pairs as needed in the en-US.yml file for your attributes.
4.  Check and see if BedBrigade.Client\Resources\en-US.yml was modified locally.  
5.  Go to this site and upload the changed en-US.yml https://akmultilanguages.azurewebsites.net/TranslateApplication
6.  Translate to es-MX
7.  Download the file and replace the existing es-MX file in BedBrigade.Client\Resources
8.  For a full example, see BedRequest.razor.cs  You will need to create a ValidationMessageStore private variable and initialize it.  Also, clear it before validating (see ClearValidationMessages).  In your IsValid method, instead of calling Validate() on the Edit Context, run this method:

    ```C#
    formIsValid = ValidationLocalization.ValidateModel(newRequest, _validationMessageStore, _lc);
    ```
## How to Translate Drop Down Lists and Other Dynamic Content
1. In BedBrigade.Client\Resources\en-US.yml add a key value pair starting with the word Dynamic for phrases and words that will appear in your dropdown.  Example:  
    ```yml
    DynamicDelivery: Delivery
    ```
2. Go to this site and upload the changed en-US.yml https://akmultilanguages.azurewebsites.net/TranslateApplication
3. Translate to es-MX
4. Download the file and replace the existing es-MX file in BedBrigade.Client\Resources
5. In your Data Service that builds your drop down list key and value, use TranslationLogic to get the translation.  Example in ScheduleDataService:

    ```C#
    private void FillEventSelects(List<Schedule> schedules)
    {
        foreach (var schedule in schedules.ToList())
        {
            FillSingleEventSelect(schedule);
        }
    }
    
    private void FillSingleEventSelect(Schedule schedule)
    {
        string? eventName = _translateLogic.GetTranslation(schedule.EventName);
        schedule.EventSelect = $"{eventName}: {schedule.EventDateScheduled.ToShortDateString()}, {schedule.EventDateScheduled.ToShortTimeString()}";
    }
    ```
## Seeding Localized Content
1.  Add the HTML file to this directory:  BedBrigade.Data\Data\Seeding\SeedHtml
2.  Modify the SeedContentsLogic.  Example: 

    ```C#
    private static async Task SeedGroveCity(DataContext context)
    {
        Log.Logger.Information("SeedGroveCity Started");
        var location = await context.Locations.FirstOrDefaultAsync(l => l.LocationId == (int)LocationNumber.GroveCity);
    
        if (location == null)
        {
            Console.WriteLine($"Error cannot find location with id: " + LocationNumber.GroveCity);
            return;
        }
    
        await SeedContentItem(context, ContentType.Header, location, "Header", "GroveCityHeader.html");
        await SeedContentItem(context, ContentType.Home, location, "Home", "GroveCityHome.html");
        await SeedContentItem(context, ContentType.Body, location, "AboutUs", "GroveCityAboutUs.html");
        await SeedContentItem(context, ContentType.Body, location, "Donations", "GroveCityDonations.html");
        await SeedContentItem(context, ContentType.Body, location, "Assembly-Instructions", "GroveCityAssemblyInstructions.html");
        await SeedContentItem(context, ContentType.Body, location, "Partners", "GroveCityPartners.html");
        await SeedContentItem(context, ContentType.Body, location, "Calendar", "GroveCityCalendar.html");
        await SeedContentItem(context, ContentType.Body, location, "Inventory", "GroveCityInventory.html");
        await SeedContentItem(context, ContentType.Body, location, "History", "GroveCityHistory.html");
    }
    ```    
3. In BedBrigade.SpeakIt.Tests.LocalizationSeedingSetup remove the ignore for the Setup and run the test.
4. Check and see if BedBrigade.Data\Data\Seeding\SeedTranslations\en-US.yml was modified locally.  
5.  Go to this site and upload the changed en-US.yml https://akmultilanguages.azurewebsites.net/TranslateApplication
6.  Translate to es-MX
7.  Download the file and replace the existing es-MX file in BedBrigade.Data\Data\Seeding\SeedTranslations
8.  In SeedTranslationsLogic class in the SeedContentTranslations method, add the file to translate.
9.  Delete all the records in the ContentTranslations table and the Translations table.
10.  Upload the en-US.yml and es-MX.yml files to the
Data\Seeding\SeedTranslations folder on Development using FTP
11.  Perform a deployment

## Adding a New Language like French, Chinese etc.
1.  Go to this site:  https://akmultilanguages.azurewebsites.net/TranslateApplication
2.  Upload BedBrigade.Client\Resources\en-US.yml
3.  Select the desired language and download to BedBrigade.Client\Resources
4.  Upload BedBrigade.Data\Data\Seeding\SeedTranslations\en-US.yml
5.  Select the desired language and download to BedBrigade.Data\Data\Seeding\SeedTranslations
6.  In BedBrigade.Data\Data\Seeding\SeedTranslationLogic you will need to alter a bunch of code to support the new language and then get those translations into development/test/production, etc.  Really it should loop through the languages other than English based on what is in the directory:  BedBrigade.Data\Data\Seeding\SeedTranslations
