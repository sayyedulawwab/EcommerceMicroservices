# EcommerceMicroservices
Welcome to the Ecommerce Microservices repository! In this repository I am attempting to build a Ecommerce Microservice. It is a project in progress!

I have drawn out the above diagram to get the big picture of the architecture of the microservice I have been building.

![Ecommerce Microservice](https://raw.githubusercontent.com/sayyedulawwab/EcommerceMicroservices/refs/heads/main/EcommerceMicroservice.png)

## Technologies Used
- .NET 10, Entity Framework Core 10, FluentValidation, Microsoft SQL Server, Redis, RabbitMQ, MassTransit, Docker, YARP
- Architecture Patterns: Clean architecture, Domain-driven design (DDD), CQRS, Event-Driven Architecture (EDA), Microservices

## Get started
To get started, follow these steps:
- Clone this repository to your local machine.
- Install Docker Desktop for windows.

In terminal
- Navigate to the project directory in your terminal.
- Run the command docker-compose up.

In Visual Studio
- Run the Docker Compose command from toolbar.

## Using the API
- Everything goes through the gateway at `http://localhost:7000/api/{service}/v1/...` (`identity`, `catalog`, `cart`, `ordering`). In Development, `http://localhost:7000/swagger` shows every service's API in one Swagger UI.
- Sign in with `POST /api/identity/v1/auth/login`. The access and refresh tokens come back as HttpOnly cookies, so a browser frontend never handles a JWT. `GET /api/identity/v1/users/me` tells the frontend who is signed in, and `POST /api/identity/v1/auth/refresh` renews the cookies.
- A cross-site frontend must be listed in the gateway's `Cors:AllowedOrigins`, send requests with credentials, and add `Content-Type: application/json` or `X-Requested-With` to every write (CSRF protection).
- An admin account (`admin@ecommerce.local` / `Admin123!`) is created on startup from the `AdminUser` settings. Only admins can change products and categories or ship and deliver orders.
- To place an order, send only product ids and quantities. Catalog sets the prices and reserves the stock, Payment charges the order, and the order moves to `Paid` (or `Cancelled`) on its own. Poll `GET /api/ordering/v1/orders/{id}` to see its status.

## Running tests
- `dotnet test --solution EcommerceMicroservices.slnx` runs the unit and integration tests.
- The integration tests start SQL Server, RabbitMQ and Redis with Testcontainers, so Docker must be running.