# Getting Started

## Repository Setup

### 1.  Request a Community License of SyncFusion
https://www.syncfusion.com/sales/communitylicense

### 2.  Install This Software
* Git:  https://git-scm.com/downloads
* Chocolatey package manager:  https://chocolatey.org/install
* <a href="https://community.chocolatey.org/packages/drawio" target="_blank">Draw.io</a> for the diagrams
* <a href="https://community.chocolatey.org/packages/visualstudio2022community">Visual Studio 2022</a> for the IDE.
* <a href="https://community.chocolatey.org/packages/sql-server-express" target="_blank">SQL Server Express</a> for local database development.
* To run the project the <a href="https://community.chocolatey.org/packages/visualstudio2022-workload-netweb">ASP.NET Workload</a> is required. 
* .NET 7 SDK:  https://dotnet.microsoft.com/
* To run the NUnit tests, <a href="https://www.jetbrains.com/resharper/">Resharper</a> or <a href="https://marketplace.visualstudio.com/items?itemName=NUnitDevelopers.NUnit3TestAdapter">NUnit Test Adapter</a> is required
* Install SyncFusion:  https://www.syncfusion.com/account/downloads

### 3.  Clone the repository
On the command line run as administrator in the directory you want to clone it to:

```dos
git clone https://github.com/GregFinzer/BedBrigadeNational
```

### 4. Install entity framework tooling
* Open the Solution in Visual Studio 2022
* Open the NuGet Package Manager
* Install the Entity Framework Command Line Tool: 
    ```dos
    dotnet tool install --global dotnet-ef
    ```

### 5. Set Gold License Key
In order to run the NUnit Tests and check for Quality Locally, please set a Windows Environment variable of Gold to what is in this document:  Bed Brigade National Website Information.docx
* Login to the SmarterAsp.NET FTP Site using credentials given.
* Download the Secrets Folder.
* Open the file Bed Brigade National Website Information.docx to see the Gold Suite License Key.

### 6. Seed the data
* Open the solution in Visual Studio 
* On the menu, select View -> Other Windows -> Package Manager Console
* Enter:  cd bedbrigade.server
* Enter:  dotnet ef database update

### 7. Run the Application
* Right click the solution
* Set startup projects
    * Order the server first and then the client
    * Set both to start
* Press the play button

