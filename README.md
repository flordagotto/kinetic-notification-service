# Kinetic Notification Service
Servicio de consola para consumir eventos de RabbitMQ y registrar logs de inventario en SQLite.

## Requisitos

- Docker
- .NET 8 SDK (solo si se quiere compilar local)

## Ejecución con Docker Compose

### 1. Clonar repo

git clone <url-repo-notification>
cd kinetic-notification-service
docker-compose up --build

Este `docker-compose.yml` espera que RabbitMQ ya esté corriendo (por ejemplo, desde el docker-compose de InventoryAPI). Se conecta a la misma instancia para consumir eventos.

### 2. Acceder a la base SQLite

El servicio usa SQLite para persistir logs en `notifications.db`.

Podés copiar el archivo desde el contenedor y abrirlo localmente con herramientas como DB Browser SQLite.
Tambien podes acceder desde la consola, entrando al contenedor y copiando el archivo a local. El archivo puede demorar en actualizarse en el contenedor, se recomienda esperar unos minutos luego de procesar los mensajes.

docker ps
docker cp notificationservice:/app/notifications.db ./notifications.db

## Descripción de arquitectura — Notification Service

### NotificationService

Proyecto principal (consola), contiene la clase `Program` y la inicialización del consumer.

Contiene la definición y configuración de `RabbitMqConsumer`, que hereda de `BackgroundService`.

Se encarga de escuchar mensajes de RabbitMQ, procesarlos y guardarlos en la base de logs.

Conoce el proyecto DAL.

Conoce el proyecto DTOs.

### DAL — Data Access Layer

Acceso a datos con Entity Framework Core.

Define la base SQLite `notifications.db`.

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

- Cada mensaje consumido se guarda como log en la base `notifications.db` con los siguientes campos:
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
                         | SQLite |
                         +--------+
```

El servicio no expone endpoints HTTP ni Swagger. Corre como proceso en segundo plano y persiste logs automáticamente.

---
