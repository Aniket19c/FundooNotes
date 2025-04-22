# Fundoo Notes Project

## Project Overview

The **Fundoo Notes** application is a web application developed using **ASP.NET Core** and **RabbitMQ**. It allows users to register, log in, and manage their notes efficiently. This project includes features like password reset via email, OTP-based user authentication, and JWT-based secure login.

The project uses a **clean architecture** pattern with separate layers for **Business** and **Repository** operations, and it integrates **NLog** for logging.

## Features

- **User Registration**: New users can register with the application.
- **Login**: Users can log in using their credentials and get a JWT token for authentication.
- **Forget Password**: Users can reset their password via email using an OTP.
- **Reset Password**: Users can reset their password after logging in with a JWT token.
- **Email Notifications**: OTPs are sent to the user's email for verification.
- **RabbitMQ Integration**: The application integrates RabbitMQ for message queuing and OTP delivery.

## Tech Stack

- **Backend**: ASP.NET Core (Web API)
- **Database**: SQL Server
- **Message Queue**: RabbitMQ
- **Logging**: NLog
- **Authentication**: JWT (JSON Web Tokens)

## Project Structure

The project is organized into the following layers:

- **Business Layer**: Contains the business logic and interacts with the Repository Layer.
- **Repository Layer**: Handles data access and interaction with the database.
- **Controllers**: Exposes endpoints for the client-side application to interact with.
- **Helper Classes**: Includes utility classes such as RabbitMQ producers/consumers and logging configurations.

### Directory Structure

```bash
Fundoo_Notes/
│
├── Business/
│   ├── Interface/
│   └── Service/
│
├── Repository/
│   ├── Context/
│   ├── DTO/
│   ├── Helper/
│   └── Interface/
│
├── Controllers/
├── appsettings.json
├── Program.cs
├── Startup.cs
└── README.md
