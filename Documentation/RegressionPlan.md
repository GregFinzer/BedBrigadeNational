# Regression Plan

## Public Pages

### Home Page
* [ ] All links from the home page work without a page not found error.
* [ ] On the home page, when the user chooses Spanish the home page immediately translates into Spanish.
* [ ] On the home page, using the Chrome Dev Tools select Samsung Galaxy 8+.  The page should have Request A Bed, Volunteer, and Donate buttons at the top.  The menu should be a hamburger menu.

### Find Bed Brigade Near Me
* [ ] Searching for 43228 shows Grove City and Rock City Polaris
* [ ] Searching for 96813 shows message:  *No locations found within 30 miles of 90210*

### Request A Bed
* [ ] The user is required to fill out required fields.
* [ ] The email must have a valid syntax and top level domain. Try putting in test@test.999 with all other fields valid.
* [ ] The phone must have a valid prefix. Try putting in (999) 123-4567 with all other fields valid.
* [ ] The user is required to fill out the recaptcha
* [ ] Verify after submitting a Bed Request.
    - [ ] The Bed Request will appear in the Manage Bed Requests page.
    - [ ] The user will receive a confirmation email in production within 5 minutes.  In development or local there will be evidence in the log that an email would have been sent.
    - [ ] The scheduler for the location will receive an email within 5 minutes. In development or local there will be evidence in the log that an email would have been sent.  If there is no scheduler for a location then send to the location admin.

### Volunteer
* [ ] The user is required to fill out required fields.
* [ ] The email must have a valid syntax and top level domain. Try putting in test@test.999 with all other fields valid.
* [ ] The phone must have a valid prefix. Try putting in (999) 123-4567 with all other fields valid.
* [ ] The user is required to fill out the recaptcha
* [ ] Verify after submitting a Volunteer Sign-up.
    - [ ] The Volunteer will appear in the Manage Volunteer page.
    - [ ] The Volunteer will appear in the Sign Up Page for the event.
    - [ ] The user will receive a confirmation email in production within 5 minutes.  In development or local there will be evidence in the log that an email would have been sent.
    - [ ] The Location Admin for the location will receive an email within 5 minutes. In development or local there will be evidence in the log that an email would have been sent.    
    - [ ] There will be a Sign Up Reminder SMS message that is queued in the database. 

### Contact Us
* [ ] The user is required to fill out required fields.
* [ ] The email must have a valid syntax and top level domain. Try putting in test@test.999 with all other fields valid.
* [ ] The phone must have a valid prefix. Try putting in (999) 123-4567 with all other fields valid.
* [ ] The user is required to fill out the recaptcha
* [ ] Verify after submitting a Contact Us Request.
    - [ ] The Contact Us will appear in the Contact Us page.
    - [ ] The user will receive a confirmation email in production within 5 minutes.  In development or local there will be evidence in the log that an email would have been sent.
    - [ ] The Location Admin will be sent an email in production within 5 minutes.  The reply to should be the email address of the user. In development or local there will be evidence in the log that an email would have been sent.   