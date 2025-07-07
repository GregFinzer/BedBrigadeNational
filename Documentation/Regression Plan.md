# Regression Plan

## Public Pages

### Mobile Testing
* [ ] Using the Chrome Dev Tools select Samsung Galaxy 8+.
* [ ] All National pages should look good and not be cut off.
* [ ] All Grove City pages should look good and not be cut off.
* [ ] All Rock City Polaris pages should look good and not be cut off.
* [ ] When Requesting a Bed it should be possible to see all validation messages without being cut off by submitting an empty form.
* [ ] It should be possible to Request a Bed in the mobile width by filling out all required fields.
* * [ ] When Volunteering it should be possible to see all validation messages without being cut off by submitting an empty form.
* [ ] It should be possible to Volunteer in the mobile width by filling out all required fields.
* [ ] When doing a Contact Us it should be possible to see all validation messages without being cut off by submitting an empty form.
* [ ] It should be possible to Contact US in the mobile width by filling out all required fields.

### Tablet Testing
* [ ] Using the Chrome Dev Tools select iPad Mini.
* [ ] All National pages should look good and not be cut off.
* [ ] All Grove City pages should look good and not be cut off.
* [ ] All Rock City Polaris pages should look good and not be cut off.
* [ ] When Requesting a Bed it should be possible to see all validation messages without being cut off by submitting an empty form.
* [ ] It should be possible to Request a Bed in the tablet width by filling out all required fields.
* * [ ] When Volunteering it should be possible to see all validation messages without being cut off by submitting an empty form.
* [ ] It should be possible to Volunteer in the tablet width by filling out all required fields.
* [ ] When doing a Contact Us it should be possible to see all validation messages without being cut off by submitting an empty form.
* [ ] It should be possible to Contact US in the tablet width by filling out all required fields.

### Home Page
* [ ] All links from the home page work without a page not found error.
* [ ] On the home page, when the user chooses Spanish the home page immediately translates into Spanish.
* [ ] On the home page, using the Chrome Dev Tools select Samsung Galaxy 8+.  The page should have Request A Bed, Volunteer, and Donate buttons at the top.  The menu should be a hamburger menu.

### Find Bed Brigade Near Me
* [ ] Searching for 43228 shows Grove City and Rock City Polaris
* [ ] Searching for 96813 shows message:  *No locations found within 30 miles of 96813*

### Request A Bed
* [ ] The user is required to fill out required fields.
* [ ] The email must have a valid syntax and top level domain. Try putting in test@test.999 with all other fields valid.
* [ ] The phone must have a valid prefix. Try putting in (999) 123-4567 with all other fields valid.
* [ ] The user is required to fill out the recaptcha
* [ ] Verify after submitting a Bed Request.
    - [ ] The Bed Request will appear in the Manage Bed Requests page.
    - [ ] The user will receive a confirmation email in production within 5 minutes.  In development or local there will be evidence in the log that an email would have been sent.
    - [ ] Version 2, The scheduler for the location will receive an email within 5 minutes. In development or local there will be evidence in the log that an email would have been sent.  If there is no scheduler for a location then send to the location admin.

### Volunteer
* [ ] The user is required to fill out required fields.
* [ ] The email must have a valid syntax and top level domain. Try putting in test@test.999 with all other fields valid.
* [ ] The phone must have a valid prefix. Try putting in (999) 123-4567 with all other fields valid.
* [ ] The user is required to fill out the recaptcha
* [ ] Verify after submitting a Volunteer Sign-up.
    - [ ] The Volunteer will appear in the Manage Volunteer page.
    - [ ] The Volunteer will appear in the Sign Up Page for the event.
    - [ ] The user will receive a confirmation email in production within 5 minutes.  In development or local there will be evidence in the log that an email would have been sent.
    - [ ] Version 2, The Location Admin for the location will receive an email within 5 minutes. In development or local there will be evidence in the log that an email would have been sent.    
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

### National News 
* [ ] The user should be able to navigate to the national news page and view the detail of the news.

### Grove City Stories 
* [ ] The user should be able to navigate to the Grove City Stories and view the detail of a individual story

### Events
* [ ] It should be possible to signup for a specific event by clicking on the Event Name.

## Admin Pages
For all the pages below login as National Admin unless otherwise specified.

### Manage Bed Requests
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export
* [ ] It should be possible to Search
* [ ] It should be possible to Download Delivery Sheet
* [ ] It should be possible to Sort Waiting Closest


### Manage Configuration
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export
* [ ] It should be possible to Search

### Manage Contacts
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export
* [ ] It should be possible to Search

### Manage Donations
* [ ] It should be possible to sort the columns by the column headers.
* [ ] Where applicable, it should be able to filter.
* [ ] Where applicable, it should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.
* [ ] It should be possible to Send Tax Form.

### Manage Locations
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.
* [ ] It should be possible to Send Tax Form.

### Manage Metro Areas
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.
* [ ] It should be possible to Send Tax Form.


### Manage Pages
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.
* [ ] It should be possible to Rename a page.

### Manage News
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.
* [ ] It should be possible to Rename a News Item.

### Manage Newsletters
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.

### Manage Stories
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.
* [ ] It should be possible to Rename a Story Item.

### Manage Schedules
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.

### Manage Sign-Ups
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.

### Manage Volunteers
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.

### Bulk Email
* [ ] It should be possible to send an email to yourself.
* [ ] Look in the log.  The email should send within 5 minutes.  If the EmailuseFileMock is true then it will simply be logged.

### Bulk Text Messages
* [ ] It should be possible to send an text to yourself.
* [ ] Look in the log.  The text should send within 5 minutes.  If the SmsUseFileMock is true then it will simply be logged.
* [ ] Look in the Text Messages screen.  The text should be there within 5 minutes.

### Manage Users
* [ ] It should be possible to sort the columns by the column headers.
* [ ] It should be able to filter.
* [ ] It should be possible to group by a column.
* [ ] When the Reset button is clicked it should go back to the default grouping and sorting.
* [ ] It should be possible to Add a record. The grid should update.
* [ ] It should be possible to Edit a record.  The grid should update.
* [ ] It should be possible to Delete a record. The grid should update.
* [ ] It should be possible to Print.
* [ ] It should be possible to PDF Export.
* [ ] It should be possible to Export to Excel.
* [ ] It should be possible to Csv Export.
* [ ] It should be possible to Search.

### Manage Media
* [ ] It should be possible to Upload a File
* [ ] It should be possible to Copy a File
* [ ] It should be possible to Cut a File
* [ ] It should be possible to Rename a File
* [ ] It should be possible to get the Info for a File
* [ ] It should be possible to Search for a File

### Server Information
* [ ] It should be possible to view Server Information
* [ ] It should be possible to Clear the Cache

### Hard Refresh
* [ ] Go to the Manage Volunteers Page.  When doing a Ctrl-F5 in the Browser it should automatically log back in and go to the Volunteer page.