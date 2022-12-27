# Getting Started

## Repository Setup

### 1.  Install This Software
* Git:  https://git-scm.com/downloads
* Chocolatey package manager:  https://chocolatey.org/install
* <a href="https://community.chocolatey.org/packages/drawio" target="_blank">Draw.io</a> for the diagrams
* <a href="https://community.chocolatey.org/packages/visualstudio2022community">Visual Studio 2022</a> for the IDE.
* <a href="https://community.chocolatey.org/packages/sql-server-express" target="_blank">SQL Server Express</a> for local database development.
* To run the project the <a href="https://community.chocolatey.org/packages/visualstudio2022-workload-netweb">ASP.NET Workload</a> is required. 
* .NET 7 SDK:  https://dotnet.microsoft.com/
* To run the NUnit tests, <a href="https://www.jetbrains.com/resharper/">Resharper</a> or <a href="https://marketplace.visualstudio.com/items?itemName=NUnitDevelopers.NUnit3TestAdapter">NUnit Test Adapter</a> is required

### 2.  Clone the repository
On the command line run as administrator in the directory you want to clone it to:

```dos
git clone https://github.com/GregFinzer/BedBrigadeNational
```

### 3. Install entity framework tooling
* Open the Solution in Visual Studio 2022
* Open the NuGet Package Manager
* Install the Entity Framework Command Line Tool: 
    ```dos
    dotnet tool install --global dotnet-ef
    ```
### 4. Run the Application
* Press the play button
* Execute Swagger Locally:  http://localhost:5125/swagger/index.html
    