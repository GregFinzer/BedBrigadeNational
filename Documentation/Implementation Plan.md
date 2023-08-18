# Implementation Plan

## Project Goals
* #1 Rule for the project is that it has to be simple enough for a retired Elementary School Teacher to use.
    * Giant readable admin dashboard buttons with icons
* High Performance Blazor Server
    * All the rendering is performed on the server.
    * Caching - A entities are automatically cached.  The cache is cleared for that entity type when an entity is created or updated.  
       * Use CDN for Bootstrap
    * Use versioning for CSS and JavaScript
* Mobile first responsive design    
* Minimum Viable Product Deliverables

## Agile Execution
* Story cards will be as small as possible
* Story cards will be a vertical slice

## Development Process
* Create Feature Branches with the title of the story in camel case like feature/editHomePage
* Seed default data for story cards
* Create Unit Tests for business logic
