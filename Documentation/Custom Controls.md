# Custom Controls

There are a few custom HTML Controls that have been created to be used with Bed Brigade.  In each of these cases, the HTML for the Content Management System is modified or being completely replaced with something else.

## Image Rotator
This is an image control that is identified by the word *ImageRotator* in the id.  It specifies a normal image path in the media directory but when the Index.razor.cs loads, the src will be replaced with a different path every 30 minutes.  You can have multiple image rotators on a page but they must have a different id for each image rotator.  The home page has a single image rotator for Grove City.  There is also an ImageRotator razor Blazor Custom Component that is used by the RotatorContainer Razor Custom Component that is used by BedBrigadeNearMe and other pages.

### Example Code
Notice in the example below the id of headerImageRotator in GroveCityHome.html
```html
<img
  src="media/grove-city/pages/home/headerImageRotator/TwoWildBeds.jpg"
  id="headerImageRotator"
  alt="sliderRotator"
  class="Slide-home-rotator"
  height="620"
  width="70%"
/>
```

### Flow of Replacement
Index.razor.cs LoadDefaultContent or LoadContentByLanguage &rarr; LoadImagesService.SetImagesForHtml

## BBCarousel
The Bed Brigade Carousel is an HTML replacement of a div.  It is identified by *data-component="bbcarousel"*.  It generates a <a href="https://getbootstrap.com/docs/5.0/components/carousel/" target="_blank">Bootstrap 5.x Carousel</a> with all of the image references and controls.  JavaScript is used to set the interval and to ensure that the images change regardless if the carousel is in the viewport.  This was done because the images would not change at all without it in Blazor.

### Example Code
This is an example of the Bed Brigade Carousel from RockCityPolarisHome.html

```html
<div data-component="bbcarousel" id="rockp-home-carousel" src="media/rock-city-polaris/pages/home/carousel"></div>
```

### Attributes 
These have to be in this exact order without any other attributes!
* **data-component** - Identifies it as a bbcarousel
* **id** - Unique id that must end with the word carousel
* **src** - The path to the images for the carousel

### Flow of replacement
1. Index.razor.cs LoadDefaultContent or LoadContentByLanguage &rarr; CarouselService.ReplaceCarousel
2. Index.razor.cs OnAfterRenderAsync &rarr; BedBrigadeUtil.runCarousel
