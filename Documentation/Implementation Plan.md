# Implementation Plan

## Project Goals
* #1 Rule for the project is that it has to be simple enough for a retired Elementary School Teacher to use.
    * Giant readable admin dashboard buttons with icons
* High Performance Blazor Front End
    * Business logic should be performed by the back end
    * Administration will be a separate assembly that should be lazy loaded.  https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-lazy-load-assemblies?view=aspnetcore-7.0
    * Use CDN for Bootstrap
    * Follow configuration here: https://github.com/dotnet/aspnetcore/issues/42284
    * Also see:  https://github.com/dotnet/aspnetcore/issues/41909
    * Use versioning for CSS and JavaScript
* Mobile first responsive design    
* Minimum Viable Product Deliverables

## Project Execution
* Story cards will be as small as possible
* Story cards will be a vertical slice
* Seed default data for story cards
