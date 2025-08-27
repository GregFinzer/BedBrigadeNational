
# Seeding Information

## Overview
* The seeding functionality allows data for the web application to be setup in the database and with images.  When the application is run locally the seeding is run every time.  For development and production it is run with the PerformSetup endpoint.  
* The root of the seeding is on Seed.cs
* All content seeding is located in SeedContentsLogic.cs
* BedRequests, Volunteers, Donations test data is seeded for development but not production.
* In the repository, the data for the seeding is located in BedBrigade.Data\Data\Seeding

## Image Seeding
* Images are copied from Data\Seeding\SeedImages to the wwwroot\Media directory if wwwroot\Media\national\logo.png does not exist. To re-copy the images locally or in development you can simply delete the logo.png.

## General Seeding
* Any new Configuration will be seeded on deployment.
* Any new Locations will be seeded on deployment.
* MetroAreas are seeded once. If a location is added, it will need to be manually added to a MetroArea.
* Roles will be seeded once.
* Users will be seeded if the user does not exist by the username.  There are different users for development and production.
* VolunteersFor are seeded once.
* Volunteers test data is seeded once for development but not production.
* DonationCampaign of General is seeded if it does not exist for the location.
* Donations test data is seeded once for development but not production.
* BedRequests test data is seeded once for development but not production.
* Schedules data is seeded once for Grove City and Rock City Polaris.  There is a production script to update the Organizer Information.
* Stories is seeded once.  The data comes from the Data\Seeding\SeedHtml directory.
* News is seeded once. The data comes from the Data\Seeding\SeedHtml directory.
* Translations are seeded once.  The data comes from the Data\Seeding\SeedTranslations directory.
* ContentTranslations are seeded once.  
* SpokenLanguages are seeded once.
* Newsletters are seeded once.

## Content Seeding
* The data comes from the Data\Seeding\SeedHtml directory.
* Content Header will be seeded if it does not exist for the location.
* Content Footer will be seeded if it does not exist for the location.
* Content AboutUs will be seeded if it does not exist for the location.
* Content HomePage will be seeded if it does not exist for the location.
* Content Donations will be seeded if it does not exist for the location.
* Content National History is seeded once if it does not exist.
* Content National Locations is seeded once if it does not exist.
* Content National Donations is seeded once if it does not exist.
* Content Assembly Instructions is seeded if it does not exist for the location.
* Content DeliveryChecklist is seeded if it does not exist for the location.
* Content TaxForm is seeded if it does not exist for the location.
* Content ThreeRotatorPageTemplate is seeded once if it does not exist.
* Content for Grove City is seeded once if it does not exist.
* Content for Rock City Polaris is seeded once if it does not exist.
* Content BedRequestConfirmationForm is seeded if it does not exist for the location.
* Content SignUpEmailConfirmationForm is seeded if it does not exist for the location.
* Content SignUpSmsConfirmationForm is seeded if it does not exist for the location.
* Content NewsletterForm is seeded if it does not exist for the location.

