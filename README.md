==================
SETUP INSTRUCTIONS
==================

1. Configure Database Connection
----------------------------------------------
Ensure the connection string in appsettings.json matches your local SQL Server instance.


2. Apply Database Migrations
----------------------------------------
Run the following command in the project directory:


dotnet ef database update

Note: Migrations are already included in the project.


3. Configure User Secrets
------------------------------------
Run the following commands in the project directory:

NB: Replace <SET_ADMIN_PASSWORD> and <SET_HOST_PASSWORD> with passwords of your choice.


dotnet user-secrets init

dotnet user-secrets set "SuperAdmin:Password" "<SET_ADMIN_PASSWORD>"
dotnet user-secrets set "Host:Password" "<SET_HOST_PASSWORD>"


4. Run the Application
-------------------------------
dotnet run


SEEDED USER ACCOUNTS
======================

Super Admin
--------------------
Email:    super.admin@staybnb.com
Password: The password configured under SuperAdmin:Password


Host
--------------------
Email:    host@staybnb.com
Password: The password configured under Host:Password
