# Localization

TLDR; This document outlines the localization process for translating static strings in a project using resource files and the aksoftware.localization.multilanguages NuGet package, which includes a language translation tool. It also details how the custom SpeakItLogic.cs automates modifications to Razor files and resource files, while providing guidelines for addressing localization test failures and instructions on how to localize new strings within Razor files.

## Overview
Localization also known as language translation is one of the more complicated pieces of the project.  There are three major pieces which perform the language translation.

1.  We are using resource files to contain the translations into different languages for static strings.  See the BedBrigade.Client\Resources folder.  We are using the NuGet package <a href="https://www.nuget.org/packages/aksoftware.localization.multilanguages">aksoftware.localization.multilanguages</a> with this <a href="https://github.com/aksoftware98/multilanguages">GitHub Repo</a>.  The advantage to using this instead of regular resx files is that there is a language translator: https://akmultilanguages.azurewebsites.net
2.  Greg developed the SpeakItLogic.cs with several complicated functionalities.
    a.  It has the ability to automatically modify both the razor file and the associated razor.cs file and create key value pairs in the en-US.yml.  
    b.  It tests to ensure strings inside all razor files are localized. 
    c.  It tests to make sure there are no duplicate values in the key value pairs.
    d.  It tests to ensure any keys that are in use in the razor files can be found in the resource file.
    e.  It tests to ensure all keys are in use in the razor files.

## Failing Tests
* **VerifyAllRazorFilesAreLocalized** - If this test is failing it means that there are new strings in your razor file that need to be localized. Follow the instructions below on "How to localize a new string in a Razor file"
* **VerifyDuplicateKeys** - If this test is failing it means that there are new strings that need to be localized and if they were to be created automatically, two values would be created with the same key.  You may have to create the key value pairs manually in BedBrigade.Client\Resources\en-US.yml and then modify your razor file manually.
* **VerifyAllKeysCanBeFound** - If this test is failing it means that you manually typed in a key in your razor file, and it does not exist in the BedBrigade.Client\Resources\en-US.yml file, or you deleted a key value pair in the en-US.yml file that was in use.  Keys are case sensitive.  Correct your typo or add the key to the en-US.yml file.
* **VerifyNoUnusedKeys** - If this test is failing, it means that you have keys in your BedBrigade.Client\Resources\en-US.yml file that are not being used in your razor files.  Most likely you deleted some code in a Razor file or you deleted the entire .razor file.  Remove the key value pair from the en-US.yml file.


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