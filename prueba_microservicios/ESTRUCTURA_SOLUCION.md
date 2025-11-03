# Estructura de la Solución - Sistema de Inventario

## 1. Stack Tecnológico

### Tecnologías Principales
- **.NET 8.0** - Framework principal
- **ASP.NET Core Web API** - Para ambos microservicios
- **Entity Framework Core** - ORM para acceso a datos
- **PostgreSQL** - Base de datos para ambos microservicios (una por servicio)
- **Redis** - Caché distribuido
- **RabbitMQ** - Cola de mensajería para eventos (más ligero que Kafka para esta solución)
- **JWT** - Autenticación y autorización
- **Docker & Docker Compose** - Containerización y orquestación
- **xUnit** - Framework de pruebas unitarias
- **AutoMapper** - Mapeo de entidades

### NuGet Packages Clave
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `StackExchange.Redis`
- `RabbitMQ.Client`
- `MassTransit.RabbitMQ` (opcional, simplifica RabbitMQ)
- `FluentValidation.AspNetCore`
- `Swashbuckle.AspNetCore` (Swagger)
- `Serilog.AspNetCore` (Logging)

## 2. Estructura del Proyecto

```
prueba_microservicios/
│
├── src/
│   ├── ProductService/                    # Microservicio de Productos
│   │   ├── ProductService.API/           # Capa de presentación (Controllers, Startup)
│   │   ├── ProductService.Application/   # Lógica de negocio, DTOs, Validaciones
│   │   ├── ProductService.Domain/        # Entidades, Interfaces, Eventos
│   │   ├── ProductService.Infrastructure/ # EF Core, Repositorios, RabbitMQ Publisher
│   │   └── ProductService.Tests/         # Pruebas unitarias
│   │
│   └── InventoryService/                 # Microservicio de Inventario
│       ├── InventoryService.API/         # Capa de presentación
│       ├── InventoryService.Application/ # Lógica de negocio
│       ├── InventoryService.Domain/     # Entidades, Interfaces, Eventos
│       ├── InventoryService.Infrastructure/ # EF Core, Repositorios, RabbitMQ Consumer
│       └── InventoryService.Tests/       # Pruebas unitarias
│
├── docker-compose.yml                    # Orquestación de toda la infraestructura
├── docker-compose.override.yml          # Configuración de desarrollo
├── .env                                  # Variables de entorno
├── .gitignore
├── README.md                             # Documentación principal
├── ARCHITECTURE.md                       # Diagramas y arquitectura detallada
└── docs/                                 # Documentación adicional
    ├── API.md                            # Endpoints y ejemplos
    └── DEPLOYMENT.md                     # Guía de despliegue
```

## 3. Estructura Detallada de Cada Microservicio

### 3.1. ProductService

#### API Layer (ProductService.API)
```
ProductService.API/
├── Controllers/
│   ├── ProductsController.cs            # CRUD de productos
│   ├── AuthController.cs                # Login/Register
│   └── PriceHistoryController.cs        # Historial de precios
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs       # Manejo global de errores
│   └── LoggingMiddleware.cs             # Logging de requests
├── Program.cs                           # Configuración y Startup
├── appsettings.json
└── appsettings.Development.json
```

#### Application Layer (ProductService.Application)
```
ProductService.Application/
├── DTOs/
│   ├── ProductDto.cs
│   ├── CreateProductDto.cs
│   ├── UpdateProductDto.cs
│   └── ProductPriceHistoryDto.cs
├── Mappings/
│   └── ProductMappingProfile.cs        # AutoMapper
├── Validators/
│   ├── CreateProductValidator.cs       # FluentValidation
│   └── UpdateProductValidator.cs
├── Services/
│   ├── IProductService.cs
│   ├── ProductService.cs
│   ├── IPriceHistoryService.cs
│   ├── PriceHistoryService.cs
│   ├── ICurrencyConverterService.cs
│   └── CurrencyConverterService.cs     # Integración con exchangerate-api.com
├── Events/
│   ├── ProductCreatedEvent.cs
│   ├── ProductUpdatedEvent.cs
│   └── ProductDeletedEvent.cs
└── Interfaces/
    └── IEventPublisher.cs               # Interface para publicar eventos
```

#### Domain Layer (ProductService.Domain)
```
ProductService.Domain/
├── Entities/
│   ├── Product.cs
│   ├── PriceHistory.cs
│   └── User.cs                          # Usuarios y roles
├── Interfaces/
│   └── IProductRepository.cs
└── ValueObjects/
    └── Currency.cs
```

#### Infrastructure Layer (ProductService.Infrastructure)
```
ProductService.Infrastructure/
├── Data/
│   ├── ProductDbContext.cs             # DbContext de EF Core
│   └── Repositories/
│       └── ProductRepository.cs
├── Messaging/
│   ├── RabbitMQEventPublisher.cs       # Publicador de eventos
│   └── EventBusConfiguration.cs
├── Caching/
│   └── RedisCacheService.cs            # Servicio de caché
├── ExternalServices/
│   └── ExchangeRateApiClient.cs       # Cliente HTTP para exchangerate-api.com
└── Migrations/                          # Migraciones de EF Core
```

### 3.2. InventoryService

#### API Layer (InventoryService.API)
```
InventoryService.API/
├── Controllers/
│   ├── InventoryController.cs          # Gestión de inventario
│   ├── StockController.cs              # Consultas de stock
│   └── MovementHistoryController.cs   # Historial de movimientos
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs
│   └── LoggingMiddleware.cs
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

#### Application Layer (InventoryService.Application)
```
InventoryService.Application/
├── DTOs/
│   ├── InventoryDto.cs
│   ├── AdjustInventoryDto.cs
│   ├── StockDto.cs
│   └── InventoryMovementDto.cs
├── Mappings/
│   └── InventoryMappingProfile.cs
├── Validators/
│   └── AdjustInventoryValidator.cs
├── Services/
│   ├── IInventoryService.cs
│   ├── InventoryService.cs
│   ├── IStockService.cs
│   └── StockService.cs
├── Events/
│   ├── InventoryAdjustedEvent.cs
│   └── Handlers/
│       ├── ProductCreatedEventHandler.cs    # Consumidor de eventos de ProductService
│       ├── ProductUpdatedEventHandler.cs
│       └── ProductDeletedEventHandler.cs
└── Interfaces/
    └── IEventConsumer.cs
```

#### Domain Layer (InventoryService.Domain)
```
InventoryService.Domain/
├── Entities/
│   ├── Inventory.cs
│   ├── InventoryMovement.cs
│   └── ProductReference.cs             # Referencia al producto (solo ID y datos básicos)
└── Interfaces/
    └── IInventoryRepository.cs
```

#### Infrastructure Layer (InventoryService.Infrastructure)
```
InventoryService.Infrastructure/
├── Data/
│   ├── InventoryDbContext.cs
│   └── Repositories/
│       └── InventoryRepository.cs
├── Messaging/
│   ├── RabbitMQEventConsumer.cs        # Consumidor de eventos
│   └── EventBusConfiguration.cs
├── Caching/
│   └── RedisCacheService.cs
└── Migrations/
```

## 4. Arquitectura de Eventos

### 4.1. Eventos del ProductService
```
ProductCreatedEvent:
  - ProductId
  - Name
  - SKU
  - Category
  - Timestamp
  - EventId (para idempotencia)

ProductUpdatedEvent:
  - ProductId
  - Changes (nombre de campos modificados)
  - Timestamp
  - EventId

ProductDeletedEvent:
  - ProductId
  - Timestamp
  - EventId
```

### 4.2. Eventos del InventoryService
```
InventoryAdjustedEvent:
  - ProductId
  - QuantityChange
  - MovementType (In/Out)
  - Timestamp
  - EventId
```

### 4.3. Flujo de Sincronización
1. **ProductService crea producto** → Publica `ProductCreatedEvent` → **InventoryService** lo consume y crea registro de inventario
2. **ProductService actualiza producto** → Publica `ProductUpdatedEvent` → **InventoryService** actualiza referencias si es necesario
3. **ProductService elimina producto** → Publica `ProductDeletedEvent` → **InventoryService** marca inventario como inactivo (soft delete recomendado)

### 4.4. Idempotencia
- Cada evento incluye un `EventId` único
- **InventoryService** mantiene una tabla `ProcessedEvents` con `EventId` para evitar procesar eventos duplicados
- Verificación antes de procesar cualquier evento

## 5. Bases de Datos

### 5.1. ProductService Database
```sql
-- Tablas principales
Products (Id, Name, Description, Price, Category, SKU, CreatedAt, UpdatedAt)
PriceHistory (Id, ProductId, Price, Currency, Date, CreatedAt)
Users (Id, Username, Email, PasswordHash, Role, CreatedAt)
```

### 5.2. InventoryService Database
```sql
-- Tablas principales
Inventory (Id, ProductId, ProductName, ProductSKU, CurrentStock, LastUpdated)
InventoryMovements (Id, InventoryId, ProductId, QuantityChange, MovementType, Reason, Timestamp)
ProcessedEvents (Id, EventId, EventType, ProcessedAt) -- Para idempotencia
```

## 6. Autenticación y Autorización

### 6.1. JWT Configuration
- **Issuer**: Cada microservicio puede tener su propio token o usar uno compartido
- **Roles**: `Admin`, `User`
- **Endpoints protegidos**:
  - **Admin**: POST, PUT, DELETE (Productos e Inventario)
  - **User**: GET (solo lectura)

### 6.2. Usuarios por defecto (desarrollo)
- **Admin**: admin@test.com / password123
- **User**: user@test.com / password123

## 7. Caché (Redis)

### 7.1. Keys y TTL
```
product:list -> Lista de productos (TTL: 5 minutos)
product:category:{category} -> Productos por categoría (TTL: 5 minutos)
product:{id} -> Producto individual (TTL: 10 minutos)
stock:{productId} -> Stock disponible (TTL: 2 minutos)
exchange_rate:{from}:{to} -> Tasas de conversión (TTL: 1 hora)
```

## 8. Docker Compose - Infraestructura

```yaml
Services:
  1. product-service (ASP.NET Core)
  2. inventory-service (ASP.NET Core)
  3. postgres-products (Base de datos ProductService)
  4. postgres-inventory (Base de datos InventoryService)
  5. redis (Caché)
  6. rabbitmq (Message Broker)
```

## 9. Endpoints API

### 9.1. ProductService
```
POST   /api/auth/login
POST   /api/auth/register

GET    /api/products                    # Con parámetro opcional ?currency=USD
GET    /api/products?category={category}
GET    /api/products/{id}               # Con parámetro opcional ?currency=USD
POST   /api/products                    # [Admin]
PUT    /api/products/{id}               # [Admin]
DELETE /api/products/{id}               # [Admin]

GET    /api/products/{id}/price-history
```

### 9.2. InventoryService
```
GET    /api/inventory/{productId}/stock
GET    /api/inventory/{productId}/movements
POST   /api/inventory/adjust             # [Admin] - Aumentar/reducir stock
```

## 10. Plan de Implementación

### Fase 1: Setup Base
1. Crear estructura de carpetas
2. Configurar proyectos .NET
3. Configurar Docker Compose básico
4. Configurar bases de datos (EF Core migrations)

### Fase 2: ProductService
1. Implementar entidades y DbContext
2. Implementar repositorios
3. Implementar servicios y DTOs
4. Implementar controllers
5. Implementar JWT authentication
6. Implementar caché Redis
7. Implementar conversión de moneda
8. Implementar historial de precios

### Fase 3: InventoryService
1. Implementar entidades y DbContext
2. Implementar repositorios
3. Implementar servicios y DTOs
4. Implementar controllers
5. Implementar caché Redis

### Fase 4: Eventos y Mensajería
1. Configurar RabbitMQ en ambos servicios
2. Implementar publisher en ProductService
3. Implementar consumer en InventoryService
4. Implementar idempotencia
5. Probar flujo completo de eventos

### Fase 5: Testing
1. Crear pruebas unitarias para ambos servicios
2. Verificar cobertura de código
3. Probar flujos end-to-end

### Fase 6: Documentación
1. Completar README.md
2. Crear diagramas de arquitectura (Mermaid)
3. Documentar APIs con Swagger
4. Crear guía de deployment

## 11. Consideraciones de Diseño

### 11.1. Patrones Aplicados
- **Repository Pattern**: Abstracción de acceso a datos
- **CQRS Ligero**: Separación entre comandos y consultas
- **Event-Driven Architecture**: Comunicación desacoplada
- **Dependency Injection**: Desacoplamiento de dependencias
- **Clean Architecture**: Separación por capas

### 11.2. Manejo de Errores
- Middleware global de manejo de errores
- Respuestas HTTP estándar
- Logging estructurado con Serilog

### 11.3. Validaciones
- FluentValidation en DTOs
- Validaciones en capa de dominio

### 11.4. Resiliencia
- Reintentos en conexión a RabbitMQ
- Fallback en caché (si Redis falla, consultar BD)
- Manejo de eventos fallidos (dead letter queue)

## 12. Variables de Entorno

```env
# ProductService
PRODUCT_SERVICE_CONNECTION_STRING=Host=postgres-products;Database=products;Username=postgres;Password=postgres
REDIS_CONNECTION_STRING=redis:6379
RABBITMQ_CONNECTION_STRING=amqp://guest:guest@rabbitmq:5672/
JWT_SECRET_KEY=tu_clave_secreta_super_segura_aqui
JWT_ISSUER=ProductService
EXCHANGE_RATE_API_KEY=tu_api_key_aqui

# InventoryService
INVENTORY_SERVICE_CONNECTION_STRING=Host=postgres-inventory;Database=inventory;Username=postgres;Password=postgres
REDIS_CONNECTION_STRING=redis:6379
RABBITMQ_CONNECTION_STRING=amqp://guest:guest@rabbitmq:5672/
JWT_SECRET_KEY=tu_clave_secreta_super_segura_aqui (misma que ProductService)
JWT_ISSUER=InventoryService
```

## Próximos Pasos

1. Revisar y aprobar esta estructura
2. Comenzar con la implementación siguiendo el plan de fases
3. Implementar Feature por Feature con pruebas

