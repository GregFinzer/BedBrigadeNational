# Coding Standards

## Naming Conventions
Please use the naming conventions given here:
https://cheatography.com/gregfinzer/cheat-sheets/c-naming-conventions/

## C# Coding Standards
* Please follow the C# coding standards given here: https://cheatography.com/gregfinzer/cheat-sheets/c-coding-standards/
* Pull Requests are automatically validated for code quality by the unit tests using the <a href="https://kellermansoftware.com/products/static-code-analysis">Kellerman Code Quality Analysis</a>. 

## Blazor Coding Standards
* Never run custom validation onblur.  Run validation when the submit button is clicked.  Allow the user to fix mistakes before submission.
* Avoid having more than one bootstrap alert per component.
* Avoid getting all records for an entity in Entity Framework. Unless it is known that the data will not grow beyond 100 records, avoid doing a get all and then having a where clause with a filter. Instead do a where clause in the data service. Paging might also need to be implemented.