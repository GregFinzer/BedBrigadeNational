# Getting Started

## Repository Setup

### 1.  Request a Community License of SyncFusion
https://www.syncfusion.com/sales/communitylicense

### 2.  Sign Up with Location IQ (this is used for Geolocation lookup)
https://my.locationiq.com/

### 3a.  Install Development Software for Windows (see 3b if installing on Linux)
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

### 3b.  Installing Development Software for Linux
Here is my desktop environment setup if you want to install a bunch of stuff.  
https://github.com/GregFinzer/Linux/blob/main/Documentation/MySetup.md

The minimum is below.

```bash
# Git command line
sudo apt install git -y

# GitHub Desktop (Like TortoiseGit)
sudo flatpak install flathub io.github.shiftey.Desktop -y

# All the drawings are in this
sudo flatpak install flathub drawio -y

# Like Visual Studio but better
sudo snap install rider --classic 

# Data Beaver (Like Database.NET or SQL Server Management Studio)
sudo flatpak install flathub io.dbeaver.DBeaverCommunity -y

# Install .NET Version 8 SDK 
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh 
chmod +x ./dotnet-install.sh 
./dotnet-install.sh --channel 8.0 

# Install VS Code from Microsoft Official 
# From here:  https://code.visualstudio.com/docs/setup/linux
sudo apt-get install wget gpg &&
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg &&
sudo install -D -o root -g root -m 644 microsoft.gpg /usr/share/keyrings/microsoft.gpg &&
rm -f microsoft.gpg

sudo tee /etc/apt/sources.list.d/vscode.sources > /dev/null <<EOF
Types: deb
URIs: https://packages.microsoft.com/repos/code
Suites: stable
Components: main
Architectures: amd64,arm64,armhf
Signed-By: /usr/share/keyrings/microsoft.gpg
EOF

sudo apt install apt-transport-https &&
sudo apt update &&
sudo apt install code -y
```

**Install SQL Server in a Docker Container**
https://github.com/GregFinzer/Linux/blob/main/Scripts/InstallSqlServerOnKUbuntu.sh

**Add dotnet to the path and set other environment variables**

```bash
nano ~/.bashrc
```

Add this at the end

```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools
export Syncfusion="Replace With Your Key"
export GOLD="Replace With Your Key"
export BedBrigadeConnectionString="Server=localhost,1433;Database=bedbrigade;User Id=sa;Password=Str0ng!Passw0rd123;TrustServerCertificate=True;"

```

Reload

```bash
source ~/.bashrc
```

**Add dotnet to VS Code**

In the VS Code Terminal

```bash
nano ~/.profile
```

Add to the end

```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools
```

Close and reopen VS Code

### 4.  Clone the repository
On the command line run as administrator in the directory you want to clone it to:

```dos
git clone https://github.com/GregFinzer/BedBrigadeNational
```

### 5. Install entity framework tooling
* Open the Solution in Visual Studio 2022
* Open the NuGet Package Manager
* Install the Entity Framework Command Line Tool: 
    ```dos
    dotnet tool install --global dotnet-ef
    ```

### 6. Set Gold License Key
In order to run the NUnit Tests and check for Quality Locally, please set a Windows Environment variable of GOLD to what is in this document:  Bed Brigade National Website Information.docx
* Login to the SmarterAsp.NET FTP Site using credentials given.
* Download the Secrets Folder.
* Open the file Bed Brigade National Website Information.docx to see the Gold Suite License Key.

**Setting Environment Variables in Linux**
In a terminal edit the file
```bash
nano ~/.bashrc
```

Add these at the end of the file with the actual key
```bash
export Syncfusion="zzz"
export GOLD="zzz"
export BedBrigadeConnectionString="Server=localhost,1433;Database=bedbrigade;User Id=sa;Password=Str0ng!Passw0rd123;TrustServerCertificate=True;"
```

Refresh the changes
```bash
source ~/.bashrc
```

Do the same for the profile
```bash
nano ~/.profile
```

Add these at the end of the file with the actual key
```bash
export Syncfusion="zzz"
export GOLD="zzz"
export BedBrigadeConnectionString="Server=localhost,1433;Database=bedbrigade;User Id=sa;Password=Str0ng!Passw0rd123;TrustServerCertificate=True;"
```
Refresh the changes
```bash
source ~/.profile
```


### 7. Set Syncfusion License Key
* Set an environment variable of Syncfusion to your community license.

### 8. Run the Application
* Close and Reopen Visual Studio (this is necessary after setting the environment variables).
* Right click the BedBrigade.Client
* Set as startup project
* Press the play button
    * It will automatically create the database and seed the data.

### 9. Set GeoLocationApiKey
* Click Login
* Login with national.admin@bedbrigade.org and Password
* Go to Administration -> Configuration
* Search for GeoLocationApiKey
* Set the value to your API key from https://my.locationiq.com/dashboard#accesstoken
