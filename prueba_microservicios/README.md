# Sistema de Inventario - Microservicios

Sistema de gestión de inventario implementado con arquitectura de microservicios usando .NET 8, ASP.NET Core, PostgreSQL, Redis y RabbitMQ.

## 📋 Tabla de Contenidos

- [Componentes del Sistema](#componentes-del-sistema)
- [Stack Tecnológico](#stack-tecnológico)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Requisitos Previos](#requisitos-previos)
- [Ejecución de Tests](#ejecución-de-tests)
- [Ejecución con Docker](#ejecución-con-docker)
- [Cómo Probar el Proyecto](#cómo-probar-el-proyecto)
- [Endpoints de la API](#endpoints-de-la-api)
- [Desarrollo Local](#desarrollo-local)
- [Configuración](#configuración)

---

## 🏗️ Componentes del Sistema

El sistema está compuesto por **tres microservicios independientes** y servicios de infraestructura:

### Microservicios

#### 1. **AuthService** (Puerto 5002)
Microservicio de autenticación y autorización:
- **Funcionalidad**: Gestión de usuarios, login, registro y generación de tokens JWT
- **Base de Datos**: PostgreSQL (`postgres-auth` en puerto 5434)
- **Endpoints**:
  - `POST /api/auth/login` - Iniciar sesión
  - `POST /api/auth/register` - Registrar nuevo usuario
- **Características**:
  - Hash de contraseñas con BCrypt
  - Generación de tokens JWT compartidos
  - Roles: Admin y User
  - Validación con FluentValidation

#### 2. **ProductService** (Puerto 5000)
Microservicio de gestión de productos:
- **Funcionalidad**: CRUD completo de productos, historial de precios, conversión de moneda
- **Base de Datos**: PostgreSQL (`postgres-products` en puerto 5432)
- **Características**:
  - Gestión de productos con categorías
  - Historial de cambios de precio
  - Conversión de moneda (USD a otras monedas)
  - Publicación de eventos a RabbitMQ (ProductCreated, ProductUpdated, ProductDeleted)
  - Caché de productos en Redis

#### 3. **InventoryService** (Puerto 5001)
Microservicio de gestión de inventario:
- **Funcionalidad**: Gestión de stock, movimientos de inventario, sincronización con productos
- **Base de Datos**: PostgreSQL (`postgres-inventory` en puerto 5433)
- **Características**:
  - Gestión de stock por producto
  - Movimientos de inventario (entradas y salidas)
  - Sincronización automática mediante eventos de ProductService
  - Idempotencia de eventos procesados
  - Caché de stock en Redis

### Servicios de Infraestructura

- **PostgreSQL** (3 instancias):
  - `postgres-products` (Puerto 5432) - Base de datos de ProductService
  - `postgres-inventory` (Puerto 5433) - Base de datos de InventoryService
  - `postgres-auth` (Puerto 5434) - Base de datos de AuthService

- **Redis** (Puerto 6379):
  - Caché distribuido para productos, stock y tasas de conversión

- **RabbitMQ**:
  - Puerto 5672 (AMQP) - Message broker
  - Puerto 15672 (Management UI) - Interfaz web de administración

---

## 🛠️ Stack Tecnológico

- **.NET 8.0** - Framework principal
- **ASP.NET Core Web API** - API REST
- **Entity Framework Core 8.0** - ORM para acceso a datos
- **PostgreSQL 16** - Base de datos relacional
- **Redis 7** - Caché distribuido
- **RabbitMQ 3** - Message broker para eventos
- **JWT** - Autenticación y autorización
- **Serilog** - Logging estructurado
- **Swagger/OpenAPI** - Documentación de API
- **AutoMapper** - Mapeo de objetos
- **FluentValidation** - Validación de entrada
- **BCrypt** - Hash de contraseñas
- **xUnit** - Framework de testing
- **Moq** - Framework de mocking para tests
- **FluentAssertions** - Assertions expresivas

---

## 📁 Estructura del Proyecto

```
prueba_microservicios/
├── src/
│   ├── AuthService/
│   │   ├── AuthService.API/          # Capa de presentación (Controllers, Program.cs)
│   │   ├── AuthService.Application/   # Lógica de negocio (Services, DTOs, Validators)
│   │   ├── AuthService.Domain/       # Entidades y contratos (Entities, Interfaces)
│   │   ├── AuthService.Infrastructure/ # Implementaciones técnicas (DbContext, Repositories)
│   │   └── AuthService.Tests/        # Tests unitarios
│   │
│   ├── ProductService/
│   │   ├── ProductService.API/
│   │   ├── ProductService.Application/
│   │   ├── ProductService.Domain/
│   │   ├── ProductService.Infrastructure/
│   │   └── ProductService.Tests/
│   │
│   └── InventoryService/
│       ├── InventoryService.API/
│       ├── InventoryService.Application/
│       ├── InventoryService.Domain/
│       ├── InventoryService.Infrastructure/
│       └── InventoryService.Tests/
│
├── docker-compose.yml                # Configuración de Docker Compose
└── README.md                          # Este archivo
```

---

## 📋 Requisitos Previos

- **Docker Desktop** instalado y ejecutándose
- **.NET 8 SDK** (opcional, solo para desarrollo local y tests)
- **Git** para clonar el repositorio

---

## 🧪 Ejecución de Tests

### Ejecutar todos los tests

Desde la raíz del proyecto:

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar con más detalle
dotnet test --verbosity normal
```

### Ejecutar tests de un servicio específico

```bash
# Tests de AuthService
cd src/AuthService
dotnet test

# Tests de ProductService
cd src/ProductService
dotnet test

# Tests de InventoryService
cd src/InventoryService
dotnet test
```

### Resultado esperado

```
✅ AuthService.Tests: 9 tests pasando
✅ ProductService.Tests: 9 tests pasando
✅ InventoryService.Tests: 9 tests pasando

Total: 27 tests - Todos pasando ✓
```

---

## 🐳 Ejecución con Docker

### 1. Levantar toda la infraestructura

```bash
# Levantar todos los servicios
docker-compose up -d

# Ver logs de todos los servicios
docker-compose logs -f

# Ver logs de un servicio específico
docker-compose logs -f auth-service
docker-compose logs -f product-service
docker-compose logs -f inventory-service
```

### 2. Verificar que los servicios están corriendo

```bash
# Ver estado de todos los contenedores
docker-compose ps

# Deberías ver algo como:
# NAME                    STATUS          PORTS
# auth-service            Up 2 minutes    0.0.0.0:5002->80/tcp
# product-service         Up 2 minutes    0.0.0.0:5000->80/tcp
# inventory-service       Up 2 minutes    0.0.0.0:5001->80/tcp
# postgres-auth           Up 2 minutes    0.0.0.0:5434->5432/tcp
# postgres-products       Up 2 minutes    0.0.0.0:5432->5432/tcp
# postgres-inventory      Up 2 minutes    0.0.0.0:5433->5432/tcp
# redis-cache             Up 2 minutes    0.0.0.0:6379->6379/tcp
# rabbitmq                Up 2 minutes    0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
```

### 3. Detener los servicios

```bash
# Detener todos los servicios
docker-compose down

# Detener y eliminar volúmenes (borra las bases de datos)
docker-compose down -v
```

### 4. Acceder a las interfaces web

- **AuthService Swagger**: http://localhost:5002/swagger
- **ProductService Swagger**: http://localhost:5000/swagger
- **InventoryService Swagger**: http://localhost:5001/swagger
- **RabbitMQ Management**: http://localhost:15672 (usuario: `guest`, contraseña: `guest`)

---

## 🧪 Cómo Probar el Proyecto

### Paso 1: Obtener un Token de Autenticación

Primero, necesitas autenticarte para obtener un token JWT.

#### Opción A: Usar el usuario Admin predefinido

```bash
curl -X POST http://localhost:5002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@test.com",
    "password": "Admin123!"
  }'
```

**Respuesta esperada:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "admin@test.com",
  "username": "admin",
  "role": "Admin",
  "expiresAt": "2025-11-03T18:30:00Z"
}
```

#### Opción B: Registrar un nuevo usuario

```bash
curl -X POST http://localhost:5002/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "testuser@example.com",
    "password": "Test123!",
    "role": "User"
  }'
```

**Respuesta esperada:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "testuser@example.com",
  "username": "testuser",
  "role": "User",
  "expiresAt": "2025-11-03T18:30:00Z"
}
```

**Guarda el token** de la respuesta para usarlo en los siguientes pasos.

---

### Paso 2: Crear un Producto

Usa el token obtenido en el paso anterior (reemplaza `YOUR_TOKEN_HERE`):

```bash
TOKEN="YOUR_TOKEN_HERE"

curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "name": "Laptop Dell XPS 15",
    "description": "Laptop profesional con procesador Intel i7 y 16GB RAM",
    "price": 1299.99,
    "category": "Electronics",
    "sku": "DL-XPS15-001"
  }'
```

**Respuesta esperada:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Laptop Dell XPS 15",
  "description": "Laptop profesional con procesador Intel i7 y 16GB RAM",
  "price": 1299.99,
  "category": "Electronics",
  "sku": "DL-XPS15-001",
  "createdAt": "2025-11-02T18:00:00Z",
  "updatedAt": "2025-11-02T18:00:00Z"
}
```

**Nota importante**: Al crear un producto, el `InventoryService` automáticamente crea un registro de inventario para ese producto con stock inicial de 0.

**Guarda el `id` del producto** para los siguientes pasos.

---

### Paso 3: Verificar Sincronización Automática

El `InventoryService` consume eventos de `ProductService`. Verifica que el inventario se creó automáticamente:

```bash
PRODUCT_ID="3fa85f64-5717-4562-b3fc-2c963f66afa6"  # Reemplaza con el ID del producto creado

curl -X GET http://localhost:5001/api/inventory/$PRODUCT_ID/stock
```

**Respuesta esperada:**
```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "productName": "Laptop Dell XPS 15",
  "productSKU": "DL-XPS15-001",
  "currentStock": 0
}
```

✅ **Resultado**: El inventario se creó automáticamente cuando se creó el producto.

---

### Paso 4: Ajustar el Inventario (Aumentar Stock)

```bash
curl -X POST http://localhost:5001/api/inventory/adjust \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "productId": "'$PRODUCT_ID'",
    "quantityChange": 50,
    "movementType": "In",
    "reason": "Reabastecimiento inicial"
  }'
```

**Respuesta esperada:**
```json
{
  "id": "5fa85f64-5717-4562-b3fc-2c963f66afa7",
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "productName": "Laptop Dell XPS 15",
  "productSKU": "DL-XPS15-001",
  "currentStock": 50,
  "lastUpdated": "2025-11-02T18:05:00Z"
}
```

✅ **Resultado**: El stock aumentó de 0 a 50.

---

### Paso 5: Obtener Productos (con conversión de moneda)

```bash
# Obtener todos los productos
curl -X GET http://localhost:5000/api/products

# Obtener productos en EUR
curl -X GET "http://localhost:5000/api/products?currency=EUR"

# Obtener un producto específico en GBP
curl -X GET "http://localhost:5000/api/products/$PRODUCT_ID?currency=GBP"
```

**Respuesta esperada (con EUR):**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Laptop Dell XPS 15",
    "price": 1169.99,  // Convertido de USD a EUR
    "category": "Electronics",
    ...
  }
]
```

✅ **Resultado**: Los precios se convierten automáticamente usando la API externa de tasas de cambio.

---

### Paso 6: Obtener Historial de Precios

```bash
curl -X GET http://localhost:5000/api/products/$PRODUCT_ID/price-history
```

**Respuesta esperada:**
```json
[
  {
    "id": "7fa85f64-5717-4562-b3fc-2c963f66afa8",
    "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "price": 1299.99,
    "currency": "USD",
    "date": "2025-11-02T18:00:00Z"
  }
]
```

✅ **Resultado**: Se registra automáticamente el historial de precios cuando se crea o actualiza un producto.

---

### Paso 7: Actualizar un Producto

```bash
curl -X PUT http://localhost:5000/api/products/$PRODUCT_ID \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "name": "Laptop Dell XPS 15 (Updated)",
    "price": 1199.99
  }'
```

**Respuesta esperada:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Laptop Dell XPS 15 (Updated)",
  "price": 1199.99,  // Precio actualizado
  ...
}
```

**Verifica la sincronización:**
```bash
# Verificar que el evento se procesó
docker-compose logs inventory-service | grep "Product updated"
```

✅ **Resultado**: El `InventoryService` recibe el evento y actualiza el nombre del producto en su base de datos.

---

### Paso 8: Reducir Stock (Venta)

```bash
curl -X POST http://localhost:5001/api/inventory/adjust \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "productId": "'$PRODUCT_ID'",
    "quantityChange": 10,
    "movementType": "Out",
    "reason": "Venta a cliente"
  }'
```

**Respuesta esperada:**
```json
{
  "currentStock": 40,  // 50 - 10 = 40
  ...
}
```

✅ **Resultado**: El stock se redujo correctamente.

---

### Paso 9: Obtener Historial de Movimientos

```bash
curl -X GET http://localhost:5001/api/inventory/$PRODUCT_ID/movements
```

**Respuesta esperada:**
```json
[
  {
    "id": "...",
    "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "quantityChange": 50,
    "movementType": "In",
    "reason": "Reabastecimiento inicial",
    "timestamp": "2025-11-02T18:05:00Z"
  },
  {
    "id": "...",
    "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "quantityChange": -10,
    "movementType": "Out",
    "reason": "Venta a cliente",
    "timestamp": "2025-11-02T18:10:00Z"
  }
]
```

✅ **Resultado**: Se mantiene un historial completo de todos los movimientos de inventario.

---

### Paso 10: Probar Errores Comunes

#### Error: Stock Insuficiente
```bash
# Intentar reducir más stock del disponible
curl -X POST http://localhost:5001/api/inventory/adjust \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "productId": "'$PRODUCT_ID'",
    "quantityChange": 100,
    "movementType": "Out",
    "reason": "Intento de vender más del disponible"
  }'
```

**Respuesta esperada:** `400 Bad Request` - "Insufficient stock"

#### Error: Sin Autorización
```bash
# Intentar crear producto sin token
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Producto sin permisos",
    "price": 100
  }'
```

**Respuesta esperada:** `401 Unauthorized`

#### Error: Token Expirado
Usa un token inválido o espera a que expire (60 minutos).

**Respuesta esperada:** `401 Unauthorized`

---

## 📡 Endpoints de la API

### AuthService (Puerto 5002)

| Método | Endpoint | Descripción | Autenticación |
|--------|----------|-------------|---------------|
| POST | `/api/auth/login` | Iniciar sesión | No |
| POST | `/api/auth/register` | Registrar usuario | No |

### ProductService (Puerto 5000)

| Método | Endpoint | Descripción | Autenticación |
|--------|----------|-------------|---------------|
| GET | `/api/products` | Obtener todos los productos | No |
| GET | `/api/products/{id}` | Obtener producto por ID | No |
| GET | `/api/products/category/{category}` | Obtener por categoría | No |
| GET | `/api/products/{id}/price-history` | Historial de precios | No |
| POST | `/api/products` | Crear producto | Admin |
| PUT | `/api/products/{id}` | Actualizar producto | Admin |
| DELETE | `/api/products/{id}` | Eliminar producto | Admin |

**Parámetros opcionales:**
- `?currency=EUR` - Convertir precio a otra moneda (GET productos)

### InventoryService (Puerto 5001)

| Método | Endpoint | Descripción | Autenticación |
|--------|----------|-------------|---------------|
| GET | `/api/inventory/{productId}/stock` | Obtener stock disponible | No |
| GET | `/api/inventory/{productId}/movements` | Historial de movimientos | No |
| POST | `/api/inventory/adjust` | Ajustar inventario | Admin |

---

## 💻 Desarrollo Local

### 1. Clonar el repositorio

```bash
git clone <repository-url>
cd prueba_microservicios
```

### 2. Levantar servicios de infraestructura

```bash
# Levantar solo las bases de datos y servicios de infraestructura
docker-compose up -d postgres-products postgres-inventory postgres-auth redis rabbitmq
```

### 3. Ejecutar los servicios localmente

```bash
# AuthService
cd src/AuthService/AuthService.API
dotnet run

# ProductService (en otra terminal)
cd src/ProductService/ProductService.API
dotnet run

# InventoryService (en otra terminal)
cd src/InventoryService/InventoryService.API
dotnet run
```

### 4. Ejecutar migraciones manualmente (si es necesario)

```bash
# AuthService
cd src/AuthService
dotnet ef migrations add InitialCreate --project AuthService.Infrastructure --startup-project AuthService.API
dotnet ef database update --project AuthService.Infrastructure --startup-project AuthService.API

# ProductService
cd src/ProductService
dotnet ef migrations add InitialCreate --project ProductService.Infrastructure --startup-project ProductService.API
dotnet ef database update --project ProductService.Infrastructure --startup-project ProductService.API

# InventoryService
cd src/InventoryService
dotnet ef migrations add InitialCreate --project InventoryService.Infrastructure --startup-project InventoryService.API
dotnet ef database update --project InventoryService.Infrastructure --startup-project InventoryService.API
```

---

## ⚙️ Configuración

### Variables de Entorno (Docker Compose)

Las variables de entorno están configuradas en `docker-compose.yml`:

- **JWT Secret Key**: `JWT__SecretKey` - Clave secreta para firmar tokens JWT
- **JWT Issuer**: `JWT__Issuer` - Emisor del token (usualmente "AuthService")
- **Connection Strings**: Configuración de las bases de datos PostgreSQL
- **Redis Connection**: `Redis__ConnectionString`
- **RabbitMQ Connection**: `RabbitMQ__ConnectionString`

### Usuarios Predefinidos (Desarrollo)

El `AuthService` crea automáticamente dos usuarios al iniciar:

- **Admin**:
  - Email: `admin@test.com`
  - Password: `Admin123!`
  - Role: `Admin`

- **User**:
  - Email: `user@test.com`
  - Password: `User123!`
  - Role: `User`

---

## 📝 Notas Importantes

1. **Seguridad**: En producción, cambia la clave JWT y las contraseñas por defecto
2. **Bases de Datos**: Se crean automáticamente al iniciar los servicios
3. **Caché**: Redis debe estar disponible para que la caché funcione correctamente
4. **RabbitMQ**: Asegúrate de que esté corriendo para la sincronización de eventos
5. **Migraciones**: Se aplican automáticamente al iniciar los servicios
6. **Eventos**: Los eventos son idempotentes, pueden procesarse múltiples veces sin duplicar datos

---

## 🎯 Características Implementadas

✅ **Autenticación y Autorización**
- Login y registro de usuarios
- Generación de tokens JWT compartidos
- Roles: Admin y User
- Hash de contraseñas con BCrypt

✅ **Gestión de Productos**
- CRUD completo
- Historial de precios
- Conversión de moneda (USD a EUR, GBP, etc.)
- Búsqueda por categoría

✅ **Gestión de Inventario**
- Control de stock por producto
- Movimientos de inventario (entradas/salidas)
- Historial de movimientos
- Validación de stock suficiente

✅ **Sincronización con Eventos**
- Publicación de eventos al crear/actualizar/eliminar productos
- Consumo de eventos en InventoryService
- Idempotencia de eventos procesados

✅ **Caché Distribuida**
- Caché de productos en Redis
- Caché de stock disponible
- Caché de tasas de conversión
- TTL configurable

✅ **Testing**
- 27 tests unitarios (9 por microservicio)
- Cobertura de casos de éxito y error
- Mocking de dependencias con Moq

---

## 📚 Recursos Adicionales

- **Swagger UI**: Documentación interactiva de cada servicio en `/swagger`
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **Logs**: Ver logs de cada servicio con `docker-compose logs -f <service-name>`

---

## 👨‍💻 Autor

Implementado con arquitectura de microservicios en .NET 8
