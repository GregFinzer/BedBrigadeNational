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

## Branching and Continuous Integration
* feature branch -> develop -> main
* Pull requests are validated with <a href="https://github.com/GregFinzer/BedBrigadeNational/blob/develop/.github/workflows/pull_request_validation.yml">GitHub actions</a> by running Unit Tests and reviewing <a href="https://kellermansoftware.com/products/static-code-analysis">code quality</a>.  
* When a PR is merged into the develop branch a <a href="https://github.com/GregFinzer/BedBrigadeNational/blob/develop/.github/workflows/develop_bedbrigadedev.yml">GitHub action</a> is run and it is deployed to the development environment.

## Development Process
* Create Feature Branches with the title of the story in camel case like feature/editHomePage
* Seed default data for story cards
* Create Unit Tests for business logic using Arrange, Act, Assert pattern
* Run all tests locally with <a href="https://www.jetbrains.com/resharper/">Resharper</a> or <a href="https://marketplace.visualstudio.com/items?itemName=NUnitDevelopers.NUnit3TestAdapter">NUnit Test Adapter</a> 
* Check in your feature branch and create a PR
* Have someone review your PR branch (unless the PR is completely cosmetic, the you can approve your own PR)
* The PR approver can merge or you can merge

