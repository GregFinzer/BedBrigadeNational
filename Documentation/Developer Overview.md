# Developer Overview

## Solution Overview
* The **BedBrigade.Client** project is a Blazor Web App that in Server Interactive Render Mode.
* The **BedBrigade.Common** project contains the Models and shared business logic.
* The **BedBrigade.Data** project uses Entity Framework for CRUD operations.
* The **BedBrigade.Tests** project contains unit tests and code quality tests.

## BedBrigade.Client
* This project has both client facing and administration pages.  It has a custom content management system (CMS).  
* MainLayout.razor is used for client facing pages.  It displays a Header and Footer using the respective Header.razor and Footer.razor components.  
* AdminLayout.razor is used after logging in.  The only difference between the MainLayout and AdminLayout is the AdminLayout does not have a footer.
* CheckAuthorization.razor is used in the Routes.razor to display a message if they are not authorized.
* Index.razor displays all of the static content for all locations using the MyBody component.
* Because there is so much startup code.  The program.cs calls off to StartupLogic.cs which handles everything.  The StartupLogic.cs creates and seeds the database if it does not exist.
