# WebSocketLib

A .NET library for managing **WebSocket connections** with support for **message broadcasting, direct messaging, Kafka integration, and Redis caching**.
Built with ASP.NET Core and designed for **scalable real-time communication**.

---

## ✨ Features

* Manage multiple WebSocket client connections.
* Broadcast messages to all clients with **concurrent + fault-tolerant sending**.
* Automatic cleanup of **dead or broken sockets**.
* Optional **throttling / batching** to smooth out high-throughput events.
* Send direct messages to specific clients.
* Structured message handling with JSON.
* Kafka integration for message streaming across services.
* Redis integration for connection management and caching.
* Middleware for logging and telemetry.

---

## 🏗️ Architecture

Below is how the components interact in a real-world WebSocket server:

```mermaid
flowchart LR
    subgraph Client["WebSocket Clients"]
        A1["Client 1"] -->|Send/Receive| S1
        A2["Client 2"] -->|Send/Receive| S1
        A3["Client N"] -->|Send/Receive| S1
    end

    subgraph Server["WebSocket Server"]
        S1["ConnectionManager"]
        S2["BrokeredConnectionManager"]
        S1 <--> S2
    end

    subgraph Infra["Infrastructure"]
        R["Redis (cache & connection metadata)"]
        K["Kafka (pub/sub streaming)"]
    end

    S2 -->|Publish/Subscribe| R
    S2 -->|Publish/Subscribe| K
```

* **ConnectionManager**
  Handles socket lifecycle (add/remove), direct sends, and **concurrent broadcasts with fault-tolerance**.

* **BrokeredConnectionManager**
  Subscribes to external brokers (Kafka, Redis pub/sub) and relays messages into the active connections.
  Useful when running **multiple server instances** or handling **event streams**.

* **Redis**
  Can be used for connection metadata and lightweight pub/sub.

* **Kafka**
  For scalable, high-throughput event streaming and cross-service communication.

---

## 🚀 Getting Started

### Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/)
* [Docker](https://www.docker.com/)
* [Kafka](https://kafka.apache.org/)
* [Redis](https://redis.io/)

### Clone the repository

```bash
git clone https://github.com/emmanuel-karanja/WebSocketLib.git
cd WebSocketLib
```

### Build the project

```bash
dotnet build
```

### Run locally

```bash
dotnet run --project WebSocketUtils.Demo
```

This will start the demo API with the WebSocket endpoint:

```
GET ws://localhost:5000/api/websocket/ws
```

---

## 🐳 Docker Setup

We provide a **Docker Compose** configuration to run the project along with Kafka and Redis.

### Build and Run with Docker Compose

```bash
docker-compose up --build
```

This starts:

* **WebSocketLib Demo API** on `http://localhost:5000`
* **Kafka** broker on `localhost:9092`
* **Redis** on `localhost:6379`

### Stop services

```bash
docker-compose down
```

---

## 🔌 Example Usage

### Connecting with Postman or a WebSocket client

1. Open a WebSocket connection to:

   ```bash
   ws://localhost:5000/api/websocket/ws
   ```

2. Send a broadcast message:

   ```json
   {
     "type": "broadcast",
     "message": "Hello everyone!"
   }
   ```

3. Send a direct message:

   ```json
   {
     "type": "direct",
     "target": "<client-id>",
     "message": "Hello friend!"
   }
   ```

---

## 📂 Project Structure

```
WebSocketLib/
│
├── WebSocketUtils/                # Core WebSocket utilities
│   ├── Connection/                 # Connection manager & handlers
│   ├── Middleware/                 # Logging & telemetry middleware
│   ├── WebSocketUtils.csproj
│
├── WebSocketUtils.Demo/           # Demo ASP.NET Core project
│   ├── Controllers/                # WebSocket endpoints
│   ├── Extensions/                 # Extension methods for DI/config
│   ├── Options/                    # Options & configuration bindings
│   ├── Services/                   # Demo services (Kafka, Redis)
│   ├── WebSocketUtils.Demo.csproj
│
├── WebSocketUtils.Tests/          # Unit and integration tests
│
├── docker-compose.yml             # Local environment (WebSocket + Kafka + Redis)
├── Dockerfile                     # Container build file
├── .gitignore
├── README.md                      # Project documentation
```

---

## 🧪 Testing

Run unit tests:

```bash
dotnet test
```

---

## 🤝 Contributing

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/my-feature`).
3. Commit your changes (`git commit -m 'Add new feature'`).
4. Push to the branch (`git push origin feature/my-feature`).
5. Open a Pull Request.

---

## 📜 License

MIT License. See [LICENSE](LICENSE) for details.
