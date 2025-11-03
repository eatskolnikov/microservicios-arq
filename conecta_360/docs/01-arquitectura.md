# 1. Diagrama de Arquitectura Detallado - Conecta360

## Visión General

La arquitectura de Conecta360 está diseñada siguiendo los principios de **microservicios**, **event-driven architecture** y **arquitectura en capas**, asegurando escalabilidad, resiliencia y mantenibilidad.

## Diagrama de Arquitectura - Vista de Niveles (C4 Model Level 1)

```mermaid
graph TB
    subgraph "Capa de Presentación"
        WEB[Portal Web<br/>React/Next.js]
        MOBILE[App Móvil<br/>React Native]
        CALL[Call Center<br/>UI Web]
        SOCIAL[Integración Redes<br/>Sociales]
    end

    subgraph "Capa de API Gateway"
        GATEWAY[API Gateway<br/>Kong/AWS API Gateway<br/>Rate Limiting, Auth, Routing]
    end

    subgraph "Capa de Aplicación - Microservicios"
        MS_AUTH[Servicio de<br/>Autenticación<br/>OAuth2/OIDC]
        MS_CASOS[Servicio de<br/>Gestión de Casos<br/>Core Domain]
        MS_NOTIF[Servicio de<br/>Notificaciones<br/>Email/SMS/Push]
        MS_CHATBOT[Servicio de<br/>Chatbot IA<br/>NLP/ML]
        MS_ANALYTICS[Servicio de<br/>Analítica<br/>CQRS Read Model]
        MS_DERIVACION[Servicio de<br/>Derivación<br/>Routing Rules]
        MS_SLA[Servicio de<br/>Gestión SLA<br/>Monitoring]
    end

    subgraph "Capa de Integración"
        ADAPTER[Adaptadores<br/>Legacy Systems<br/>REST/SOAP]
        SSO[SSO Nacional<br/>Identity Provider]
        MSG_PROVIDER[Proveedores<br/>Mensajería<br/>Twilio/SendGrid]
        SOCIAL_API[APIs Redes<br/>Sociales<br/>Facebook/Twitter/WhatsApp]
    end

    subgraph "Capa de Mensajería Asíncrona"
        KAFKA[Apache Kafka<br/>Event Streaming<br/>Topics por Dominio]
        DLQ[Dead Letter Queue<br/>Error Handling]
    end

    subgraph "Capa de Datos"
        DB_PRIMARY[(Base de Datos<br/>PostgreSQL<br/>Primary/Read Replicas)]
        DB_CACHE[(Redis Cache<br/>Sesiones/Tokens)]
        DB_SEARCH[(Elasticsearch<br/>Búsqueda/Logs)]
        DB_ANALYTICS[(Data Warehouse<br/>ClickHouse/Redshift<br/>Analytics)]
    end

    subgraph "Infraestructura y Observabilidad"
        MONITOR[Prometheus/Grafana<br/>Monitoring]
        LOGS[ELK Stack<br/>Centralized Logging]
        TRACE[Jaeger/Zipkin<br/>Distributed Tracing]
    end

    WEB --> GATEWAY
    MOBILE --> GATEWAY
    CALL --> GATEWAY
    SOCIAL --> GATEWAY

    GATEWAY --> MS_AUTH
    GATEWAY --> MS_CASOS
    GATEWAY --> MS_NOTIF
    GATEWAY --> MS_CHATBOT
    GATEWAY --> MS_ANALYTICS

    MS_AUTH --> SSO
    MS_CASOS --> KAFKA
    MS_NOTIF --> MSG_PROVIDER
    MS_CHATBOT --> MS_CASOS
    MS_DERIVACION --> MS_CASOS
    MS_SLA --> MS_CASOS

    MS_CASOS --> DB_PRIMARY
    MS_AUTH --> DB_CACHE
    MS_ANALYTICS --> DB_ANALYTICS
    MS_CHATBOT --> DB_SEARCH

    MS_CASOS --> ADAPTER
    KAFKA --> MS_DERIVACION
    KAFKA --> MS_NOTIF
    KAFKA --> MS_ANALYTICS

    MS_CASOS -.-> MONITOR
    MS_CASOS -.-> LOGS
    MS_CASOS -.-> TRACE

    KAFKA --> DLQ
```

## Diagrama de Arquitectura - Vista de Despliegue (Multi-Región)

```mermaid
graph TB
    subgraph "Región A - Producción Principal"
        LB_A[Load Balancer<br/>AWS ALB/NGINX]
        subgraph "Availability Zone 1-A"
            API_GW_A1[API Gateway<br/>Instance 1]
            MS_CASOS_A1[Microservicios<br/>Auto-scaling Group]
            DB_A1[(PostgreSQL<br/>Primary)]
        end
        subgraph "Availability Zone 2-A"
            API_GW_A2[API Gateway<br/>Instance 2]
            MS_CASOS_A2[Microservicios<br/>Auto-scaling Group]
            DB_A2[(PostgreSQL<br/>Standby)]
        end
        KAFKA_A[Kafka Cluster<br/>3 Brokers]
    end

    subgraph "Región B - Disaster Recovery"
        LB_B[Load Balancer<br/>AWS ALB/NGINX]
        subgraph "Availability Zone 1-B"
            API_GW_B1[API Gateway<br/>Instance 1]
            MS_CASOS_B1[Microservicios<br/>Auto-scaling Group]
            DB_B1[(PostgreSQL<br/>Replica)]
        end
        subgraph "Availability Zone 2-B"
            API_GW_B2[API Gateway<br/>Instance 2]
            MS_CASOS_B2[Microservicios<br/>Auto-scaling Group]
            DB_B2[(PostgreSQL<br/>Replica)]
        end
        KAFKA_B[Kafka Cluster<br/>3 Brokers]
    end

    subgraph "Región Global - CDN y Caché"
        CDN[CloudFront/CDN<br/>Static Assets]
        REDIS_GLOBAL[Redis Global<br/>Multi-region Replication]
    end

    USERS[Usuarios Ciudadanos] --> CDN
    CDN --> LB_A
    CDN -.Failover.-> LB_B

    LB_A --> API_GW_A1
    LB_A --> API_GW_A2
    LB_B --> API_GW_B1
    LB_B --> API_GW_B2

    API_GW_A1 --> MS_CASOS_A1
    API_GW_A2 --> MS_CASOS_A2
    API_GW_B1 --> MS_CASOS_B1
    API_GW_B2 --> MS_CASOS_B2

    MS_CASOS_A1 --> DB_A1
    MS_CASOS_A2 --> DB_A1
    MS_CASOS_B1 --> DB_B1
    MS_CASOS_B2 --> DB_B1

    DB_A1 -.Replication.-> DB_A2
    DB_A1 -.Replication.-> DB_B1
    DB_A1 -.Replication.-> DB_B2

    MS_CASOS_A1 --> KAFKA_A
    MS_CASOS_A2 --> KAFKA_A
    MS_CASOS_B1 --> KAFKA_B
    MS_CASOS_B2 --> KAFKA_B

    KAFKA_A -.Replication.-> KAFKA_B
    MS_CASOS_A1 --> REDIS_GLOBAL
    MS_CASOS_B1 --> REDIS_GLOBAL
```

## Flujo de Procesamiento de un Caso

```mermaid
sequenceDiagram
    participant C as Ciudadano
    participant GW as API Gateway
    participant AUTH as Servicio Auth
    participant CASOS as Servicio Casos
    participant KAFKA as Kafka
    participant DERIV as Servicio Derivación
    participant NOTIF as Servicio Notificaciones
    participant INST as Sistema Institucional
    participant DB as Base de Datos

    C->>GW: POST /api/v1/casos (con token)
    GW->>AUTH: Validar token
    AUTH-->>GW: Token válido + perfil
    GW->>CASOS: Crear caso
    CASOS->>DB: Persistir caso (Write)
    CASOS-->>GW: Caso creado (ID: 12345)
    GW-->>C: 201 Created {caso_id: 12345}
    
    CASOS->>KAFKA: Publicar evento: CasoCreado
    KAFKA->>DERIV: Consumir evento
    DERIV->>DERIV: Evaluar reglas de derivación
    DERIV->>CASOS: Actualizar caso (dependencia asignada)
    DERIV->>KAFKA: Publicar: CasoAsignado
    
    KAFKA->>NOTIF: Consumir: CasoAsignado
    NOTIF->>NOTIF: Preparar notificación
    NOTIF->>C: Enviar SMS/Email
    
    DERIV->>INST: Webhook/SOAP a sistema institucional
    INST-->>DERIV: Acknowledgment
    
    Note over KAFKA: Event-Driven Architecture<br/>Desacoplamiento temporal
```

## Capas del Sistema

### 1. Capa de Presentación
- **Portal Web**: React/Next.js con SSR para SEO
- **App Móvil**: React Native (iOS/Android)
- **Call Center UI**: Aplicación web interna para operadores
- **Widget Embebible**: Para integración en sitios de dependencias

### 2. Capa de API Gateway
- **Función**: Punto único de entrada, enrutamiento, autenticación, rate limiting
- **Tecnología**: Kong, AWS API Gateway o NGINX Plus
- **Características**:
  - Rate limiting por IP/usuario
  - Autenticación OAuth2/OIDC
  - Transformación de requests/responses
  - Circuit breaker patterns
  - Request/Response logging

### 3. Capa de Aplicación (Microservicios)

#### 3.1 Servicio de Autenticación y Autorización
- **Responsabilidad**: Gestión de identidades, SSO, tokens JWT
- **Stack**: Node.js/Python con OAuth2 server

#### 3.2 Servicio de Gestión de Casos (Core Domain)
- **Responsabilidad**: CRUD de casos, workflow, estados
- **Stack**: Java/Spring Boot o .NET Core
- **Patrón**: Domain-Driven Design (DDD)

#### 3.3 Servicio de Notificaciones
- **Responsabilidad**: Email, SMS, push notifications
- **Stack**: Node.js con workers asíncronos

#### 3.4 Servicio de Chatbot IA
- **Responsabilidad**: Clasificación inicial, routing inteligente
- **Stack**: Python con NLP (BERT/GPT-based models)

#### 3.5 Servicio de Derivación
- **Responsabilidad**: Reglas de negocio para asignar casos
- **Stack**: Java/.NET con rule engine (Drools/Ortools)

#### 3.6 Servicio de Gestión SLA
- **Responsabilidad**: Monitoreo de tiempos, alertas
- **Stack**: Go/Rust para alta concurrencia

#### 3.7 Servicio de Analítica (CQRS Read Model)
- **Responsabilidad**: Dashboards, reportes, KPIs
- **Stack**: Node.js con consultas optimizadas

### 4. Capa de Integración
- **Adaptadores Legacy**: REST/SOAP/gRPC adapters para sistemas antiguos
- **SSO Nacional**: Integración con identity provider gubernamental
- **Proveedores Mensajería**: Twilio, SendGrid, FCM
- **APIs Redes Sociales**: Facebook Graph API, Twitter API, WhatsApp Business API

### 5. Capa de Mensajería Asíncrona
- **Apache Kafka**: Event streaming para comunicación desacoplada
- **Topics principales**:
  - `casos.creados`
  - `casos.asignados`
  - `casos.actualizados`
  - `casos.cerrados`
  - `notificaciones.enviadas`
  - `sla.violados`

### 6. Capa de Datos
- **PostgreSQL**: Base de datos principal (ACID compliance)
  - Primary-Replica setup multi-región
  - Particionamiento por dependencia y fecha
- **Redis**: Cache distribuido (sesiones, tokens, datos frecuentes)
- **Elasticsearch**: Búsqueda full-text, logs centralizados
- **ClickHouse/Redshift**: Data warehouse para analítica

### 7. Infraestructura y Observabilidad
- **Monitoring**: Prometheus + Grafana (métricas)
- **Logging**: ELK Stack (Elasticsearch, Logstash, Kibana)
- **Tracing**: Jaeger/Zipkin (distributed tracing)
- **Alerting**: PagerDuty/OpsGenie

## Seguridad y Compliance

### Seguridad en Capas
```mermaid
graph LR
    A[WAF/CloudFront] --> B[API Gateway<br/>Rate Limiting]
    B --> C[Auth Service<br/>OAuth2/OIDC]
    C --> D[Microservicios<br/>mTLS]
    D --> E[Base de Datos<br/>Encryption at Rest]
    
    F[VPC/Network] --> D
    G[IAM/Roles] --> D
    H[Secrets Manager] --> D
```

### Consideraciones de Seguridad
1. **Encriptación en tránsito**: TLS 1.3 en todas las comunicaciones
2. **Encriptación en reposo**: AES-256 para bases de datos
3. **Segregación de datos**: Aislamiento lógico por dependencia (multitenancy)
4. **Auditoría**: Log completo de todas las operaciones (quién, qué, cuándo)
5. **Control de acceso**: RBAC con roles granulares (ciudadano, operador, supervisor, admin)
6. **Compliance**: GDPR-like, protección de datos personales

## Escalabilidad

### Estrategia de Escalado
- **Horizontal**: Auto-scaling basado en CPU/memoria/request rate
- **Vertical**: Instancias optimizadas por carga de trabajo
- **Cache**: Redis para reducir carga en base de datos
- **CDN**: CloudFront para assets estáticos
- **Read Replicas**: Distribución de lecturas en múltiples réplicas

### Capacidad Estimada
- **500,000 solicitudes/día** = ~5,787 req/s (peak)
- **Pico estimado**: 20,000 req/s (día de alta demanda)
- **Replicas necesarias**: ~40-50 instancias de microservicios (con balanceo)

## Resiliencia y Alta Disponibilidad

### Patrones Aplicados
1. **Circuit Breaker**: Evita cascading failures
2. **Retry con exponential backoff**: Para operaciones transitorias
3. **Bulkhead**: Aislamiento de recursos críticos
4. **Health Checks**: Endpoints `/health` y `/ready`
5. **Graceful Shutdown**: Cierre ordenado sin pérdida de datos
6. **Database Connection Pooling**: Optimización de conexiones

### Disaster Recovery
- **RTO (Recovery Time Objective)**: < 1 hora
- **RPO (Recovery Point Objective)**: < 15 minutos
- **Backup Strategy**: Diarios incrementales + backups completos semanales
- **Failover automático**: Route 53 health checks con DNS failover

## Consideraciones de Rendimiento

### Optimizaciones
1. **CQRS**: Separación de modelos de lectura/escritura
2. **Database Indexing**: Índices estratégicos en campos frecuentes
3. **Pagination**: Todos los endpoints de listado con paginación
4. **Compression**: Gzip/Brotli para responses grandes
5. **Async Processing**: Operaciones pesadas en background workers
6. **Connection Pooling**: Gestión eficiente de conexiones DB

### SLA de Rendimiento
- **P95 Response Time**: < 1.5s (requerimiento cumplido)
- **P99 Response Time**: < 3s
- **Availability**: 99.9% (≈ 8.76 horas de downtime/año)

---

**Siguiente**: Ver [Base de Datos](./02-base-datos.md)


