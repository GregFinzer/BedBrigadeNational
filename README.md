# The Bed Brigade National Website

We are looking :mag_right: for volunteer developers to help with this effort.  Please email  <a href="mailto:gfinzer@hotmail.com">Greg Finzer</a> if interested.

In 2023-2024 we are developing a national website for The Bed Brigade.  Bed Brigade of Grove City, Ohio is a 501 3c registered charity.  Bed Brigade builds and delivers Twin XL beds to people in need.  80% of the beds delivered serve children that do not have a bed.  Several chapters of Bed Brigade opened up throughout Ohio in 2022.  There is a need  for a national website for all Bed Brigade Chapters to use. 
See the this page for more information about Bed Brigade: https://www.bedbrigadecolumbus.org/about-us
 
This is the current website that runs in Orchard Core:
https://www.bedbrigadecolumbus.org/

The national web site is being built in Blazor Server.  NET Core 8 is being used.

**Overall Direction**
* The Bed Brigade national website will follow the Goodwill model where users will search a bed brigade near me by entering in a zip code
* Each location of Bed Brigade will manage their home page, bed requests, volunteers, stories, news, users, and assembly instructions
* The styling of the header and footer will be minimal so that each Bed Brigade location can have their own colors.

**Business Pain**
* Currently the only Bed Brigade location that has a website is Grove City.  All other locations are using either Facebook or an email address and phone number.  This is not optimal as everything is a manual process for handling volunteers, Bed Requests, scheduling, donations and contacts. 
* The current Orchard Core CMS is too complicated to use.
* The current volunteer page is sub optimal as it allows people to volunteer on days where there is no cut, build, or delivery happening.
* There is no scheduling system which results in a out order of delivery schedule.
* No tax forms are sent for donations.
* There is currently no bulk email for: emailing all volunteers, emailing just delivery vehicle volunteers, emailing volunteer reminders, emailing bed request contacts, emailing delivery teams, performing followup, and performing announcements.

<a href="https://ci.appveyor.com/project/GregFinzer/bedbrigadenational">
  <img src="https://ci.appveyor.com/api/projects/status/9m16d94gudguouv2?svg=true" alt="AppVeyor Status" height="50">
</a>
<a href="https://github.com/GregFinzer/BedBrigadeNational/actions/workflows/develop_bedbrigadedev.yml">
	<img src="https://github.com/GregFinzer/BedBrigadeNational/actions/workflows/develop_bedbrigadedev.yml/badge.svg?branch=develop" alt="Develop Status" height="50">
</a>

Current Website running in Orchard CMS:  https://www.bedbrigadecolumbus.org/

* [Architecture](Documentation/Architecture.md)
* [Getting Started](Documentation/Getting%20Started.md)
* [Entity Framework](Documentation/Entity%20Framework.md)
* <a href="https://github.com/GregFinzer/BedBrigadeNational/raw/main/Documentation/Design/Estimates.xlsx" target="_blank">Estimates</a>
* [Implementation Plan](Documentation/Implementation%20Plan.md)
* [Project Plan](Documentation/Project%20Plan.md)
* [References](Documentation/References.md)
* [Resources for Learning Blazor 8](Documentation/Resources%20for%20Learning%20Blazor%208.md)
* [Roles](Documentation/Roles.md)
* <a href="https://bedbrigade.slack.com" target="_blank">Slack Workspace</a> used for project communication
* <a href="https://trello.com/b/SfXILMoU/bed-brigade" target="_blank">Trello Board</a> used for Kanban

 
