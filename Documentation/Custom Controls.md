# Custom Controls

There are a few custom HTML Controls that have been created to be used with Bed Brigade.  In each of these cases, the HTML for the Content Management System is modified or being completely replaced with something else.

* <a href="#image-rotator" target="_blank">Image Rotator</a>
* <a href="#bbcarousel" target="_blank">Bed Brigade Carousel</a>
* <a href="#bbschedule" target="_blank">Bed Brigade Schedule</a>
* <a href="#bb-iframe" target="_blank">Bed Brigade iFrame</a>


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

<hr />


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

<hr />

## BBSchedule
The Bed Brigade Schedule is an HTML replacement of a div.  It is identified by *data-component="bbschedule"*.  It generates a block of HTML code.  The *data-months* determines how many months of the schedule to display.

### Example Control Code
This is an example of the Bed Brigade Schedule from RockCityPolarisHome.html

```html
<div data-component="bbschedule" data-months="3" id="rockp-schedule"></div>
```

### Example Output
```html
<div class="rockp-home-bipanel-container">
    <div class="rockp-home-bipanel-left">
        <h3 class="rockp-home-bipanel-title">MAY</h3>
    </div>
    <div class="rockp-home-bipanel-right">
        <p>
            <a class="rockp-a-bold" href="/rock-city-polaris/volunteer/66">Build: 5/3/2025, 9:00 AM</a>
        </p>
        <p class="rockp-home-bipanel-info">
            <a class="rockp-a" href="https://www.google.com/maps/search/?api=1&query=171+E.+5th+Ave%2C+Columbus%2C+OH%2C+" target="_blank">171 E. 5th Ave, Columbus, OH</a>
        </p>
	<br />
        <p>
            <a class="rockp-a-bold" href="/rock-city-polaris/volunteer/78">Delivery: 5/10/2025, 9:00 AM</a>
        </p>
        <p class="rockp-home-bipanel-info">
            <a class="rockp-a" href="https://www.google.com/maps/search/?api=1&query=171+E.+5th+Ave%2C+Columbus%2C+OH%2C+" target="_blank">171 E. 5th Ave, Columbus, OH</a>
        </p>
	<br />
    </div>
</div>
```

## BB IFrame
The Bed Brigade IFrame is an HTML replacement of a div.  It is identified by *data-component="bbiframe"*.  It generates a block of HTML code. The reason why we have an iFrame Control is that the Syncfusion Rich Text editor will strip out iFrame tags when saving the content.  It is an HTML replacement of a div.  All attributes are required and they must be in the exact order or nothing will match.  when the word youtube appears in the source it will generate additional attributes for YouTube. 

### Attributes 
These have to be in this exact order without any other attributes!
* **data-component** - Identifies it as a bbiframe
* **id** - Unique id
* **width** - The width for the iFrame
* **height** - The height for the iFrame
* **src** - The path for the src of the iFrame

### Example Control Code
This is an example of the Bed Brigade Schedule from RockCityPolarisHome.html
```html
<div data-component="bbiframe" id="rockp-subscribe-iframe" width="100%" height="250px" src="/SubscribeToNewsletter?locationId=3&newsletterName=Rock+City+Polaris+Newsletter"></div>
```

### Example Output
```html
<iframe id="rockp-subscribe-iframe" src="/SubscribeToNewsletter?locationId=3&newsletterName=Rock+City+Polaris+Newsletter"
		style="border:none; width:100%; height:250px;">
</iframe>
```

### Example YouTube Control Code
```html
<div data-component="bbiframe" id="assembly-iframe" width="300px" height="250px" src="https://www.youtube.com/embed/uErhxwvxNsg"></div>
```

### Example YouTube Output
```html
<iframe id="assembly-iframe" src="https://www.youtube.com/embed/uErhxwvxNsg" style="border:none; width:300px; height:250px;" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen=""></iframe>
```