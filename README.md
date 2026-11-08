# PART-3-SIKHO-NKOSI

# RaceDay

RaceDay is a full-stack event management platform for the South African road running, walking and cycling community. Event Organisers create and manage events, categories and race results. Participants browse upcoming events, enter a category, and follow their own enrolments and race history.

This repository is **Part 3** of the PROG6212 Portfolio of Evidence. It contains everything built across the project: the RESTful API (Part 2, extended in Part 3), the ASP.NET Core MVC website, the unit tests, the Docker setup and the CI workflow.

**Video presentation:** [...........)

![CI passing](........)

## Contents

- [Features](#features)
- [How the system fits together](#how-the-system-fits-together)
- [Run with Docker](#run-with-docker)
- [Run locally in Visual Studio](#run-locally-in-visual-studio)
- [Azure Blob Storage](#azure-blob-storage)
- [Authentication and roles](#authentication-and-roles)
- [Website pages](#website-pages)
- [API endpoints](#api-endpoints)
- [Unit tests](#unit-tests)
- [Continuous integration](#continuous-integration)
- [Docker details](#docker-details)
- [Design decisions and differences](#design-decisions-and-differences)
- [Known limitations](#known-limitations)

## Features

**For Organisers**
- A dashboard showing the events they created, how many are upcoming, and total enrolments per event
- Create, edit and delete events (name, description, date, location, province, distance, event type of Run, Walk or Cycle, and an optional route map link)
- Upload an event banner image when creating or editing an event
- Add age or distance categories to each event, with an entry fee and number of places
- See everyone enrolled for an event, with their chosen category and a colour-coded enrolment status
- Capture and update finish times and finishing positions after an event

**For Participants**
- A home page listing upcoming events, with search, event type and province filters
- An event page with full details, the available categories, the route link, and public results once the event has finished
- Enter an event by choosing a category (the enrolment is recorded immediately) and cancel an upcoming enrolment
- A My Events page showing every event they entered and its enrolment status
- A My Results page showing event name, date, category, finish time and position, for example "47th out of 312 finishers"
- A profile page to view and update their details and upload a profile picture

**For everyone**
- Register and log in through the website, with a distinct menu and set of pages for each role
- Logging out clears the session, and logged-out users are redirected to the login page
- A consistent RaceDay theme built with Bootstrap, with colour-coded statuses (green Confirmed, amber Pending, red Cancelled)
- Friendly messages for invalid input and for problems reaching the server, never raw error text

## How the system fits together

```
Browser  -->  RaceDay.Web (MVC website)  -->  RaceDay.API (REST API)  -->  SQL Server
                                                      |
                                                      +--> Azure Blob Storage (images)
```

- The website never touches the database and contains no copied business logic. Every read and write goes through the API.
- The website never talks to Azure. Images are sent to the API, which stores them in Azure Blob Storage.
- In Docker, three containers run together: SQL Server, the API and the website.

Project structure:

- `RaceDay.API.slnx` - the Visual Studio solution
- `RaceDay.API/` - the Web API (controllers, models, EF Core context, migrations, session authentication, Azure image storage)
- `RaceDay.Web/` - the MVC website (controllers, views, the API client, the RaceDay theme)
- `RaceDay.API.Tests/` - the xUnit tests
- `api.Dockerfile` and `web.Dockerfile` - one Dockerfile per application
- `docker-compose.yml` - starts the database, API and website together
- `.env.example` - a template for the private `.env` settings file
- `.github/workflows/ci.yml` - the continuous integration workflow

## Run with Docker

You need Docker Desktop installed and running. Ports 8080 and 5141 must be free.

1. Clone this repository and open a terminal in the repository folder (the one containing `docker-compose.yml`).
2. Optional, for image uploads: copy `.env.example` to a new file called `.env` and put your Azure Blob Storage connection string after `AZURE_STORAGE_CONNECTION_STRING=`. In PowerShell: `Copy-Item .env.example .env`. Without it, everything works except uploading banner and profile pictures, which shows a friendly message.
3. Start everything with one command:

```
docker compose up --build
```

When the logs show both the API and the website listening, open:

- The website: http://localhost:8080
- The API and its Swagger page: http://localhost:5141/swagger

Notes:
- The first run downloads images and builds both applications, which can take 5 to 15 minutes. Later runs take seconds.
- The API creates its own database tables when it starts. While SQL Server is waking up you may see "The database is not ready yet" messages. This is normal.
- To stop, press Ctrl+C and run `docker compose down`. Your data is kept in a Docker volume.
- To delete all data and start fresh, run `docker compose down -v`.

## Run locally in Visual Studio

You need Visual Studio with the ASP.NET workload, the .NET 10 SDK and SQL Server LocalDB (installed with Visual Studio).

1. Open `RaceDay.API.slnx`.
2. Right-click the solution, choose Properties, then Multiple startup projects, and set both `RaceDay.API` and `RaceDay.Web` to Start.
3. For image uploads, right-click `RaceDay.API`, choose Manage User Secrets, and add your connection string:

```json
{
  "AzureStorage": {
    "ConnectionString": "YOUR-AZURE-CONNECTION-STRING"
  }
}
```

4. Press F5. The API creates the `RaceDayDB` database on LocalDB by itself using its EF Core migrations, and the website opens in your browser.

The API's Swagger page is at https://localhost:7083/swagger. The database connection string is in `RaceDay.API/appsettings.json`, and the address the website uses to reach the API is `ApiSettings:BaseUrl` in `RaceDay.Web/appsettings.json`.

## Azure Blob Storage

Banner images and profile pictures are stored in Azure Blob Storage.

To set it up: create a storage account in the Azure portal, create a container called `raceday-images` with the anonymous access level set to Blob (so the images can be shown on web pages), and copy the connection string from the Access keys page. Use it as `AZURE_STORAGE_CONNECTION_STRING` in `.env` (Docker) or in User Secrets (local). The connection string is a secret and is never committed to this repository.

How an upload works:
1. The user picks an image on the website.
2. The website sends the file to the API (`POST /api/events/{eventId}/banner` or `POST /api/users/me/profile-picture`).
3. The API checks the file (JPG, PNG or WEBP, up to 5 MB), uploads it to the container and saves its web address in the database.
4. The website shows the image using that stored address.

## Authentication and roles

There are two roles, Organiser and Participant, chosen when registering. Passwords are hashed with BCrypt and never stored in plain text.

**In the API:** logging in creates a server-side session that stores the user's ID and role, and the browser holds only a session cookie. A custom authentication handler reads the session on every request, and endpoints use role checks such as `[Authorize(Roles = "Organiser")]`. Some endpoints also check ownership, so an Organiser can only change their own events, categories and results, and a Participant can only cancel their own enrolments. A request with no session gets 401, and the wrong role or someone else's resource gets 403.

**In the website:** the website keeps its own server-side session that stores the user's ID, role and name, plus the API's session cookie, which it sends with every call to the API so the API treats the request as that user. Logging out ends the API session and clears the website session. Pages are protected by a filter: logged-out users are redirected to the login page (and returned to the page they wanted after logging in), and the wrong role sees an "access denied" page. If the API is restarted and the session is lost, the website sends the user back to the login page with a clear message.

## Website pages

- Everyone: `/` (upcoming events with filters), `/Events/Details/{id}`, `/Account/Login`, `/Account/Register`
- Organiser: `/Organiser/Dashboard`, `/ManageEvents` (create, edit, delete), `/ManageCategories?eventId=`, `/Organiser/Enrolments?eventId=`, `/Organiser/Results?eventId=`
- Participant: `/MyEvents`, `/MyResults`
- Both roles: `/Profile`
