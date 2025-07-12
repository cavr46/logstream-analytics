# LogStream Analytics - Setup Guide

## 🚀 Guía de Instalación y Configuración

Esta guía te llevará paso a paso para configurar y ejecutar la plataforma LogStream Analytics en tu entorno local o de producción.

## 📋 Prerrequisitos

### Software Requerido
- **.NET 8 SDK** (8.0.101 o superior)
- **Docker Desktop** (para servicios de infraestructura)
- **Visual Studio 2022** o **VS Code** con extensión C#
- **Git** para clonar el repositorio

### Opcional (para desarrollo avanzado)
- **Azure CLI** (para servicios Azure)
- **kubectl** (para Kubernetes)
- **Terraform** (para Infrastructure as Code)

## 🛠️ Instalación Paso a Paso

### 1. Clonar el Repositorio
```bash
git clone https://github.com/tu-usuario/logstream-analytics.git
cd logstream-analytics
```

### 2. Verificar Prerrequisitos
```bash
# Verificar .NET 8
dotnet --version

# Verificar Docker
docker --version
docker-compose --version

# Verificar que Docker está corriendo
docker ps
```

### 3. Configurar Servicios de Infraestructura

#### Opción A: Docker Compose (Recomendado para desarrollo)
```bash
# Levantar servicios de infraestructura
docker-compose up -d sql-server redis elasticsearch kibana rabbitmq prometheus grafana

# Verificar que todos los servicios estén corriendo
docker-compose ps

# Ver logs si hay problemas
docker-compose logs sql-server
docker-compose logs elasticsearch
```

#### Opción B: Servicios Locales
Si prefieres usar servicios locales:

**SQL Server LocalDB:**
```bash
# Crear base de datos
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

**Redis (Windows con Chocolatey):**
```bash
choco install redis-64
redis-server
```

**Elasticsearch:**
```bash
# Descargar desde elastic.co
# Configurar como servicio local
```

### 4. Configurar Cadenas de Conexión

Crear `appsettings.Development.json` en cada proyecto:

**src/LogStream.Api/appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LogStreamDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true",
    "Redis": "localhost:6379",
    "Elasticsearch": "http://localhost:9200"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**src/LogStream.Web/appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LogStreamDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true",
    "Redis": "localhost:6379",
    "Elasticsearch": "http://localhost:9200"
  },
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

### 5. Configurar Base de Datos

```bash
# Navegar al directorio del proyecto
cd src/LogStream.Infrastructure

# Instalar herramientas EF Core (si no las tienes)
dotnet tool install --global dotnet-ef

# Verificar instalación
dotnet ef --version

# Crear y aplicar migraciones
dotnet ef migrations add InitialCreate --startup-project ../LogStream.Api
dotnet ef database update --startup-project ../LogStream.Api
```

### 6. Restaurar Dependencias
```bash
# En el directorio raíz
dotnet restore

# Verificar que se restauraron correctamente
dotnet build
```

## 🚀 Ejecutar la Aplicación

### Opción 1: Ejecución Individual (Desarrollo)

**Terminal 1 - API REST:**
```bash
cd src/LogStream.Api
dotnet run
# La API estará disponible en: https://localhost:7001
# Swagger UI: https://localhost:7001/swagger
```

**Terminal 2 - gRPC Service:**
```bash
cd src/LogStream.Grpc
dotnet run
# gRPC service estará disponible en: https://localhost:7002
```

**Terminal 3 - Web UI:**
```bash
cd src/LogStream.Web
dotnet run
# Web UI estará disponible en: https://localhost:7000
```

**Terminal 4 - Azure Functions (Opcional):**
```bash
cd src/LogStream.Functions
# Instalar Azure Functions Core Tools si no las tienes
npm install -g azure-functions-core-tools@4 --unsafe-perm true
func start
```

### Opción 2: Docker Compose (Producción-like)

```bash
# Construir y ejecutar toda la aplicación
docker-compose up --build

# O en background
docker-compose up -d --build

# Ver logs
docker-compose logs -f

# Para detener
docker-compose down
```

## 🔍 Verificar la Instalación

### 1. Verificar Servicios de Infraestructura

**SQL Server:**
```bash
# Conectar y verificar base de datos
docker exec -it logstream-analytics_sql-server_1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "LogStream123!" -Q "SELECT name FROM sys.databases"
```

**Redis:**
```bash
# Verificar conexión
docker exec -it logstream-analytics_redis_1 redis-cli ping
```

**Elasticsearch:**
```bash
# Verificar salud del cluster
curl http://localhost:9200/_health
```

### 2. Verificar APIs

**REST API Health Check:**
```bash
curl https://localhost:7001/health
```

**Test API Endpoint:**
```bash
# Crear tenant de prueba
curl -X POST "https://localhost:7001/api/tenants" \
     -H "Content-Type: application/json" \
     -d '{"tenantId":"test-tenant","name":"Test Tenant","description":"Tenant for testing"}'

# Verificar que se creó
curl "https://localhost:7001/api/tenants/test-tenant"
```

### 3. Verificar Web UI

1. Abrir navegador en `https://localhost:7000`
2. Verificar que carga el dashboard
3. Verificar que no hay errores en la consola del navegador

### 4. Test de Ingesta de Logs

```bash
# Ingestar un log de prueba
curl -X POST "https://localhost:7001/api/logs/test-tenant" \
     -H "Content-Type: application/json" \
     -d '{
       "level": "INFO",
       "message": "Test log message from setup",
       "source": {
         "application": "SetupTest",
         "environment": "development"
       },
       "originalFormat": "JSON"
     }'

# Buscar el log
curl "https://localhost:7001/api/logs/test-tenant/search?query=setup&size=10"
```

## 🐛 Troubleshooting

### Problemas Comunes

#### 1. Error de Conexión a SQL Server
```
Error: Cannot open database "LogStreamDb" requested by the login
```

**Solución:**
```bash
# Verificar que SQL Server está corriendo
docker-compose ps sql-server

# Recrear base de datos
dotnet ef database drop --startup-project src/LogStream.Api --force
dotnet ef database update --startup-project src/LogStream.Api
```

#### 2. Error de Conexión a Redis
```
Error: No connection could be made to Redis
```

**Solución:**
```bash
# Verificar que Redis está corriendo
docker-compose ps redis

# Reiniciar Redis
docker-compose restart redis
```

#### 3. Error de Conexión a Elasticsearch
```
Error: Elasticsearch cluster is not available
```

**Solución:**
```bash
# Verificar estado de Elasticsearch
curl http://localhost:9200/_cluster/health

# Si no responde, reiniciar
docker-compose restart elasticsearch

# Esperar a que esté listo (puede tomar unos minutos)
```

#### 4. Errores de Compilación .NET
```
Error: Package X could not be found
```

**Solución:**
```bash
# Limpiar caché de NuGet
dotnet nuget locals all --clear

# Restaurar paquetes
dotnet restore

# Reconstruir
dotnet clean
dotnet build
```

#### 5. Puerto en Uso
```
Error: Address already in use
```

**Solución:**
```bash
# Verificar qué está usando el puerto
netstat -ano | findstr :7001

# Matar proceso si es necesario
taskkill /PID <PID> /F

# O cambiar puerto en launchSettings.json
```

### Logs Útiles

**Ver logs de Docker Compose:**
```bash
# Todos los servicios
docker-compose logs

# Servicio específico
docker-compose logs logstream-api
docker-compose logs sql-server
docker-compose logs elasticsearch
```

**Ver logs de aplicación .NET:**
Los logs se escriben en:
- Console (durante desarrollo)
- `logs/` directory en cada proyecto
- Application Insights (si está configurado)

## ⚙️ Configuración Avanzada

### Variables de Entorno

Crear archivo `.env` en la raíz del proyecto:
```bash
# .env
LOGSTREAM_ENVIRONMENT=Development
LOGSTREAM_SQL_PASSWORD=LogStream123!
LOGSTREAM_REDIS_HOST=localhost:6379
LOGSTREAM_ELASTICSEARCH_URL=http://localhost:9200

# Azure (opcional)
AZURE_SUBSCRIPTION_ID=your-subscription-id
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret

# Application Insights (opcional)
APPLICATIONINSIGHTS_CONNECTION_STRING=your-connection-string
```

### SSL/TLS Certificates

Para desarrollo con HTTPS:
```bash
# Confiar en certificados de desarrollo
dotnet dev-certs https --trust
```

### Performance Tuning

**SQL Server:**
```sql
-- Optimizar SQL Server para desarrollo
ALTER DATABASE LogStreamDb SET AUTO_CREATE_STATISTICS ON
ALTER DATABASE LogStreamDb SET AUTO_UPDATE_STATISTICS ON
```

**Elasticsearch:**
```yaml
# elasticsearch.yml
cluster.name: "logstream-cluster"
node.name: "logstream-node-1"
bootstrap.memory_lock: true
```

## 📊 Monitoreo Post-Instalación

### Health Checks

Configurar monitoreo de salud:
```bash
# API Health
curl https://localhost:7001/health

# Elasticsearch Health
curl http://localhost:9200/_cluster/health

# Redis Health
docker exec -it logstream-analytics_redis_1 redis-cli ping
```

### Métricas

Acceder a Grafana para métricas:
1. Abrir `http://localhost:3000`
2. Login: `admin` / `LogStream123!`
3. Importar dashboards desde `infrastructure/grafana/dashboards/`

### Logs Centralizados

Configurar agregación de logs:
1. Todos los servicios envían logs a Elasticsearch
2. Visualizar en Kibana: `http://localhost:5601`
3. Crear índices y visualizaciones

## 🔄 Actualizaciones

### Actualizar Dependencias
```bash
# Actualizar paquetes NuGet
dotnet list package --outdated
dotnet add package <PackageName>

# Actualizar imágenes Docker
docker-compose pull
docker-compose up -d --build
```

### Migraciones de Base de Datos
```bash
# Crear nueva migración
dotnet ef migrations add <MigrationName> --startup-project src/LogStream.Api

# Aplicar migración
dotnet ef database update --startup-project src/LogStream.Api
```

## ✅ Checklist de Verificación

- [ ] ✅ .NET 8 SDK instalado
- [ ] ✅ Docker Desktop corriendo
- [ ] ✅ Repositorio clonado
- [ ] ✅ Servicios Docker levantados
- [ ] ✅ Base de datos creada y migrada
- [ ] ✅ API REST respondiendo
- [ ] ✅ gRPC service funcionando
- [ ] ✅ Web UI cargando
- [ ] ✅ Test de ingesta exitoso
- [ ] ✅ Elasticsearch indexando
- [ ] ✅ Redis cacheando
- [ ] ✅ Monitoreo configurado

## 🎯 Próximos Pasos

1. **Configurar Autenticación**: Azure AD B2C para producción
2. **Configurar CI/CD**: GitHub Actions para deployment
3. **Optimizar Performance**: Tuning de base de datos y cache
4. **Configurar Alertas**: Monitoreo proactivo
5. **Documentar APIs**: Completar documentación Swagger

## 💬 Soporte

Si encuentras problemas durante la instalación:

1. Revisar esta guía paso a paso
2. Verificar la sección de troubleshooting
3. Revisar los logs de la aplicación
4. Crear un issue en GitHub con:
   - Descripción del problema
   - Pasos para reproducir
   - Logs relevantes
   - Información del entorno

¡Disfruta explorando LogStream Analytics! 🚀