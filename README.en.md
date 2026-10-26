# ASP.NET Core + PostgreSQL on Master da Web

[![Deploy on Master da Web](https://panelv2.masterdaweb.com/deploy/button-en.svg)](https://panelv2.masterdaweb.com/templates/aspnet)

An ASP.NET Core (.NET 10) app with PostgreSQL through Entity Framework Core, its migrations on every deploy, private files on Object Storage and the login keys kept in the database.

| Service | What it runs |
| --- | --- |
| `web` | the `src/MwApp` project; `./efbundle` (the EF Core migrations) runs once per deploy, before the new version takes traffic |

The database comes as `ConnectionStrings__DefaultConnection`, in Npgsql's format, which the app reads with `GetConnectionString("DefaultConnection")`: through the pooler for the app, straight to the server for migrations. The private bucket comes as `MW_STORAGE_*`; each download link is signed and expires in 15 minutes.

`POST /api/files` (field `file`) stores a file; `GET /api/files` lists them with their links. The home page does the same from the browser, with ASP.NET's antiforgery token.

## How the platform builds it

There is no Dockerfile: the platform's build finds the solution (`MwApp.sln`), picks its web project, publishes it self-contained and runs the app as an unprivileged user on the port the platform gives (`$PORT`). Since the project references `Microsoft.EntityFrameworkCore.Design`, the build also makes `efbundle`, the migrations.

- A solution with more than one web app? Add the variable `PROJECT_FILE` (for example `src/Shop.Web/Shop.Web.csproj`), marked **Available at build time**.
- `BUILD_CONFIGURATION` and `MSBUILD_VERBOSITY_LEVEL` work too, as on Heroku.
- Rather build it yourself? A `Dockerfile` at the root takes precedence.

## Make it yours

1. In the console, open the project and follow **Your code**: copy this
   template to your GitHub and connect the copy to the project.
2. Change `src/MwApp` and push: every push deploys, with no downtime.

## Run it on your machine

```bash
dotnet run --project src/MwApp      # uses the PostgreSQL in appsettings.json
```

A new migration:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add NameOfTheChange --project src/MwApp
```

## Good to know

- Logins and forms use ASP.NET Data Protection, whose keys live in the `DataProtectionKeys` table: every instance opens what any other encrypted, before and after each deploy. Without it, each instance would keep keys of its own on its container's disk and logins would drop at every restart.
- The app gets the visitor's scheme and address from the platform proxy's `X-Forwarded-*` headers (`ASPNETCORE_FORWARDEDHEADERS_ENABLED`), so links and redirects come out as `https`.

*[Português](README.md)*
