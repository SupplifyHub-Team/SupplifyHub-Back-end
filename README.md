# V-1

## English 🇬🇧

### 📌 Overview
**V-1** is a private backend project built with **ASP.NET** (N-Tier Architecture).  
It provides core APIs for clients, suppliers, admins, and subscription management.  
This is **Version 1** of the project (first production-ready release).

---

### 🚀 Tech Stack

- **Backend:** ASP.NET Core  
- **Database:** PostgreSQL (Hosted on Neon)  
- **Hosting:** monasterasp.net (Backend)  
- **Media Storage:** Cloudinary  
- **Authentication:** Role-based Authentication (Client / Supplier / Admin)  
- **API Documentation:** Swagger  
- **Design Patterns:** Repository Pattern, Service Layer  
- **Email Integration:** Configured with custom SMTP (stored securely in appsettings.json)  

---

### 🛠️ Features

- 🔹 Client & Supplier registration (single endpoint based on role)  
- 🔹 Login & Token-based authentication (JWT)  
- 🔹 Order Management (Core API)  
- 🔹 Subscription Management (Core API)  
- 🔹 Admin Dashboard APIs:
  - Manage clients, suppliers, orders, subscriptions  
- 🔹 Background Service:
  - Automatically removes expired subscriptions
  - Sends email notifications
  - Resets users to free plan  
- 🔹 Media Upload via Cloudinary

---

### 🔒 Access & Privacy

- Project is **PRIVATE**:  
  Only team members working on the project have access to the repository.  
- Frontend is handled separately by another team.

---

### 🧪 Testing

- All testing is **manual testing** (no automated unit or integration tests yet).

---

### 🔮 Future Plans

- Add **JobSeekers** functionality in future versions.  
- Improve documentation and testing coverage.

---

### ⚙️ Setup Instructions

1. Clone the repository:
   ```bash
   git clone <repo-url>
   ```

2. No additional setup is needed:

   - `appsettings.json` contains:
     - Database connection strings
     - Cloudinary credentials
     - Email configuration
     - Secret tokens

3. Run the project directly.

4. Use the hosted website for interactions; cloning the repo is optional for non-developers.

---

### 📂 Project Structure (Key Points)

- `appsettings.json`: Contains all environment configurations
- **Swagger**: Used for API testing and exploration
- **No Docker configuration** is provided (not needed for this version)
