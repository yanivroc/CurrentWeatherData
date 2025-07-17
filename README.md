# CurrentWeatherData

This repository contains a simple weather application with an ASP.NET Core Web API backend and a React frontend. The backend provides weather data for a given city and country, while the frontend allows users to input these details and view the results.

---

## Table of Contents

- [Prerequisites](#1-prerequisites)  
- [Getting Started](#2-getting-started)  
  - [Clone the Repository](#clone-the-repository)  
  - [Backend Setup (ASP.NET Core API)](#backend-setup-aspnet-core-api)  
  - [Frontend Setup (React App)](#frontend-setup-react-app)  
- [Running the Applications](#3-running-the-applications)  
  - [Run the Backend API](#run-the-backend-api)  
  - [Run the Frontend App](#run-the-frontend-app)  
- [Running Tests](#4-running-tests)  
  - [Run Backend Unit Tests](#run-backend-unit-tests)  
  - [Run Frontend Unit Tests](#run-frontend-unit-tests)  
- [Project Structure](#5-project-structure)  
- [CORS Configuration](#6-cors-configuration)  
- [Environment Variables](#7-environment-variables)

---

## 1. Prerequisites

Before you begin, ensure you have the following installed on your system:

- **.NET SDK 8.0 or later**: [Download .NET SDK](https://dotnet.microsoft.com/download)
- **Node.js (v18+ recommended)**: [Download Node.js](https://nodejs.org)
- **npm** (comes with Node.js)
- **Git**: [Download Git](https://git-scm.com/)
- **Code editor** (e.g., Visual Studio Code, Visual Studio)

---

## 2. Getting Started

### Clone the Repository

```bash
git clone https://github.com/yanivroc/CurrentWeatherData.git
cd CurrentWeatherData
````

### Backend Setup (ASP.NET Core API)

Navigate into the API project directory and restore dependencies:

```bash
cd CurrentWeatherData/CurrentWeatherData
dotnet restore
```

### Frontend Setup (React App)

Navigate into the frontend directory and install dependencies:

```bash
cd CurrentWeatherData/CurrentWeatherData/ClientApp
npm install
```
---

## 3. Running the Applications

Use two terminal windows to run both frontend and backend concurrently.

### Run the Backend API

```bash
cd C:\Users\rocke\Documents\GitHub\CurrentWeatherData\CurrentWeatherData
dotnet run
```

The API will start at `https://localhost:7285`.

### Run the Frontend App

```bash
cd C:\Users\rocke\Documents\GitHub\CurrentWeatherData\CurrentWeatherData\ClientApp
npm start
```

The React app will open at `http://localhost:3000`.

---

## 4. Running Tests

### Run Backend Unit Tests

```bash
cd C:\Users\rocke\Documents\GitHub\CurrentWeatherData\CurrentWeatherData\CurrentWeatherData.Tests
dotnet test
```

### Run Frontend Unit Tests

```bash
cd C:\Users\rocke\Documents\GitHub\CurrentWeatherData\CurrentWeatherData\ClientApp
npm test
```

---

## 5. Project Structure

```
CurrentWeatherData/
├── CurrentWeatherData/                  # ASP.NET Core API project
│   ├── ClientApp/                       # React frontend project
│   │   ├── public/
│   │   ├── src/
│   │   │   ├── App.js
│   │   │   ├── App.test.js
│   │   │   └── index.css
│   │   ├── .env.development
│   │   ├── package.json
│   ├── Controllers/
│   ├── Middleware/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   ├── CurrentWeatherData.csproj
└── CurrentWeatherData.Tests/            # Backend unit test project
    ├── CurrentWeatherData.Tests.csproj
```

---

## 6. CORS Configuration

CORS is configured in `Program.cs` to allow requests from the React frontend.

**Program.cs (snippet):**

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          var allowedOrigins = builder.Configuration
                            .GetValue<string>("CorsSettings:AllowedOrigins")
                            ?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                          if (allowedOrigins != null && allowedOrigins.Length > 0)
                          {
                              policy.WithOrigins(allowedOrigins)
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                          }
                      });
});

app.UseCors(MyAllowSpecificOrigins);
app.UseHttpsRedirection();
```

**appsettings.json:**

```json
{
  "CorsSettings": {
    "AllowedOrigins": "http://localhost:3000"
  }
}
```

---

## 7. Environment Variables

The frontend requires two environment variables:

```env
REACT_APP_API_HOST=https://localhost:7285
REACT_APP_API_KEY=test-api-key-123
```

* `REACT_APP_API_HOST`: The backend API URL
* `REACT_APP_API_KEY`: API key used in backend validation (`RateLimitMiddleware`)

> For production builds, place these in `.env.production`.

---
