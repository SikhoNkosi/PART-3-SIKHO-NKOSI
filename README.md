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
