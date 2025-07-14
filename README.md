# 📊 LogStream Analytics

Una plataforma completa de análisis de logs en tiempo real construida con **ASP.NET Core 8**, **Blazor Server**, y **MudBlazor**.

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

## 🛠️ Tecnologías Utilizadas

### Backend
- **ASP.NET Core 8.0** - Framework web
- **Entity Framework Core** - ORM para base de datos
- **SignalR** - Comunicación en tiempo real
- **Serilog** - Logging estructurado
- **MediatR** - Patrón mediador para CQRS

### Frontend
- **Blazor Server** - Framework UI interactivo
- **MudBlazor** - Componentes Material Design
- **Chart.js** - Gráficos y visualizaciones
- **CSS Custom Properties** - Sistema de diseño consistente

### Infraestructura
- **SQL Server** - Base de datos principal
- **Redis** - Cache distribuido
- **Elasticsearch** - Motor de búsqueda de logs
- **Application Insights** - Monitoreo y telemetría

## 🚀 Instalación y Configuración

### Prerrequisitos
- **.NET 8.0 SDK** o superior
- **SQL Server** (LocalDB o instancia completa)
- **Redis** (opcional, para cache)
- **Elasticsearch** (opcional, para búsqueda avanzada)

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

## 🎨 Personalización

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

## 📋 Roadmap

### 🔄 Próximas Funcionalidades
- [ ] **API REST completa** para integración externa
- [ ] **Alertas por email/SMS** configurables
- [ ] **Dashboards personalizables** por usuario
- [ ] **Machine Learning** para detección de anomalías
- [ ] **Multi-tenancy** avanzada
- [ ] **Exportación a Excel/PDF** funcional

### 🐛 Mejoras Técnicas
- [ ] **Unit Tests** completos
- [ ] **Integration Tests** end-to-end
- [ ] **Performance Testing** con carga
- [ ] **Security Audit** completo

## 🤝 Contribución

1. **Fork** el repositorio
2. **Crea** una rama para tu feature (`git checkout -b feature/amazing-feature`)
3. **Commit** tus cambios (`git commit -m 'Add amazing feature'`)
4. **Push** a la rama (`git push origin feature/amazing-feature`)
5. **Abre** un Pull Request

## 📄 Licencia

Este proyecto está bajo la licencia MIT. Ver `LICENSE` para más detalles.

## 🙏 Agradecimientos

- **MudBlazor** - Por los componentes Material Design
- **Chart.js** - Por los gráficos interactivos
- **ASP.NET Core Team** - Por el excelente framework

## 📞 Soporte

¿Tienes preguntas o necesitas ayuda?

- **Issues**: [GitHub Issues](https://github.com/cavr46/logstream-analytics/issues)
- **Discussions**: [GitHub Discussions](https://github.com/cavr46/logstream-analytics/discussions)
- **Email**: support@logstream-analytics.com

---

**Desarrollado con ❤️ usando ASP.NET Core 8 y Blazor Server**