# Entity Framework

## Tool Installation
In NuGet package manager console install dotnet-ef which is the entity framework command line tool:
```dos
dotnet tool install --global dotnet-ef
```

## Adding a migration
1. In Visual Studio, open the NuGet Package Manager Console
2. Go to the BedBrigade/Server directory.  Example:
3. Add a migration.  Example:
    ```dos
    dotnet ef migrations add Addresses
    ```
4. Update the database.  Example:
    ```dos
    dotnet ef database update
     ```
     
## Removing the last migration
```dos
dotnet ef migrations remove
```


