# Generate Migrations

## Install CLI tools for Entity Framework Core
```cmd
dotnet tool install --global dotnet-ef --version 8.0.8
```

or update

```cmd
dotnet tool update --global dotnet-ef --version 8.0.8
```

## Add Migration
Select Data.<Provider> folder and run following command for each provider:

```cmd
dotnet ef migrations add <migration-name>
```
