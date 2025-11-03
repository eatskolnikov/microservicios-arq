# 4. Diseño de APIs RESTful - Conecta360

## Visión General

Este documento define las APIs RESTful principales del sistema Conecta360, siguiendo los principios REST y las mejores prácticas de diseño de APIs.

## Especificación OpenAPI/Swagger

### Especificación Completa (OpenAPI 3.0)

```yaml
openapi: 3.0.3
info:
  title: Conecta360 API
  description: |
    Sistema Integral de Atención Ciudadana - Conecta360
    
    API RESTful para gestión de casos, ciudadanos, dependencias y servicios institucionales.
    
    **Autenticación:** OAuth2 / OpenID Connect (Bearer Token)
    **Versioning:** URL-based (v1, v2, etc.)
    **Rate Limiting:** 1000 requests/hour por usuario autenticado
  version: 1.0.0
  contact:
    name: API Support
    email: api-support@conecta360.gov.cv
  license:
    name: Propietario - Gobierno de Costa Verde

servers:
  - url: https://api.conecta360.gov.cv/v1
    description: Servidor de producción
  - url: https://api-staging.conecta360.gov.cv/v1
    description: Servidor de staging

tags:
  - name: Casos
    description: Gestión de casos ciudadanos
  - name: Ciudadanos
    description: Gestión de información de ciudadanos
  - name: Categorías
    description: Categorías de casos por dependencia
  - name: Dependencias
    description: Gestión de dependencias gubernamentales
  - name: Notificaciones
    description: Gestión de notificaciones
  - name: SLAs
    description: Seguimiento de Service Level Agreements
  - name: Analítica
    description: Dashboards y reportes

security:
  - bearerAuth: []

paths:
  /casos:
    get:
      summary: Listar casos
      description: |
        Obtiene una lista paginada de casos. Filtros disponibles por estado, dependencia, categoría, fecha, etc.
        Los resultados están ordenados por fecha de creación (más recientes primero).
      tags:
        - Casos
      parameters:
        - $ref: '#/components/parameters/PageNumber'
        - $ref: '#/components/parameters/PageSize'
        - $ref: '#/components/parameters/EstadoFilter'
        - $ref: '#/components/parameters/DependenciaFilter'
        - $ref: '#/components/parameters/CategoriaFilter'
        - $ref: '#/components/parameters/FechaDesdeFilter'
        - $ref: '#/components/parameters/FechaHastaFilter'
        - $ref: '#/components/parameters/BusquedaFilter'
      responses:
        '200':
          description: Lista de casos obtenida exitosamente
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/PaginatedCasos'
              examples:
                success:
                  value:
                    data:
                      - numero_caso: "CRV-2024-00012345"
                        titulo: "Bache en calle principal"
                        estado: "ASIGNADO"
                        prioridad: "ALTA"
                        fecha_creacion: "2024-01-15T10:30:00Z"
                        dependencia:
                          id: 1
                          nombre: "Ministerio de Obras Públicas"
                        categoria:
                          id: 5
                          nombre: "Infraestructura Vial"
        '401':
          $ref: '#/components/responses/UnauthorizedError'
        '429':
          $ref: '#/components/responses/RateLimitError'
    
    post:
      summary: Crear nuevo caso
      description: |
        Crea un nuevo caso ciudadano. El caso se crea inicialmente en estado PENDIENTE y será asignado automáticamente a la dependencia correspondiente según las reglas de derivación.
      tags:
        - Casos
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CasoCreateRequest'
            examples:
              incidencia_vial:
                value:
                  categoria_id: 5
                  dependencia_id: 1
                  canal_atencion_id: 1
                  titulo: "Bache en calle principal"
                  descripcion: "Hay un bache grande en la calle principal del barrio Los Rosales, cerca del parque central"
                  prioridad: "ALTA"
                  ubicacion:
                    lat: -12.046374
                    lng: -77.042793
                    direccion: "Calle Principal, Barrio Los Rosales"
                    barrio: "Los Rosales"
                  metadata:
                    foto_url: "https://storage.example.com/fotos/bache-123.jpg"
                    sugerencia: "Recomendado reparar en horario nocturno"
      responses:
        '201':
          description: Caso creado exitosamente
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/CasoResponse'
              examples:
                success:
                  value:
                    id: 12345
                    numero_caso: "CRV-2024-00012345"
                    titulo: "Bache en calle principal"
                    estado: "PENDIENTE"
                    prioridad: "ALTA"
                    fecha_creacion: "2024-01-15T10:30:00Z"
                    mensaje: "Su caso ha sido registrado. Número de caso: CRV-2024-00012345"
        '400':
          $ref: '#/components/responses/BadRequestError'
        '401':
          $ref: '#/components/responses/UnauthorizedError'
        '422':
          $ref: '#/components/responses/ValidationError'

  /casos/{numero_caso}:
    get:
      summary: Obtener caso por número
      description: |
        Obtiene la información completa de un caso específico, incluyendo historial de cambios, notificaciones y SLA.
      tags:
        - Casos
      parameters:
        - name: numero_caso
          in: path
          required: true
          schema:
            type: string
            pattern: '^CRV-\d{4}-\d{8}$'
          example: "CRV-2024-00012345"
      responses:
        '200':
          description: Caso encontrado
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/CasoDetalleResponse'
        '404':
          $ref: '#/components/responses/NotFoundError'
    
    patch:
      summary: Actualizar caso
      description: |
        Actualiza parcialmente un caso. Solo los campos proporcionados serán actualizados.
        Requiere permisos de OPERADOR o superior para la dependencia asignada.
      tags:
        - Casos
      parameters:
        - name: numero_caso
          in: path
          required: true
          schema:
            type: string
          example: "CRV-2024-00012345"
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CasoUpdateRequest'
      responses:
        '200':
          description: Caso actualizado exitosamente
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/CasoResponse'
        '403':
          $ref: '#/components/responses/ForbiddenError'
        '404':
          $ref: '#/components/responses/NotFoundError'

  /casos/{numero_caso}/historial:
    get:
      summary: Obtener historial de caso
      description: |
        Obtiene el historial completo de cambios y acciones realizadas sobre un caso.
      tags:
        - Casos
      parameters:
        - name: numero_caso
          in: path
          required: true
          schema:
            type: string
          example: "CRV-2024-00012345"
        - $ref: '#/components/parameters/PageNumber'
        - $ref: '#/components/parameters/PageSize'
      responses:
        '200':
          description: Historial obtenido exitosamente
          content:
            application/json:
              schema:
                type: object
                properties:
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/HistorialItem'
                  pagination:
                    $ref: '#/components/schemas/Pagination'

  /casos/{numero_caso}/comentarios:
    post:
      summary: Agregar comentario a caso
      description: |
        Agrega un comentario o actualización al caso. El comentario quedará registrado en el historial.
      tags:
        - Casos
      parameters:
        - name: numero_caso
          in: path
          required: true
          schema:
            type: string
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required:
                - comentario
              properties:
                comentario:
                  type: string
                  minLength: 5
                  maxLength: 2000
                interno:
                  type: boolean
                  default: false
                  description: Si es true, el comentario solo es visible para funcionarios
      responses:
        '201':
          description: Comentario agregado exitosamente
        '404':
          $ref: '#/components/responses/NotFoundError'

  /casos/{numero_caso}/cerrar:
    post:
      summary: Cerrar caso
      description: |
        Cierra un caso, cambiando su estado a CERRADO. Requiere estado actual RESUELTO o EN_PROCESO.
      tags:
        - Casos
      parameters:
        - name: numero_caso
          in: path
          required: true
          schema:
            type: string
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required:
                - motivo_cierre
              properties:
                motivo_cierre:
                  type: string
                  enum: [RESUELTO, CANCELADO, DUPLICADO, INVALIDO]
                comentario:
                  type: string
      responses:
        '200':
          description: Caso cerrado exitosamente
        '400':
          $ref: '#/components/responses/BadRequestError'

  /ciudadanos:
    get:
      summary: Buscar ciudadanos
      description: |
        Busca ciudadanos por documento de identidad, email o nombre. Requiere permisos de OPERADOR o superior.
      tags:
        - Ciudadanos
      parameters:
        - name: documento_identidad
          in: query
          schema:
            type: string
        - name: email
          in: query
          schema:
            type: string
        - name: nombre
          in: query
          schema:
            type: string
      responses:
        '200':
          description: Lista de ciudadanos encontrados
          content:
            application/json:
              schema:
                type: object
                properties:
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/CiudadanoResponse'
    
    post:
      summary: Registrar ciudadano
      description: |
        Registra un nuevo ciudadano en el sistema. Si el ciudadano ya existe (por documento o email), retorna el existente.
      tags:
        - Ciudadanos
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CiudadanoCreateRequest'
      responses:
        '201':
          description: Ciudadano registrado exitosamente
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/CiudadanoResponse'
        '409':
          description: Ciudadano ya existe

  /ciudadanos/{id}/casos:
    get:
      summary: Obtener casos de un ciudadano
      description: |
        Obtiene todos los casos asociados a un ciudadano específico.
      tags:
        - Ciudadanos
        - Casos
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
        - $ref: '#/components/parameters/PageNumber'
        - $ref: '#/components/parameters/PageSize'
      responses:
        '200':
          description: Lista de casos del ciudadano
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/PaginatedCasos'

  /categorias:
    get:
      summary: Listar categorías
      description: |
        Obtiene la lista de categorías disponibles, opcionalmente filtradas por dependencia.
      tags:
        - Categorías
      parameters:
        - name: dependencia_id
          in: query
          schema:
            type: integer
      responses:
        '200':
          description: Lista de categorías
          content:
            application/json:
              schema:
                type: object
                properties:
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/CategoriaResponse'

  /dependencias:
    get:
      summary: Listar dependencias
      description: |
        Obtiene la lista de todas las dependencias gubernamentales activas.
      tags:
        - Dependencias
      responses:
        '200':
          description: Lista de dependencias
          content:
            application/json:
              schema:
                type: object
                properties:
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/DependenciaResponse'

  /analytics/dashboard:
    get:
      summary: Dashboard central
      description: |
        Obtiene métricas y KPIs globales para el dashboard central. Requiere permisos de SUPERVISOR o superior.
      tags:
        - Analítica
      parameters:
        - name: fecha_desde
          in: query
          schema:
            type: string
            format: date
        - name: fecha_hasta
          in: query
          schema:
            type: string
            format: date
        - name: dependencia_id
          in: query
          schema:
            type: integer
      responses:
        '200':
          description: Métricas del dashboard
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DashboardResponse'

  /notificaciones:
    get:
      summary: Obtener notificaciones del usuario
      description: |
        Obtiene las notificaciones del usuario autenticado.
      tags:
        - Notificaciones
      parameters:
        - name: leido
          in: query
          schema:
            type: boolean
        - $ref: '#/components/parameters/PageNumber'
        - $ref: '#/components/parameters/PageSize'
      responses:
        '200':
          description: Lista de notificaciones
          content:
            application/json:
              schema:
                type: object
                properties:
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/NotificacionResponse'

components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
      description: Token JWT obtenido del endpoint de autenticación OAuth2

  parameters:
    PageNumber:
      name: page
      in: query
      schema:
        type: integer
        minimum: 1
        default: 1
      description: Número de página
    
    PageSize:
      name: size
      in: query
      schema:
        type: integer
        minimum: 1
        maximum: 100
        default: 20
      description: Tamaño de página
    
    EstadoFilter:
      name: estado
      in: query
      schema:
        type: string
        enum: [PENDIENTE, ASIGNADO, EN_PROCESO, RESUELTO, CERRADO, CANCELADO]
      description: Filtrar por estado
    
    DependenciaFilter:
      name: dependencia_id
      in: query
      schema:
        type: integer
      description: Filtrar por dependencia
    
    CategoriaFilter:
      name: categoria_id
      in: query
      schema:
        type: integer
      description: Filtrar por categoría
    
    FechaDesdeFilter:
      name: fecha_desde
      in: query
      schema:
        type: string
        format: date-time
      description: Filtrar casos desde fecha
    
    FechaHastaFilter:
      name: fecha_hasta
      in: query
      schema:
        type: string
        format: date-time
      description: Filtrar casos hasta fecha
    
    BusquedaFilter:
      name: q
      in: query
      schema:
        type: string
      description: Búsqueda full-text en título y descripción

  schemas:
    CasoCreateRequest:
      type: object
      required:
        - categoria_id
        - dependencia_id
        - canal_atencion_id
        - titulo
        - descripcion
      properties:
        categoria_id:
          type: integer
          example: 5
        dependencia_id:
          type: integer
          example: 1
        canal_atencion_id:
          type: integer
          example: 1
        titulo:
          type: string
          minLength: 5
          maxLength: 500
          example: "Bache en calle principal"
        descripcion:
          type: string
          minLength: 10
          maxLength: 5000
          example: "Hay un bache grande en la calle principal del barrio Los Rosales"
        prioridad:
          type: string
          enum: [BAJA, MEDIA, ALTA, URGENTE]
          default: MEDIA
        ubicacion:
          $ref: '#/components/schemas/Ubicacion'
        metadata:
          type: object
          additionalProperties: true
          example:
            foto_url: "https://storage.example.com/fotos/bache-123.jpg"
            sugerencia: "Recomendado reparar en horario nocturno"

    CasoUpdateRequest:
      type: object
      properties:
        estado:
          type: string
          enum: [PENDIENTE, ASIGNADO, EN_PROCESO, RESUELTO, CERRADO, CANCELADO]
        prioridad:
          type: string
          enum: [BAJA, MEDIA, ALTA, URGENTE]
        usuario_asignado_id:
          type: integer
        comentario:
          type: string

    CasoResponse:
      type: object
      properties:
        id:
          type: integer
          example: 12345
        numero_caso:
          type: string
          example: "CRV-2024-00012345"
        titulo:
          type: string
        estado:
          type: string
          enum: [PENDIENTE, ASIGNADO, EN_PROCESO, RESUELTO, CERRADO, CANCELADO]
        prioridad:
          type: string
          enum: [BAJA, MEDIA, ALTA, URGENTE]
        fecha_creacion:
          type: string
          format: date-time
        fecha_asignacion:
          type: string
          format: date-time
        fecha_resolucion:
          type: string
          format: date-time
        dependencia:
          $ref: '#/components/schemas/DependenciaMinimal'
        categoria:
          $ref: '#/components/schemas/CategoriaMinimal'
        ciudadano:
          $ref: '#/components/schemas/CiudadanoMinimal'

    CasoDetalleResponse:
      allOf:
        - $ref: '#/components/schemas/CasoResponse'
        - type: object
          properties:
            descripcion:
              type: string
            ubicacion:
              $ref: '#/components/schemas/Ubicacion'
            metadata:
              type: object
              additionalProperties: true
            usuario_asignado:
              type: object
              properties:
                id:
                  type: integer
                nombres:
                  type: string
                apellidos:
                  type: string
            historial_count:
              type: integer
            notificaciones_count:
              type: integer
            sla:
              $ref: '#/components/schemas/SLAResponse'

    Ubicacion:
      type: object
      properties:
        lat:
          type: number
          format: float
          example: -12.046374
        lng:
          type: number
          format: float
          example: -77.042793
        direccion:
          type: string
          example: "Calle Principal, Barrio Los Rosales"
        barrio:
          type: string
          example: "Los Rosales"
        ciudad:
          type: string
          example: "San José"

    PaginatedCasos:
      type: object
      properties:
        data:
          type: array
          items:
            $ref: '#/components/schemas/CasoResponse'
        pagination:
          $ref: '#/components/schemas/Pagination'

    Pagination:
      type: object
      properties:
        page:
          type: integer
        size:
          type: integer
        total:
          type: integer
        total_pages:
          type: integer
        has_next:
          type: boolean
        has_previous:
          type: boolean

    HistorialItem:
      type: object
      properties:
        id:
          type: integer
        fecha_cambio:
          type: string
          format: date-time
        estado_anterior:
          type: string
        estado_nuevo:
          type: string
        tipo_cambio:
          type: string
          enum: [CAMBIO_ESTADO, ASIGNACION, REASIGNACION, COMENTARIO]
        comentario:
          type: string
        usuario:
          type: object
          properties:
            id:
              type: integer
            nombres:
              type: string
            apellidos:
              type: string

    CiudadanoCreateRequest:
      type: object
      required:
        - documento_identidad
        - nombres
        - apellidos
        - email
      properties:
        documento_identidad:
          type: string
          pattern: '^\d{9,11}$'
          example: "123456789"
        nombres:
          type: string
          minLength: 2
          maxLength: 200
        apellidos:
          type: string
          minLength: 2
          maxLength: 200
        email:
          type: string
          format: email
        telefono:
          type: string
        direccion:
          type: string

    CiudadanoResponse:
      type: object
      properties:
        id:
          type: integer
        documento_identidad:
          type: string
        nombres:
          type: string
        apellidos:
          type: string
        email:
          type: string
        telefono:
          type: string
        fecha_registro:
          type: string
          format: date-time

    CiudadanoMinimal:
      type: object
      properties:
        id:
          type: integer
        nombres:
          type: string
        apellidos:
          type: string
        documento_identidad:
          type: string

    CategoriaResponse:
      type: object
      properties:
        id:
          type: integer
        codigo:
          type: string
        nombre:
          type: string
        descripcion:
          type: string
        tipo:
          type: string
        dependencia:
          $ref: '#/components/schemas/DependenciaMinimal'

    CategoriaMinimal:
      type: object
      properties:
        id:
          type: integer
        codigo:
          type: string
        nombre:
          type: string

    DependenciaResponse:
      type: object
      properties:
        id:
          type: integer
        codigo:
          type: string
          example: "MIN-SALUD"
        nombre:
          type: string
        descripcion:
          type: string
        tipo:
          type: string
          enum: [MINISTERIO, MUNICIPALIDAD, ORGANISMO_AUTONOMO]

    DependenciaMinimal:
      type: object
      properties:
        id:
          type: integer
        codigo:
          type: string
        nombre:
          type: string

    SLAResponse:
      type: object
      properties:
        id:
          type: integer
        estado:
          type: string
          enum: [VIGENTE, CUMPLIDO, VIOLADO]
        fecha_inicio:
          type: string
          format: date-time
        fecha_vencimiento:
          type: string
          format: date-time
        fecha_resolucion_objetivo:
          type: string
          format: date-time
        tiempo_resolucion_horas:
          type: integer
        violado:
          type: boolean
        tiempo_restante_horas:
          type: integer

    NotificacionResponse:
      type: object
      properties:
        id:
          type: integer
        tipo:
          type: string
          enum: [CREACION, ASIGNACION, ACTUALIZACION, CIERRE]
        canal:
          type: string
          enum: [EMAIL, SMS, PUSH, WHATSAPP]
        asunto:
          type: string
        contenido:
          type: string
        estado:
          type: string
          enum: [PENDIENTE, ENVIADO, FALLIDO, LEIDO]
        fecha_envio:
          type: string
          format: date-time
        fecha_leido:
          type: string
          format: date-time

    DashboardResponse:
      type: object
      properties:
        kpis:
          type: object
          properties:
            casos_totales:
              type: integer
            casos_pendientes:
              type: integer
            casos_resueltos:
              type: integer
            tiempo_promedio_resolucion_horas:
              type: number
            cumplimiento_sla_porcentaje:
              type: number
            satisfaccion_promedio:
              type: number
        casos_por_estado:
          type: object
          additionalProperties:
            type: integer
        casos_por_dependencia:
          type: array
          items:
            type: object
            properties:
              dependencia:
                type: string
              total:
                type: integer
        tendencias:
          type: array
          items:
            type: object
            properties:
              fecha:
                type: string
                format: date
              casos_creados:
                type: integer
              casos_resueltos:
                type: integer

    ErrorResponse:
      type: object
      properties:
        error:
          type: object
          properties:
            codigo:
              type: string
            mensaje:
              type: string
            detalles:
              type: array
              items:
                type: object
                properties:
                  campo:
                    type: string
                  mensaje:
                    type: string
            timestamp:
              type: string
              format: date-time
            ruta:
              type: string

  responses:
    BadRequestError:
      description: Solicitud inválida
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/ErrorResponse'
          example:
            error:
              codigo: "BAD_REQUEST"
              mensaje: "Los datos proporcionados no son válidos"
              detalles:
                - campo: "titulo"
                  mensaje: "El título debe tener al menos 5 caracteres"
    
    UnauthorizedError:
      description: No autenticado o token inválido
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/ErrorResponse'
          example:
            error:
              codigo: "UNAUTHORIZED"
              mensaje: "Token de autenticación inválido o expirado"
    
    ForbiddenError:
      description: No tiene permisos para realizar esta acción
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/ErrorResponse'
          example:
            error:
              codigo: "FORBIDDEN"
              mensaje: "No tiene permisos para actualizar este caso"
    
    NotFoundError:
      description: Recurso no encontrado
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/ErrorResponse'
          example:
            error:
              codigo: "NOT_FOUND"
              mensaje: "Caso no encontrado"
    
    ValidationError:
      description: Error de validación
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/ErrorResponse'
          example:
            error:
              codigo: "VALIDATION_ERROR"
              mensaje: "Los datos proporcionados no pasaron la validación"
              detalles:
                - campo: "email"
                  mensaje: "El formato del email no es válido"
    
    RateLimitError:
      description: Límite de solicitudes excedido
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/ErrorResponse'
          example:
            error:
              codigo: "RATE_LIMIT_EXCEEDED"
              mensaje: "Ha excedido el límite de solicitudes. Intente nuevamente más tarde."
              timestamp: "2024-01-15T10:30:00Z"
```

## Ejemplos de Uso

### Crear un caso (POST /casos)

**Request:**
```bash
curl -X POST "https://api.conecta360.gov.cv/v1/casos" \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "categoria_id": 5,
    "dependencia_id": 1,
    "canal_atencion_id": 1,
    "titulo": "Bache en calle principal",
    "descripcion": "Hay un bache grande en la calle principal del barrio Los Rosales, cerca del parque central. Representa un peligro para los vehículos.",
    "prioridad": "ALTA",
    "ubicacion": {
      "lat": -12.046374,
      "lng": -77.042793,
      "direccion": "Calle Principal, Barrio Los Rosales",
      "barrio": "Los Rosales"
    },
    "metadata": {
      "foto_url": "https://storage.example.com/fotos/bache-123.jpg",
      "sugerencia": "Recomendado reparar en horario nocturno"
    }
  }'
```

**Response:**
```json
{
  "id": 12345,
  "numero_caso": "CRV-2024-00012345",
  "titulo": "Bache en calle principal",
  "estado": "PENDIENTE",
  "prioridad": "ALTA",
  "fecha_creacion": "2024-01-15T10:30:00Z",
  "dependencia": {
    "id": 1,
    "codigo": "MIN-OBRAS",
    "nombre": "Ministerio de Obras Públicas"
  },
  "categoria": {
    "id": 5,
    "codigo": "INFRA-VIAL",
    "nombre": "Infraestructura Vial"
  },
  "mensaje": "Su caso ha sido registrado. Número de caso: CRV-2024-00012345"
}
```

### Obtener caso por número (GET /casos/{numero_caso})

**Request:**
```bash
curl -X GET "https://api.conecta360.gov.cv/v1/casos/CRV-2024-00012345" \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response:**
```json
{
  "id": 12345,
  "numero_caso": "CRV-2024-00012345",
  "titulo": "Bache en calle principal",
  "descripcion": "Hay un bache grande en la calle principal...",
  "estado": "ASIGNADO",
  "prioridad": "ALTA",
  "fecha_creacion": "2024-01-15T10:30:00Z",
  "fecha_asignacion": "2024-01-15T10:35:00Z",
  "dependencia": {
    "id": 1,
    "nombre": "Ministerio de Obras Públicas"
  },
  "categoria": {
    "id": 5,
    "nombre": "Infraestructura Vial"
  },
  "ciudadano": {
    "id": 5432,
    "nombres": "Juan",
    "apellidos": "Pérez",
    "documento_identidad": "123456789"
  },
  "usuario_asignado": {
    "id": 987,
    "nombres": "María",
    "apellidos": "González"
  },
  "ubicacion": {
    "lat": -12.046374,
    "lng": -77.042793,
    "direccion": "Calle Principal, Barrio Los Rosales",
    "barrio": "Los Rosales"
  },
  "sla": {
    "estado": "VIGENTE",
    "fecha_vencimiento": "2024-01-22T10:30:00Z",
    "tiempo_resolucion_horas": 168,
    "tiempo_restante_horas": 142,
    "violado": false
  },
  "historial_count": 3,
  "notificaciones_count": 2
}
```

### Listar casos con filtros (GET /casos)

**Request:**
```bash
curl -X GET "https://api.conecta360.gov.cv/v1/casos?estado=ASIGNADO&dependencia_id=1&page=1&size=20" \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response:**
```json
{
  "data": [
    {
      "numero_caso": "CRV-2024-00012345",
      "titulo": "Bache en calle principal",
      "estado": "ASIGNADO",
      "prioridad": "ALTA",
      "fecha_creacion": "2024-01-15T10:30:00Z",
      "dependencia": {
        "id": 1,
        "nombre": "Ministerio de Obras Públicas"
      },
      "categoria": {
        "id": 5,
        "nombre": "Infraestructura Vial"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "size": 20,
    "total": 156,
    "total_pages": 8,
    "has_next": true,
    "has_previous": false
  }
}
```

## Principios de Diseño REST

### 1. Recursos como Sustantivos
- `/casos` (no `/crearCaso`)
- `/ciudadanos` (no `/obtenerCiudadanos`)

### 2. Métodos HTTP Semánticos
- `GET`: Lectura (idempotente)
- `POST`: Creación
- `PATCH`: Actualización parcial
- `PUT`: Actualización completa (si aplica)
- `DELETE`: Eliminación (soft delete)

### 3. Códigos de Estado HTTP
- `200`: Éxito
- `201`: Creado
- `400`: Solicitud inválida
- `401`: No autenticado
- `403`: Sin permisos
- `404`: No encontrado
- `422`: Error de validación
- `429`: Rate limit excedido
- `500`: Error interno del servidor

### 4. Versionado
- **URL-based**: `/v1/casos`, `/v2/casos`
- **Header-based**: `Accept: application/vnd.conecta360.v1+json` (alternativa)

### 5. Paginación
- Query parameters: `?page=1&size=20`
- Headers de respuesta con información de paginación

### 6. Filtrado y Búsqueda
- Query parameters: `?estado=ASIGNADO&dependencia_id=1`
- Búsqueda full-text: `?q=bache`

### 7. Formato de Respuesta
- JSON consistente con estructura `{data: [], pagination: {}}`
- Errores en formato estándar: `{error: {codigo, mensaje, detalles}}`

---

**Siguiente**: Ver [APIs Asíncronas](./05-apis-asincronas.md)


