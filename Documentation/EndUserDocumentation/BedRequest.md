# Bed Requests

## What Is a Bed Request?

A Bed Request is how a family in the community asks the Bed Brigade to build and deliver beds for their children. When a family submits a request, it enters the system and is tracked from the initial intake all the way through scheduling and delivery.

---

## Submitting a Bed Request

Any member of the public can submit a Bed Request through the Bed Brigade website. The form collects the following information:

### Contact Information

| Field | Required | Notes |
|-------|----------|-------|
| First Name | Yes | Up to 20 characters |
| Last Name | Yes | Up to 25 characters |
| Email Address | Yes | A valid email address |
| Phone Number | Yes | Format: (000) 000-0000 |

### Delivery Address

| Field | Required | Notes |
|-------|----------|-------|
| Street Address | Yes | Where the beds will be delivered |
| City | Yes | |
| State | Yes | Selected from a dropdown list |
| Zip Code | Yes | 5 digits |

### Request Details

| Field | Required | Notes |
|-------|----------|-------|
| Number of Beds | Yes | Between 1 and 99 |
| Children's Names | Yes | Full names of the children receiving beds |
| Gender and Age | Yes | Describes each child, e.g., "B/7 G/6" (Boy age 7, Girl age 6) |
| Primary Language | Yes | The family's preferred language |
| Speaks English | Conditional | Shown only if the primary language is not English |
| Special Instructions | No | Any special needs, access instructions, or requests (up to 4,000 characters) |


### What Happens After Submitting?

Once the form is submitted:

- The request is saved with a **Waiting** status, meaning it has been received but not yet scheduled.
- If the system detects a request already exists with the same phone number or email address in **Waiting** status, the existing record is updated rather than creating a duplicate.
- An administrator will review the request and follow up to schedule delivery.

---

## The Bed Request Lifecycle

Every Bed Request moves through a series of statuses as it progresses toward fulfillment:

1. **Waiting** — The request has been received and is awaiting scheduling.
2. **Scheduled** — A delivery date has been set. The family will receive an email and/or text reminder about their delivery.
3. **Delivered** — The beds have been delivered to the family's home.
4. **Given** — The beds have been picked up by the family.
5. **Cancelled** — The request has been cancelled and will not be fulfilled.

---

## Managing Bed Requests (Administrators)

Administrators with the appropriate permissions can view, create, edit, and delete Bed Requests through the administration area.

### Administrative Fields

In addition to the information submitted by the family, administrators can track:

| Field | Description |
|-------|-------------|
| Status | The current stage of the request (Waiting, Scheduled, Delivered, Given, Cancelled) |
| Delivery Date | The date beds are scheduled to be delivered |
| Team | The volunteer team assigned to the delivery |
| Group | A category or referral group associated with the request |
| Location | Which Bed Brigade location is handling this request |
| Reference | An optional external reference number |
| Contacted | Whether the family has been contacted |
| Notes | Internal notes about the request (not visible to the family) |

### Available Actions

**For administrators who can manage Bed Requests:**

- **Add** — Manually enter a new Bed Request into the system
- **Edit** — Update any field on an existing request
- **Delete** — Remove a request from the system
- **Export** — Download request data as a PDF, Excel, or CSV file
- **Print** — Print request records directly

**For users who can view Bed Requests (read-only):**

- **Search** — Find requests by any field
- **Export** — Download data as PDF, Excel, or CSV
- **Reset** — Clear search filters and return to the default view

### Delivery Planning Tools

- **Delivery Sheet** — Generates a printable sheet with delivery instructions and address information for a selected delivery date.
- **Team Sheet** — Generates an assignment sheet listing which teams are handling which deliveries.
- **Sort Waiting Closest** — Organizes Waiting requests by geographic distance to help plan efficient delivery routes.

---

## Scheduling a Delivery

When an administrator changes a request's status to **Scheduled** and sets a delivery date:

- If no delivery schedule exists for that date, the system will prompt the administrator to create one.
- The family automatically receives a **reminder email** about their upcoming delivery.
- If a phone number is on file, the family also receives a **reminder text message (SMS)**.

---

## Tips for Administrators

- **Duplicate requests** — The system automatically detects when a family submits more than one request using the same email address or phone number while their original request is still in **Waiting** status. The duplicate is merged into the existing record.
- **Language tracking** — The primary language field and "Speaks English" indicator help ensure appropriate communication with each family.
- **Notes** — Use the Notes field to record any special circumstances, follow-up actions, or important details that other team members should be aware of.
- **Filtering** — The default view shows requests in **Waiting** status. Use the Search and Reset tools to find requests in other statuses.
- **Reporting** — Exporting to Excel or CSV is useful for creating summary reports or sharing data with other systems.
