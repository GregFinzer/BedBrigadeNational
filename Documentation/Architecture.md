# Architecture

See the Design Files in:
BedBrigadeNational\Documentation\Design

## Information Architecture Diagram

![Information Architecture Diagram](Design/Information%20Architecture%20Diagram.png)

<hr />

## Application Infrastructure Diagram
![Application Infrastructure Diagram](Design/Application%20Infrastructure%20Diagram.png)

<hr />

## Application Architecture Diagram
![Application Architecture Diagram](Design/Application%20Architecture%20Diagram.png)

## Entity Relationship Diagram
![Entity Relationship Diagram](Design/EntityRelationshipDiagram.png)

## Bed Request Flow
![Bed Request Sequence Diagram](Design/Bed%20Request%20Sequence%20Diagram.png)

## Volunteer Sign-up Flow
![Volunteer Signup Sequence Diagram](Design/Volunteer%20Signup%20Sequence%20Diagram.png)

## Contact Us Flow
![Contact Us Sequence Diagram](Design/Contact%20Us%20Sequence%20Diagram.png)

## Index.razor Call Tree
The Bed Brigade National Website has a light content management system.  The Index.razor page displays html loaded from the database.  Here is the call tree.

```
OnParametersSetAsync()
    -> PopulateCurrentLocationAndPageName()
        -> LocationDataService.GetActiveLocations()
    -> LoadLocationPage()
        -> LocationDataService.GetLocationByRouteAsync()
        -> LoadDefaultContent() (if English)
            -> ContentDataService.GetAsync()
            -> ReplaceHtmlControls()
                -> LoadImageService.SetImagesForHtml()
                -> CarouselService.ReplaceCarousel()
                -> ScheduleControlService.ReplaceScheduleControl()
                -> ReplaceIFrame()
        -> LoadContentByLanguage() (if something other than English)
            -> ContentTranslationDataService.GetAsync()
            -> ReplaceHtmlControls()
            -> TranslationDataService.GetTranslation() (for the title of the page)
            
```          

## Language Change Call Tree
The user has the ability to select a different language in the Header.razor.  When the SelectedLanguage changes, all other listening components are notified that the language has changed.  The individual components are responsible for loading different content.  The header component loads the content for English or for other languages.

```
Header.razor
	-> SelectedCulture
		-> LocalStorageService.SetItemAsync("language", value);
		-> LanguageService.CurrentCulture = CultureInfo.GetCultureInfo(value);
	-> LanguageService.OnLanguageChanged (event)
		-> LoadContent
		-> LocationDataService.GetLocationByRoute
		-> LoadDefaultContent (if English)
			-> ContentDataService.GetAsync
		-> LoadContentByLanguage (if other than English)
			-> ContentTranslationDataService.GetAsync
```
