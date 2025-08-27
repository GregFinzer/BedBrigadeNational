# Getting Started

## Repository Setup

### 1.  Request a Community License of SyncFusion
https://www.syncfusion.com/sales/communitylicense

### 2.  Create a login with API Layer (this is used for language translation)
https://apilayer.com/marketplace/language_translation-api

### 3.  Sign Up with Location IQ (this is used for Geolocation lookup)
https://my.locationiq.com/

### 4.  Install This Software
* Git:  https://git-scm.com/downloads
* Chocolatey package manager:  https://chocolatey.org/install
* <a href="https://community.chocolatey.org/packages/drawio" target="_blank">Draw.io</a> for the diagrams
* <a href="https://community.chocolatey.org/packages/visualstudio2022community">Visual Studio 2022</a> for the IDE.
* <a href="https://community.chocolatey.org/packages/sql-server-express" target="_blank">SQL Server Express</a> for local database development.
* To run the project the <a href="https://community.chocolatey.org/packages/visualstudio2022-workload-netweb">ASP.NET Workload</a> is required. 
* .NET 8 SDK:  https://dotnet.microsoft.com/
* To run the NUnit tests, <a href="https://www.jetbrains.com/resharper/">Resharper</a> or <a href="https://marketplace.visualstudio.com/items?itemName=NUnitDevelopers.NUnit3TestAdapter">NUnit Test Adapter</a> is required
* Install SyncFusion:  https://www.syncfusion.com/account/downloads
* Install Web Compiler Visual Studio Extension.  This is required for right clicking on the theme.scss and generating the theme.css and theme.min.css
* Install your favorite database editor for SQL Server Express such as <a href="https://community.chocolatey.org/packages/sql-server-management-studio">SQL Server Management Studio</a> or <a href="https://community.chocolatey.org/packages/databasenet">Database .NET</a>

### 5.  Clone the repository
On the command line run as administrator in the directory you want to clone it to:

```dos
git clone https://github.com/GregFinzer/BedBrigadeNational
```

### 6. Install entity framework tooling
* Open the Solution in Visual Studio 2022
* Open the NuGet Package Manager
* Install the Entity Framework Command Line Tool: 
    ```dos
    dotnet tool install --global dotnet-ef
    ```

### 7. Set Gold License Key
In order to run the NUnit Tests and check for Quality Locally, please set a Windows Environment variable of GOLD to what is in this document:  Bed Brigade National Website Information.docx
* Login to the SmarterAsp.NET FTP Site using credentials given.
* Download the Secrets Folder.
* Open the file Bed Brigade National Website Information.docx to see the Gold Suite License Key.

### 8. Set Syncfusion License Key
* Set an environment variable of Syncfusion to your community license.

### 9. Run the Application
* Close and Reopen Visual Studio (this is necessary after setting the environment variables).
* Right click the BedBrigade.Client
* Set as startup project
* Press the play button
    * It will automatically create the database and seed the data.

### 10. Set TranslationApiKey
* Click Login
* Login with national.admin@bedbrigade.org and Password
* Go to Administration -> Configuration
* Search for TranslationApiKey
* Set the value to your API key from https://apilayer.com/marketplace/language_translation-api

### 11. Set GeoLocationApiKey
* Click Login
* Login with national.admin@bedbrigade.org and Password
* Go to Administration -> Configuration
* Search for GeoLocationApiKey
* Set the value to your API key from https://my.locationiq.com/dashboard#accesstoken