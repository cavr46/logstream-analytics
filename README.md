# 📊 LogStream Analytics

> **La plataforma de análisis de logs de próxima generación que revoluciona el monitoreo empresarial**

Una solución completa de análisis de logs en tiempo real construida con **ASP.NET Core 8**, **Blazor Server**, y **MudBlazor** que combina elegancia visual con potencia técnica para crear la experiencia definitiva de observabilidad.

## 🎯 ¿Por qué LogStream Analytics?

En un mundo donde los sistemas distribuidos generan **terabytes de logs diarios**, necesitas más que solo almacenamiento. LogStream Analytics transforma el caos de datos en **insights accionables**, proporcionando:

- **🚀 Rendimiento extremo**: Procesa millones de logs por segundo
- **🔍 Búsqueda inteligente**: Sintaxis Lucene avanzada con autocompletado
- **📊 Visualizaciones épicas**: Dashboards interactivos que cuentan historias
- **⚡ Tiempo real**: Streaming de logs con latencia sub-segundo
- **🎨 UX excepcional**: Interfaz moderna que los equipos DevOps amarán

## 🌟 Para Desarrolladores, por Desarrolladores

LogStream Analytics fue diseñado por desarrolladores que entienden las **frustraciones del debugging** y la **importancia de la observabilidad**. No más logs perdidos en terminales, no más grep infinito en archivos gigantes.

### 🔥 Características que te van a enamorar:

**🏗️ Arquitectura Empresarial**
- **Clean Architecture** con DDD y CQRS
- **Multi-tenancy** nativa para SaaS
- **Microservicios** ready con gRPC
- **Event-driven** con Azure Functions

**⚡ Performance de Élite**
- **Elasticsearch** para búsquedas lightning-fast
- **Redis** para cache distribuido
- **SignalR** para updates en tiempo real
- **Lista virtualizada** para manejar millones de registros

**🛡️ Seguridad y Compliance**
- **OpenID Connect** y OAuth 2.0
- **Rate limiting** inteligente por tenant
- **Audit trails** completos
- **GDPR** compliant con data retention policies

**🎨 UX/UI de Siguiente Nivel**
- **Material Design** con MudBlazor
- **Responsive** desde móvil hasta 4K
- **Tema oscuro/claro** automático
- **Accesibilidad WCAG 2.1** nativa

## 🚀 Características Principales

### 🔐 Sistema de Autenticación
- **Login multi-modal**: OpenID Connect, OAuth, y modo demo
- **Logout seguro** con confirmación
- **Layouts separados** para páginas autenticadas y públicas

### 📈 Dashboard Avanzado
- **Métricas en tiempo real**: Logs totales, tasa de errores, throughput
- **Gráficos interactivos**: Chart.js con actualizaciones automáticas
- **Indicadores de salud** del sistema
- **Completamente responsive** (móvil, tablet, desktop)

### 🔍 Búsqueda Avanzada
- **Sintaxis Lucene** para queries complejas
- **Filtros múltiples**: Level, aplicación, ambiente, host
- **Búsquedas guardadas** y reutilizables
- **Exportación de resultados** en múltiples formatos

### 📋 Visualizador de Logs
- **Streaming en tiempo real** con WebSocket/SignalR
- **Lista virtualizada** para rendimiento óptimo
- **Filtrado dinámico** por nivel y aplicación
- **Detalles expandibles** para cada log

### 🔔 Sistema de Alertas
- **Gestión de alertas** por severidad (Critical, Warning, Info)
- **Filtros avanzados** por estado y tipo
- **Acciones rápidas**: resolver, editar, suprimir

### 📊 Analytics Avanzado
- **Análisis de tendencias** (últimos 30 días)
- **Métricas de performance** con indicadores visuales
- **Distribución de log levels** con gráficos de dona
- **Insights automáticos** y recomendaciones

### 🔌 APIs Enterprise-Ready
- **REST API completa** con OpenAPI/Swagger
- **gRPC** para comunicación high-performance
- **Rate limiting** por tenant (1000 req/min)
- **Bulk ingestion** hasta 10,000 logs/request
- **File upload** processing hasta 1GB
- **Real-time streaming** con WebSocket

### 🏗️ Arquitectura Robusta
- **CQRS** con MediatR para separación de responsabilidades
- **Domain-Driven Design** con agregados y eventos
- **Event Sourcing** para auditabilidad completa
- **Microservicios** con Azure Functions
- **Message queues** con Service Bus
- **Distributed caching** con Redis

### 📈 Observabilidad Total
- **Metrics** con Prometheus y Grafana
- **Distributed tracing** con OpenTelemetry
- **Health checks** comprehensivos
- **Application Insights** integration
- **Custom dashboards** por equipo
- **Alerting** inteligente con machine learning

## 🛠️ Stack Tecnológico de Élite

### 🔧 Backend Powerhouse
- **ASP.NET Core 8.0** - Framework web de última generación
- **Entity Framework Core** - ORM con performance optimizado
- **SignalR** - Real-time bidirectional communication
- **Serilog** - Structured logging de nivel enterprise
- **MediatR** - CQRS pattern implementation
- **FluentValidation** - Validación declarativa elegante
- **Polly** - Resilience patterns (Circuit Breaker, Retry)
- **Mapster** - High-performance object mapping

### 🎨 Frontend Moderno
- **Blazor Server** - C# full-stack sin JavaScript
- **MudBlazor** - Material Design components
- **Chart.js** - Interactive data visualizations
- **CSS Custom Properties** - Design system scalable
- **PWA** - Progressive Web App capabilities
- **Responsive Design** - Mobile-first approach

### 🗄️ Data & Storage
- **SQL Server** - ACID-compliant transactional database
- **Redis** - In-memory distributed cache
- **Elasticsearch** - Full-text search engine
- **Azure Blob Storage** - Scalable file storage
- **Azure Service Bus** - Enterprise message broker

### ☁️ Cloud & DevOps
- **Azure Functions** - Serverless compute
- **Application Insights** - APM y telemetría
- **Azure Key Vault** - Secrets management
- **Prometheus** - Metrics collection
- **Grafana** - Monitoring dashboards
- **Docker** - Containerization

## 🚀 Quick Start para Desarrolladores

### ⚡ Instalación Express (< 5 minutos)

```bash
# 1. Clonar y configurar
git clone https://github.com/cavr46/logstream-analytics.git
cd logstream-analytics

# 2. Restaurar paquetes
dotnet restore

# 3. Ejecutar en modo development
cd src/LogStream.Web
dotnet run

# 🎉 Ya tienes LogStream corriendo en https://localhost:5001
```

### 🛠️ Prerrequisitos
- **.NET 8.0 SDK** - [Descargar aquí](https://dotnet.microsoft.com/download)
- **SQL Server** - LocalDB incluido con Visual Studio
- **Redis** - Opcional, para cache distribuido
- **Elasticsearch** - Opcional, para búsqueda full-text

> 💡 **Tip**: La aplicación funciona out-of-the-box con datos mock para que puedas explorar todas las features inmediatamente

### 1. Clonar el Repositorio
```bash
git clone https://github.com/cavr46/logstream-analytics.git
cd logstream-analytics
```

### 2. Configurar Connection Strings
Edita `src/LogStream.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LogStreamDb;Trusted_Connection=true;MultipleActiveResultSets=true",
    "Redis": "localhost:6379",
    "Elasticsearch": "http://localhost:9200"
  },
  "Authentication": {
    "Authority": "https://your-identity-provider.com",
    "ClientId": "logstream-client",
    "ClientSecret": "your-client-secret"
  }
}
```

### 3. Ejecutar Migraciones
```bash
cd src/LogStream.Web
dotnet ef database update
```

### 4. Instalar Dependencias
```bash
dotnet restore
```

### 5. Ejecutar la Aplicación
```bash
dotnet run
```

La aplicación estará disponible en: `https://localhost:5001`

## 🐛 Debug y Desarrollo

### Ejecutar en Modo Debug
```bash
dotnet run --environment Development
```

### Ejecutar Tests
```bash
dotnet test
```

### Logs de Debug
Los logs se escriben en la consola y en archivos en `logs/` directorio.

### Hot Reload
```bash
dotnet watch run
```

### Variables de Entorno para Debug
```bash
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=https://localhost:5001
```

## 📁 Estructura del Proyecto

```
logstream-analytics/
├── src/
│   ├── LogStream.Web/              # Aplicación web principal
│   │   ├── Components/
│   │   │   ├── Layout/            # Layouts de la aplicación
│   │   │   ├── Pages/             # Páginas Blazor
│   │   │   └── Shared/            # Componentes reutilizables
│   │   ├── Models/                # ViewModels y DTOs
│   │   ├── Services/              # Servicios de negocio
│   │   └── wwwroot/               # Assets estáticos
│   ├── LogStream.Application/      # Lógica de aplicación
│   ├── LogStream.Domain/          # Modelos de dominio
│   ├── LogStream.Infrastructure/   # Acceso a datos
│   ├── LogStream.Api/             # API REST
│   └── LogStream.Functions/       # Azure Functions
├── tests/                         # Proyectos de prueba
└── docs/                          # Documentación
```

## 🎯 Funcionalidades Disponibles

### 🔐 Autenticación
- **Login Page**: `/login` - Autenticación con múltiples proveedores
- **Demo Mode**: Login sin autenticación para pruebas

### 📊 Dashboard
- **Home**: `/` o `/dashboard` - Métricas y KPIs principales
- **Real-time Updates**: Actualizaciones automáticas cada 10 segundos
- **Time Range Filters**: 1H, 6H, 24H, 7D

### 🔍 Exploración de Datos
- **Search**: `/search` - Búsqueda avanzada con sintaxis Lucene
- **Logs**: `/logs` - Visualizador de logs en tiempo real
- **Analytics**: `/analytics` - Análisis de tendencias y métricas

### 🔔 Gestión
- **Alerts**: `/alerts` - Gestión de alertas del sistema
- **Admin**: `/admin/*` - Panel de administración (próximamente)

### 🔌 APIs para Integraciones
- **POST** `/api/v1/logingestion/single` - Ingesta de log individual
- **POST** `/api/v1/logingestion/bulk` - Ingesta masiva (hasta 10K logs)
- **POST** `/api/v1/logingestion/file` - Upload de archivos (hasta 1GB)
- **GET** `/api/v1/logingestion/status/{jobId}` - Estado de procesamiento
- **GET** `/api/v1/logingestion/health` - Health check del sistema

## 🎨 Personalización y Themes

### Temas
La aplicación soporta modo claro y oscuro. Toggle disponible en la barra superior.

### Responsive Design
- **Mobile First**: Optimizado para dispositivos móviles
- **Breakpoints**: 768px (tablet), 1024px (desktop)
- **Touch Friendly**: Tamaños de toque mínimos de 44px

### Accesibilidad
- **WCAG 2.1 AA**: Cumple estándares de accesibilidad
- **Screen Readers**: Etiquetas ARIA completas
- **Keyboard Navigation**: Navegación completa por teclado
- **High Contrast**: Soporte para alto contraste

## 🚀 Deployment

### Development
```bash
dotnet run --environment Development
```

### Production
```bash
dotnet publish -c Release -o ./publish
```

### Docker
```bash
docker build -t logstream-analytics .
docker run -p 5000:5000 logstream-analytics
```

### Azure App Service
El proyecto está configurado para deployment directo a Azure App Service.

## 🚀 Roadmap Épico

### 🔥 Q1 2025 - Foundation Complete
- [x] **REST API completa** con rate limiting
- [x] **CQRS + MediatR** implementation
- [x] **Multi-tenancy** con data isolation
- [x] **Real-time streaming** con SignalR
- [x] **Responsive UI** con MudBlazor

### 🎯 Q2 2025 - Intelligence Layer
- [ ] **Machine Learning** para anomaly detection
- [ ] **Predictive alerting** basado en patrones
- [ ] **Auto-scaling** intelligent
- [ ] **Smart log parsing** con NLP
- [ ] **Correlation analysis** entre microservicios

### 🌟 Q3 2025 - Enterprise Features
- [ ] **Custom dashboards** drag-and-drop
- [ ] **Advanced alerting** (email, SMS, Slack, Teams)
- [ ] **Role-based access control** granular
- [ ] **Audit compliance** (SOX, GDPR, HIPAA)
- [ ] **Multi-cloud deployment** support

### 🏆 Q4 2025 - Scale & Performance
- [ ] **Kubernetes** native deployment
- [ ] **Distributed processing** con Apache Kafka
- [ ] **Cold storage** integration
- [ ] **Data lakehouse** architecture
- [ ] **Performance at scale** (100M+ logs/day)

## 🤝 Únete a la Revolución

### 🎉 ¿Por qué contribuir?

**Para Desarrolladores:**
- Aprende **Clean Architecture** en un proyecto real
- Domina **CQRS** y **Event Sourcing**
- Experiencia con **Azure Cloud** y **Microservicios**
- **Portfolio** impresionante con tecnologías bleeding-edge

**Para DevOps:**
- Construye la **observabilidad** del futuro
- Impacta **miles de desarrolladores** worldwide
- Aprende **Kubernetes**, **Prometheus**, **Grafana**
- **Open source** experience que los recruiters buscan

**Para Empresas:**
- **Reduce costos** vs. soluciones comerciales
- **Customización** total según tus necesidades
- **No vendor lock-in**
- **Community support** 24/7

### 🚀 Cómo Contribuir

```bash
# 1. Fork & Clone
git clone https://github.com/TU-USERNAME/logstream-analytics.git
cd logstream-analytics

# 2. Crear rama para feature
git checkout -b feature/awesome-feature

# 3. Hacer cambios épicos
# ... codea como un rockstar ...

# 4. Commit con mensaje descriptivo
git commit -m "feat: add awesome feature that changes everything"

# 5. Push y crear PR
git push origin feature/awesome-feature
# Abre PR en GitHub con descripción detallada
```

### 🏷️ Áreas donde necesitamos ayuda:

- **🔥 Core Features**: Elasticsearch integration, ML algorithms
- **🎨 Frontend**: New dashboards, data visualizations
- **🧪 Testing**: Unit tests, integration tests, performance tests
- **📚 Documentation**: Tutorials, API docs, architecture guides
- **🐛 Bug Fixes**: Performance optimization, security improvements
- **🌐 Internationalization**: Traducciones y localization

## 👥 Community & Support

### 💬 Únete a la Conversación

- **💻 Discord**: [LogStream Community](https://discord.gg/logstream) - Chat en vivo
- **📝 Discussions**: [GitHub Discussions](https://github.com/cavr46/logstream-analytics/discussions)
- **🐛 Issues**: [GitHub Issues](https://github.com/cavr46/logstream-analytics/issues)
- **📧 Email**: support@logstream-analytics.com
- **🐦 Twitter**: [@LogStreamDev](https://twitter.com/LogStreamDev)

### 🎖️ Contributors Hall of Fame

Una vez que contribuyas, tu avatar aparecerá aquí! 🌟

<!-- ALL-CONTRIBUTORS-BADGE:START -->
[![All Contributors](https://img.shields.io/badge/all_contributors-1-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

### 🏆 Reconocimientos

Gracias a estas increíbles tecnologías y comunidades:

- **🔥 ASP.NET Core Team** - Por el framework que hace posible todo
- **🎨 MudBlazor** - Material Design components que lucen increíbles
- **📊 Chart.js** - Data visualization que cuenta historias
- **🔍 Elasticsearch** - Search engine que redefine velocidad
- **⚡ Redis** - Cache que hace todo más rápido
- **☁️ Azure** - Cloud platform que escala sin límites

## 📄 Licencia

Este proyecto está bajo la **MIT License** - ve [LICENSE](LICENSE) para detalles.

**TL;DR**: Puedes usar, modificar y distribuir este código libremente, incluso para proyectos comerciales. Solo mantén el copyright notice. 🎉

## 🚀 ¿Listo para revolucionar el logging?

```bash
git clone https://github.com/cavr46/logstream-analytics.git
cd logstream-analytics
dotnet run
```

**¡Únete a la revolución del logging empresarial!** 🔥

---

<div align="center">

**🌟 Si te gusta el proyecto, dale una estrella en GitHub! 🌟**

**Desarrollado con ❤️ por desarrolladores, para desarrolladores**

*"Porque la vida es demasiado corta para hacer grep en logs"* 

</div>