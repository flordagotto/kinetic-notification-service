# Kinetic Notification Service
Servicio de consola para consumir eventos de RabbitMQ y registrar logs de inventario en PostgreSQL.

## Requisitos

- Docker
- .NET 8 SDK (solo si se quiere compilar local)

## Ejecución con Docker Compose

### 1. Clonar repo

git clone <url-repo-notification>
cd kinetic-notification-service
docker-compose up

Este `docker-compose.yml` espera que RabbitMQ ya esté corriendo (por ejemplo, desde el docker-compose de InventoryAPI). Se conecta a la misma instancia para consumir eventos.

### 2. Acceder a la base PostgreSQL

El servicio usa PostgreSQL para persistir logs en la base `notifications`.

Se puede acceder a la base usando DBeaver con las siguientes credenciales:

host: localhost
user: postgres
pass: postgres
db: notifications

## Descripción de arquitectura — Notification Service

### NotificationService

Proyecto principal (consola), contiene la clase `Program` y la inicialización del consumer.

Contiene la definición y configuración de `RabbitMqConsumer`, que hereda de `BackgroundService`.

Se encarga de escuchar mensajes de RabbitMQ, procesarlos y guardarlos en la base de logs.

Conoce el proyecto DAL.

Conoce el proyecto DTOs.

### DAL — Data Access Layer

Acceso a datos con Entity Framework Core.

Define la base PostgreSQL `notifications`.

Contiene entidades, repositorios y `DbContext`.

### DTOs

Define los objetos de transferencia de datos para los mensajes provenientes de RabbitMQ.

## Funcionamiento

- Consume mensajes de RabbitMQ desde tres colas:
  - `ProductCreated`
  - `ProductUpdated`
  - `ProductDeleted`

- Implementa políticas de resiliencia usando **Polly**:
  - Reintentos.
  - Circuit breaker.

- Después de **3 intentos fallidos**, envía el mensaje a las **DLQ** correspondientes (Dead Letter Queue):
  - `ProductCreated.dlq`
  - `ProductUpdated.dlq`
  - `ProductDeleted.dlq`

- Cada mensaje consumido se guarda como log en la tabla `InventoryLog` con los siguientes campos:
  - `Id`: identificador del log.
  - `ProductId`: ID del producto.
  - `Description`: descripción legible de la operación, por ejemplo:  
    > The inventory was modified - Product with id b0c03af4-c853-4d0e-962b-c496567e0ad8 has been Created. Check the Inventory Database to see the new values.
  - `EventType`: tipo de evento como enum (Created = 0, Updated = 1, Deleted = 2).
  - `EventDate`: fecha y hora del evento.

## Diagrama de arquitectura

```
                      +-------------+
                      |  RabbitMQ   |
                      +------+------+
                             |
                             v
                 +-----------+-----------+
                 | NotificationService   |
                 |  - RabbitMqConsumer   |
                 |  - Save Logs          |
                 +-----------+-----------+
                             |
                             v
                       +-----+-----+
                       |  DAL      |
                       |  - EF Core |
                       |  - DbContext  |
                       +-----+-----+
                             |
                             v
                         +---+----+
                         | PostgreSQL |
                         +--------+
```

El servicio no expone endpoints HTTP ni Swagger. Corre como proceso en segundo plano y persiste logs automáticamente.

---
