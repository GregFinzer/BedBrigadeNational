# Developer Gotchas

## Blazor Gotchas
* **Custom Validation Attributes do not work.**  At one point I tried to do custom phone validation and custom email validation.  If the data is invalid when loaded it dies when submitting the form.  It does nothing and shows no error.
* **Blazor 8 Root Path Variable Defect.**  Blazor 8 and higher requires the nonfile attribute in a variable path.  Example:

        `@page "/{mylocation}/{mypageName:nonfile}"`
* **Custom Authentication Broken in Blazor 8.** Blazor 8 needs to be jury rigged to get authentication to work.  See:  https://github.com/GregFinzer/Blazor8Auth

## Syncfusion Gotchas
* **No grid custom validation.** When doing a dialog for add or edit  for Syncfusion, it cannot do any type of custom validation.  If you try to display errors in the middle of a template, it does not work.

