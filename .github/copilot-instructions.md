# Copilot Instructions for The Bed Brigade National Website

## Project Overview

BedBrigadeNational is a multi-project .NET solution that supports the Bed Brigade organization in managing bed requests, volunteer information, localization, and delivery logistics. The codebase is organized into focused projects to separate concerns and keep shared logic reusable:

| Project | Purpose |
|---------|---------|
| `BedBrigade.Client` | Blazor Server client application: UI, pages, components, and startup wiring. |
| `BedBrigade.Common` | Cross-cutting models, enums, constants, exceptions, and shared business logic. |
| `BedBrigade.Data` | Entity Framework Core data layer, data services, and migrations for persistence. |
| `BedBrigade.SpeakIt` | Localization and translation parsing utilities (text extraction, validation, transformation). |
| `BedBrigade.Tests` | Unit tests for common logic and utilities. |
| `BedBrigade.Tests.Integration` | Integration tests (import flows, delivery sheet generation, external data scenarios). |
| `BedBrigade.SpeakIt.Tests` | Tests specific to localization/translation logic. |

### Core Domain Concepts
* Bed Requests: Intake, validation, scheduling for fulfillment.
* Volunteers: Intake, managing availability, and associating with delivery or build activities.
* Localization: Structured parsing and verification of translation resources to ensure consistent UI messaging.
* Delivery & Logistics: Generating delivery sheets and supporting fulfillment workflows.

### Technology Highlights
* .NET and C# solution with layered architecture.
* Blazor client for interactive web UI.
* Entity Framework Core for data access and migrations.
* Dedicated projects for tests to enforce quality and regression safety.

### High-Level Flow
1. Data (requests/volunteers) imported or entered via the client.
2. Business rules and shared logic in `BedBrigade.Common` validate and transform models.
3. Persistence handled through `BedBrigade.Data` services and EF Core migrations.
4. Localization features in `BedBrigade.SpeakIt` ensure multilingual or standardized messaging.
5. Tests verify parsing, import routines, utilities, and integration scenarios.

# Guidance for AI Assistance
## General Guidance
When generating or modifying code:
* When adding new features, do not modify existing unrelated code unless necessary to support the new feature.
* When creating new razor pages or components, always create a separate code behind file in the same folder with a `.razor.cs` extension.
* When creating new razor pages, layout with Bootstrap and follow existing styling conventions.
* When creating new razor components and pages, favor using Syncfusion controls where possible to maintain consistency.
* Place models in the `BedBrigade.Common` project in the `\Models` folder.
* Put data related logic in services in the `BedBrigade.Data` project.
* Put business logic that is not related to CRUD operations into the `BedBrigade.Common` project.
* Put layouts in the `BedBrigade.Client` project in the `\Components\Layout` folder.
* Put reusable components in the `BedBrigade.Client` project in the `\Components` folder.
* Put pages in the `BedBrigade.Client` project in the `\Components\Pages` folder.

## Naming Conventions
* Use PascalCase for these types:  Namespace, Class, Property, Method, Constant, Enumeration Type, Enumeration Value, Event, Exception Class.
* An interface should begin with a capital I and then the pascal cased identifier.  Example:  IPerson
* A class field should begin with an underscore and then a camel cased identifier.  Use a noun or noun phrase. Example:  _firstName
* Boolean properties or fields should be named with phrases like Is or Has.  Example:  IsLoading for a property or _isLoading for a field.
* Local variables and parameters should be camelCase.
* Asynchronous methods should end with Async.
* Methods should be names with verbs or verb phrases.  Example:  GenerateDeliverySheet.
* Use `@page` with **kebab-case** routes, e.g. `@page "/bed-requests"`.

## Coding Standards
* There should be one class per file.
* The maximum length for a file is 500 lines (not counting blank lines or comments).
* The maximum length for a method is 50 lines (not counting blank lines or comments).
* Methods should have a maximum of 5 parameters.  If more than 5 parameters are needed, create a class to be used as a parameter.
* The maximum number of nesting is 4.  This applies to for, if, case, foreach and while.  
* The maximum cyclomatic complexity for a method is 21.
* If a literal string appears in a file more than 3 times, create a string constant.






