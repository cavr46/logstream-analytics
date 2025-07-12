using LogStream.Contracts.DTOs;

namespace LogStream.Application.Mappings;

public class LogEntryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<LogEntry, LogEntryDto>()
            .Map(dest => dest.TenantId, src => src.TenantId.Value)
            .Map(dest => dest.Level, src => src.Level.Value)
            .Map(dest => dest.Message, src => src.Message.Content)
            .Map(dest => dest.MessageTemplate, src => src.Message.Template)
            .Map(dest => dest.Source, src => new LogSourceDto
            {
                Application = src.Source.Application,
                Environment = src.Source.Environment,
                Server = src.Source.Server,
                Component = src.Source.Component
            })
            .Map(dest => dest.Metadata, src => src.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

        config.NewConfig<LogSource, LogSourceDto>();
        config.NewConfig<LogSourceDto, LogSource>()
            .ConstructUsing(src => new LogSource(src.Application, src.Environment, src.Server, src.Component));
    }
}