# 💝 Donations

## ❓ What Is a Donation?

A Donation is a financial contribution 💵 made to a Bed Brigade location through the website. Donations help fund the building 🛠️ and delivery 🚚 of beds for children 👶 in the community.

---

## 🌐 Making a Donation (Public Users)

Anyone can donate through the Bed Brigade website. Each location has its own donation page.

### 🔢 Step-by-Step

1. 📍 Navigate to your local Bed Brigade location's **Donations** page
2. 👤 Fill in your contact information
3. 💲 Choose a donation amount
4. 🎯 Select a campaign
5. 🤖 Complete the CAPTCHA check
6. ➡️ Click **Continue**
7. 🔐 You will be redirected to a secure payment page to complete your payment

---

### 📇 Contact Information Required

| Field         | Required | Notes                     |
| ------------- | -------- | ------------------------- |
| First Name    | Yes      | Up to 50 characters       |
| Last Name     | Yes      | Up to 50 characters       |
| Email Address | Yes      | A valid email address 📧  |
| Phone Number  | Yes      | Format: (000) 000-0000 📱 |

---

### 💰 Choosing a Donation Amount

The page presents preset amounts to choose from. You can select:

* 💵 **One-time donation** — A single payment
* 🔁 **Monthly donation** — A recurring monthly payment

Select one option. You cannot select both at the same time.

---

### 🎯 Selecting a Campaign

Each donation is associated with a **campaign**. A campaign represents a specific fundraising effort for a location.

* If only one campaign is available, it will be pre-selected ✔️
* If multiple campaigns are available, choose from the dropdown ⬇️

---

### 🔐 Secure Payment

After clicking **Continue**, you will be redirected to a secure payment page powered by Stripe. Your payment information is entered and processed there — not stored on the Bed Brigade website 🔒.

After your payment is complete, you will be returned to a **Donation Confirmation** page that shows:

* 💲 The amount donated
* 📅 The date of the donation
* 🧾 A transaction ID for your records

If you cancel:

* ❌ You will see a **Payment Cancelled** message
* 💳 No charge will be made

---

## 🧾 Tax Forms

After donating, you may receive a tax acknowledgment email 📧 from the Bed Brigade location. This can be used for tax purposes.

---

## 🛠️ Managing Donations (Administrators)

Administrators with the appropriate permissions can view and manage donations in the administration area under **Manage Donations**.

---

### 👀 What Administrators Can See

| Column                 | Description                        |
| ---------------------- | ---------------------------------- |
| Location               | Associated Bed Brigade location 📍 |
| First Name / Last Name | Donor's name 👤                    |
| Email                  | Donor's email 📧                   |
| Tax Form Sent          | Whether acknowledgment was sent 🧾 |
| Donation Date          | Date donation was made 📅          |
| Net Amount             | Amount after fees 💰               |
| Donation Campaign      | Associated campaign 🎯             |

The page also displays a **Filtered Total** 📊 showing the sum of visible donations.

---

### ⚙️ Available Actions

**For administrators who can manage donations:**

* ➕ **Add** — Record a donation manually
* ✏️ **Edit** — Update donation details
* 🗑️ **Delete** — Remove a donation
* 📧 **Send Tax Form** — Email acknowledgment
* 📤 **Export** — Download as PDF, Excel, or CSV
* 🖨️ **Print** — Print the list

**For view-only administrators:**

* 📤 **Export** — Download data
* 🖨️ **Print** — Print list
* 🔍 **Search** — Find donations
* 🔄 **Reset** — Clear filters

---

### ✏️ Adding or Editing a Donation Record

| Field                  | Description                           |
| ---------------------- | ------------------------------------- |
| First Name / Last Name | Donor's name 👤                       |
| Email                  | Donor's email 📧                      |
| Campaign               | Fundraising campaign 🎯               |
| Donation Date          | Date of donation 📅                   |
| Gross Amount           | Total before fees 💵                  |
| Transaction Fees       | Processing fee 💳                     |
| Net Amount             | Gross minus fees (auto-calculated) 🧮 |
| Transaction ID         | Payment reference 🧾                  |
| Payment Status         | Status (e.g., paid) ✔️                |
| Payment Processor      | Service used (e.g., Stripe) 🔐        |
| Currency               | Donation currency 💱                  |
| Tax Form Sent          | Whether email was sent 📧             |

---

### 📧 Sending Tax Forms

Administrators can send tax acknowledgment emails:

* 📋 **Not Sent** — Donors who have not received a form
* 📨 **Send** — Selected donors

Move donors ➡️ then click **Send Tax Form**.

After sending:

* ✔️ The **Tax Form Sent** indicator updates

---

## 🎯 Managing Donation Campaigns (Administrators)

Campaigns must be set up before donations can be made.

---

### ❓ What Is a Campaign?

A campaign represents a specific fundraising effort tied to a location.

| Field         | Description             |
| ------------- | ----------------------- |
| Location      | Associated location 📍  |
| Campaign Name | Display name 🎯         |
| Start Date    | When campaign begins 📅 |
| End Date      | Optional end date ⏳     |

---

### ⚙️ Available Actions

* ➕ **Add** — Create a campaign
* ✏️ **Edit** — Update details
* 🗑️ **Delete** — Remove a campaign

---

## 💡 Tips for Administrators

* 📊 **Filtered Total** — Use filters for quick reporting
* 💵 **Manual donations** — Record checks/cash with **Add**
* 📧 **Tax forms** — Monitor the **Tax Form Sent** column
* ⚠️ **Campaign setup** — Ensure at least one active campaign exists per location

