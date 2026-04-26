# 🛏️ Bed Requests

## ❓ What Is a Bed Request?

A Bed Request is how a family in the community asks the Bed Brigade to build and deliver beds for their children. When a family submits a request, it enters the system and is tracked from the initial intake all the way through scheduling and delivery.

---

## 📝 Submitting a Bed Request

Any member of the public can submit a Bed Request through the Bed Brigade website. The form collects the following information:

### 📇 Contact Information

| Field         | Required | Notes                     |
| ------------- | -------- | ------------------------- |
| First Name    | Yes      | Up to 20 characters       |
| Last Name     | Yes      | Up to 25 characters       |
| Email Address | Yes      | A valid email address 📧  |
| Phone Number  | Yes      | Format: (000) 000-0000 📱 |

### 📍 Delivery Address

| Field          | Required | Notes                               |
| -------------- | -------- | ----------------------------------- |
| Street Address | Yes      | Where the beds will be delivered 🏠 |
| City           | Yes      |                                     |
| State          | Yes      | Selected from a dropdown list       |
| Zip Code       | Yes      | 5 digits                            |

### 📦 Request Details

| Field                | Required    | Notes                                                     |
| -------------------- | ----------- | --------------------------------------------------------- |
| Number of Beds       | Yes         | Between 1 and 99 🛏️                                      |
| Children's Names     | Yes         | Full names of the children receiving beds 👶              |
| Gender and Age       | Yes         | Describes each child, e.g., "B/7 G/6"                     |
| Primary Language     | Yes         | The family's preferred language 🌐                        |
| Speaks English       | Conditional | Shown only if the primary language is not English         |
| Special Instructions | No          | Any special needs or requests (up to 4,000 characters) 📝 |

---

### 🔄 What Happens After Submitting?

Once the form is submitted:

* The request is saved with a **Waiting** status ⏳, meaning it has been received but not yet scheduled.
* If the system detects a request already exists with the same phone number or email address in **Waiting** status, the existing record is updated rather than creating a duplicate 🔁.
* An administrator will review the request and follow up to schedule delivery 📞.

---

## 🔁 The Bed Request Lifecycle

Every Bed Request moves through a series of statuses as it progresses toward fulfillment:

1. **Waiting** ⏳ — The request has been received and is awaiting scheduling.
2. **Scheduled** 📅 — A delivery date has been set. The family will receive reminders.
3. **Delivered** 🚚 — The beds have been delivered to the family's home.
4. **Given** 🎁 — The beds have been picked up by the family.
5. **Cancelled** ❌ — The request has been cancelled and will not be fulfilled.

---

## 🛠️ Managing Bed Requests (Administrators)

Administrators with the appropriate permissions can view, create, edit, and delete Bed Requests through the administration area.

### 🧾 Administrative Fields

| Field         | Description                                            |
| ------------- | ------------------------------------------------------ |
| Status        | The current stage of the request                       |
| Delivery Date | The scheduled delivery date 📅                         |
| Team          | The volunteer team assigned 👥                         |
| Group         | A category or referral group                           |
| Location      | Which Bed Brigade location is handling this request 📍 |
| Reference     | An optional external reference number                  |
| Contacted     | Whether the family has been contacted 📞               |
| Notes         | Internal notes (not visible to the family) 📝          |

---

### ⚙️ Available Actions

**For administrators who can manage Bed Requests:**

* ➕ **Add** — Enter a new Bed Request
* ✏️ **Edit** — Update any field
* 🗑️ **Delete** — Remove a request
* 📤 **Export** — Download as PDF, Excel, or CSV
* 🖨️ **Print** — Print request records

**For read-only users:**

* 🔍 **Search** — Find requests by any field
* 📤 **Export** — Download data
* 🔄 **Reset** — Clear filters

---

### 🚚 Delivery Planning Tools

* 📄 **Delivery Sheet** — Printable delivery instructions
* 📋 **Team Sheet** — Team assignment sheet
* 📍 **Sort Waiting Closest** — Organizes requests by distance for efficient routes

---

## 📅 Scheduling a Delivery

When an administrator changes a request's status to **Scheduled** and sets a delivery date:

* If no schedule exists, the system will prompt creation ➕
* The family automatically receives a **reminder email** 📧
* If a phone number is on file, they also receive a **text message (SMS)** 📱

---

## 💡 Tips for Administrators

* 🔁 **Duplicate requests** — Automatically merged if same contact info is used while still in **Waiting**
* 🌐 **Language tracking** — Helps ensure proper communication
* 📝 **Notes** — Record important details for team awareness
* 🔍 **Filtering** — Default shows **Waiting** requests
* 📊 **Reporting** — Export to Excel/CSV for reporting and sharing


