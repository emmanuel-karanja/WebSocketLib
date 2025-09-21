# WebSocketLib

A lightweight **WebSocket utility library** built on ASP.NET Core for managing real-time connections, broadcasting, and direct messaging between clients.

---

## ✨ Features

* 🔌 Brokered connection manager for tracking clients
* 📡 Broadcast messages to all clients
* 🎯 Send direct messages to a specific client
* 🧩 Pluggable message handling service (`IWebSocketMessageService`)
* 🛠 Built with ASP.NET Core and Dependency Injection
* 🐳 Docker-ready

---

## 📂 Project Structure

```
WebSocketLib/
│── WebSocketUtils/           # Core library (connection manager, middleware, etc.)
│── WebSocketUtils.Demo/      # Demo ASP.NET Core app (controllers, services)
│── WebSocketUtils.Tests/     # Unit tests
│── Dockerfile                # Docker image build file
│── docker-compose.yml        # Docker Compose for multi-service setups
│── .gitignore
│── README.md
```

---

## 🚀 Getting Started

### Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Docker](https://docs.docker.com/get-docker/) (optional, for containerized runs)

---

### Run Locally

```bash
# Clone repository
git clone https://github.com/emmanuel-karanja/WebSocketLib.git
cd WebSocketLib

# Build & run demo project
dotnet build
dotnet run --project WebSocketUtils.Demo
```

The WebSocket endpoint will be available at:

```
ws://localhost:5000/api/websocket/ws
```

---

### Example Message Formats

#### Broadcast

```json
{
  "type": "broadcast",
  "message": "Hello everyone!"
}
```

#### Direct Message

```json
{
  "type": "direct",
  "target": "<target-client-id>",
  "message": "Hey, just to you!"
}
```

---

## 🐳 Docker Support

### Build Image

```bash
docker build -t websocketlib-demo -f WebSocketUtils.Demo/Dockerfile .
```

### Run Container

```bash
docker run -p 5000:5000 websocketlib-demo
```

Now connect to:

```
ws://localhost:5000/api/websocket/ws
```

---

### Docker Compose

You can run multiple services together with **docker-compose**.

Example `docker-compose.yml`:

```yaml
version: '3.8'
services:
  websocket-demo:
    build:
      context: .
      dockerfile: WebSocketUtils.Demo/Dockerfile
    ports:
      - "5000:5000"
    restart: always
```

Run it:

```bash
docker-compose up --build
```

---

## 🧪 Running Tests

```bash
dotnet test
```

---

## 📜 License

MIT License © 2025 \[Emmanuel Karanja]\([https://github.com/emmanuel-kara](https://github.com/emmanuel-kara)
