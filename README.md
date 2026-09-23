# Hotel Beach Remastered 🌊

**A little escape. A lasting memory.**

A personal remaster of a hotel reservation management system, built with ASP.NET Core, C#, and SQL Server.

Hotel Beach connects customer reservations with hotel administration through an MVC web application and a REST API. This version revisits the original academic project to improve functionality, access control, account management, and the user experience.

## 🔗 Project Links

- **Live demo:** [Explore Hotel Beach — Website](https://hotelbeach.somee.com/)
- **API demo:** [Hotel Beach — API Rest](http://apihotelbeach.somee.com/swagger/index.html)
- **Original group project:** [Hotel Beach — Academic Version](https://github.com/MoniAF/HotelBeach.NET)

> This is a portfolio demonstration. Reservations and payment methods represent application workflows; the system does not process real card payments. Please use fictional information when exploring the demo.

## ✨ About the Remaster

The original application was developed collaboratively for the ASP.NET Programming course at Universidad de Costa Rica.

I revisited the project individually to troubleshoot existing issues, improve its structure and behavior, and create a more consistent experience for customers and hotel staff.

This remaster reflects my continued development in full stack web development, relational databases, API integration, and debugging existing applications.

## 🏖️ Features

### Customer Experience

- Create an account and sign in.
- Look up customer information using a Costa Rican ID number during registration.
- View personal reservations.
- Create, edit, and delete reservations.
- Select hotel packages, reservation dates, and length of stay.
- Choose cash, card, or check as the payment method.
- Register and update check details.
- Change a password by providing the current password.
- Download reservation receipts as PDF files.

### Hotel Administration

- Manage customer records.
- Manage hotel packages and their pricing conditions.
- Consult and manage reservations.
- Review and update check information.
- Reset customer passwords through a reception workflow that requires identity verification.
- Manage employee accounts with administrator access.

### Reservation Calculations

- Package price and length-of-stay calculations.
- Taxes and applicable cash-payment discounts.
- Down payments and monthly installments based on package conditions.
- USD totals and their CRC equivalent through an external exchange rate service.
- Recalculation when reservation details change.

The CRC equivalent uses the exchange rate available when requested. It is not a historical exchange rate stored with the reservation.

## 🔐 Roles and Access

| Role | Main access |
|---|---|
| **Customer** | Personal reservations, associated checks, receipt downloads, and password changes. |
| **Employee** | Customer, package, reservation, and check management, including reception password resets. |
| **Admin** | Hotel management features and employee administration. |

Access restrictions are enforced through authorization checks. Customer reservation queries and receipt downloads are limited to the authenticated customer's records.

## 🛠️ Technology Stack

| Area | Technologies |
|---|---|
| **Language and framework** | C#, .NET 8, ASP.NET Core |
| **Web application** | ASP.NET Core MVC, Razor views |
| **API** | ASP.NET Core Web API |
| **Data access** | Entity Framework Core |
| **Database** | SQL Server |
| **Authentication** | JWT for API access and cookie authentication for the MVC application |
| **Interface** | HTML, CSS, JavaScript, Bootstrap |
| **PDF generation** | iText |
| **External integrations** | Exchange rate service for USD-to-CRC conversion and Costa Rican ID lookup service for customer registration |

## 🧩 Application Structure

### AppWebBeachSA — MVC Application

Provides the user interface, form validation, session handling, and communication with the API.

### APIHotelBeach — REST API

Handles authentication, authorization, business rules, reservation calculations, PDF generation, and database operations.

### SQL Server Database

Stores customers, employees, packages, reservations, checks, and audit records.

The MVC application communicates with the API, while the API manages database access.

## 🔄 Main Improvements

Compared with the original academic version, this remaster includes:

- Improved customer and employee authentication.
- Password hashing for newly created and updated credentials.
- Role-based authorization and reservation ownership checks.
- Dedicated request models for selected operations.
- Improved validation and API error responses.
- Corrections to reservation and check registration workflows.
- Database identifier and audit-record adjustments.
- Improved handling of exchange rate service failures.
- Downloadable reservation receipts with an updated visual design.
- English interface text and clearer feedback messages.
- Form layout improvements for validation errors.
- Removal of email-sending dependencies.

Password recovery is handled through the hotel's reception workflow. Customers who know their current password can change it directly from their account.

## 📄 PDF Receipts

Customers and authorized staff can download a reservation receipt from the reservation details.

Receipts include:

- Customer and reservation information.
- Payment method.
- Subtotal, tax, discount, and total.
- Equivalent total in CRC.
- Down payment and monthly installment amounts.
- Check number and bank name when applicable.

For check reservations, check details must be registered before the receipt can be downloaded.

## 🚀 Running Locally

### Requirements

- .NET 8 SDK.
- SQL Server Express or another compatible SQL Server edition.
- SQL Server Management Studio (SSMS) to run the database setup script.
- Visual Studio 2022 with the **ASP.NET and web development** workload and .NET 8 SDK installed.
- Git to clone the repository.
- Internet access for restoring NuGet packages and using external services.

### Setup

1. Clone the repository:

   ```bash
   git clone https://github.com/MoniAF/HotelBeachRemastered.git
   
2. **Install SQL Server Express and SSMS.**

   Install SQL Server Express if you do not already have a compatible SQL Server instance. Install SQL Server Management Studio (SSMS) to manage the database and execute the setup script.

3. **Connect using Windows Authentication.**

   Open SSMS, enter your SQL Server instance name, and select **Windows Authentication**. For a default Express installation, the instance is commonly `.\SQLEXPRESS`.

   Connect using a Windows account with permission to create databases.

4. **Create the database.**

   Open the project's SQL setup script in SSMS and execute it. The script creates `HotelBeachDB`, its tables, relationships, sequences, audit structures, and sample records.

   **Important:** Running this script again resets the application tables and deletes their existing data.

5. **Open the API and web application.**

   Open the `APIHotelBeach` and `AppWebBeachSA` projects in Visual Studio 2022.

   In `APIHotelBeach/appsettings.json`, check that `ConnectionStrings:StringConexion` matches the SQL Server instance used in step 3. For example:

   ```json
   "ConnectionStrings": {
     "StringConexion": "Server=.\\SQLEXPRESS; Database=HotelBeachDB; Trusted_Connection=True; TrustServerCertificate=True;"
   }
   ```

   This configuration uses Windows Authentication for local development. The Windows account running the API must have access to the database.

6. **Restore NuGet packages and build.**

   Allow Visual Studio to restore the required NuGet packages. If necessary, right-click the solution in Solution Explorer and select **Restore NuGet Packages**.

   Build both projects and resolve any build errors before continuing.

7. **Verify the API address.**

   Check the API's local URL in `APIHotelBeach/Properties/launchSettings.json`, using the `applicationUrl` value for the launch profile you will run.

   Open `AppWebBeachSA/Models/HotelAPI.cs` and update `client.BaseAddress` inside `Initial()` to match that URL. For example:

   ```csharp
   client.BaseAddress = new Uri("https://localhost:7094/");
   ```

   Use the API's base URL, including the correct protocol and port, without `/swagger` or an endpoint path.

8. **Run both applications.**

   Start `APIHotelBeach` first, then start `AppWebBeachSA`, keeping both running. You can use separate Visual Studio windows or configure both as startup projects if they are included in the same solution.

   If Visual Studio prompts you to trust the local ASP.NET Core HTTPS development certificate, accept it.

   Open the web application's URL in your browser. Create a customer account to explore customer features, or use **Staff sign in** with the local demo credentials defined in the SQL script.

Keep real credentials, signing keys, and personal data outside the repository.

## 🔑 Demo Access

Explore the staff features through **Staff sign in** using these demonstration accounts:

| Role | Email | Password |
|---|---|---|
| Administrator | sahotelbeach@gmail.com | remaster1234 |
| Employee | lgarcia@gmail.com | remaster1234 |

To explore the customer experience, select **Create an account**, register, and sign in through the customer login page.

This is a shared, interactive demo. Changes are visible to other visitors, and demo accounts may be modified during testing. Please do not enter sensitive information or reuse personal passwords.

## ✅ Manual Validation

The following workflows were manually tested in the local development environment:

- Customer registration and sign-in.
- Invalid credentials and password confirmation validation.
- Customer password changes.
- Reception-assisted password resets.
- Reservation creation, editing, and deletion.
- Cash, card, and check reservation workflows.
- Check registration and editing.
- PDF receipt downloads.
- Access restrictions between customers and staff.
- Employee creation, editing, activation, deactivation, and deletion.
- Package creation, validation, editing, and deletion restrictions.

These checks cover the tested application workflows and do not constitute a comprehensive security or production-readiness assessment.

## 👩‍💻 Authorship and Attribution

**Remaster developed by Mónica Artavia Flores.**

The original application was a university group project. Its original implementation remains a shared contribution of the project team.

The improvements described in this repository were implemented and manually tested as part of my individual remaster.

AI tools assisted with code review, troubleshooting, and implementation suggestions during the remastering process.

## 🌱 Project Purpose

Hotel Beach Remastered is part of my portfolio and documents my progress in maintaining and improving an existing full stack application.

It demonstrates work with ASP.NET Core, REST APIs, SQL Server, authorization, business rules, and user-facing workflows.