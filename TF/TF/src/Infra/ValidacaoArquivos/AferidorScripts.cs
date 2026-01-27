using System.Net;
using System.Net.Security;

namespace TF.src.Infra.ValidacaoArquivos;

public class CarregarScripts
{
    public required ConsoleLogger Logger { get; init; }

    public required IConfigProvider ConfigProvider { get; init; }
    public required RootConfig Config { get; init; }

    public required IGuardarDados GuardarDados { get; init; }

    public required IArmazenagemToken ArmazenagemToken { get; init; }
    public required IProvedorToken ProvedorToken { get; init; }

    public required HttpClient HttpApi { get; init; }
    public required HttpClient HttpUpload { get; init; }

    public required IColetorDados ApiCliente { get; init; }
    public required ITransformarDados Transformar { get; init; }
    public required ILoteador Loteador { get; init; }
    public required IUploader Uploader { get; init; }
    public required IAgendadorTabela Agendador { get; init; }
    public required CanalExecucao CanalExecucao { get; init; }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { HttpApi.Dispose(); } catch { }
        try { HttpUpload.Dispose(); } catch { }
    }
}

public static class Aferidor
{
    public static async Task<CarregarScripts> Construir(
        string caminhoConfig,
        int? paralelos = null,
        CancellationToken comando = default
    )
    {
        var logger = new ConsoleLogger();

        IConfigProvider provedorConfig = new ConfigJson(caminhoConfig);
        var config = await provedorConfig.CarregarConfiguracao(comando);

        IGuardarDados guardarDados = new GuardarDadosJson(caminhoConfig);
        logger.Info("[Aferidor] GuardarDados foi carregado com sucesso!");

        var tempToken = new GuardarTokenRemoto();
        var jsonToken = new GuardarTokenJson(caminhoConfig);
        IArmazenagemToken tokenArmazenado = new GerenciadorArmazenagemToken(tempToken, jsonToken);

        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            MaxConnectionsPerServer = Math.Max(20, (paralelos ?? 1) * 2),
            ConnectTimeout = TimeSpan.FromSeconds(16),
            AllowAutoRedirect = false,

            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(5),

            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = delegate { return true; }
            }
        };

        var httpToken = new HttpClient(handler, disposeHandler: false) {
            Timeout = TimeSpan.FromMinutes(10),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };
        var httpApi = new HttpClient(handler, disposeHandler: false) {
            Timeout = TimeSpan.FromMinutes(10),
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher            
        };
        var httpUpload = new HttpClient(handler, disposeHandler: false) {
            Timeout = TimeSpan.FromMinutes(10),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };
        httpUpload.DefaultRequestHeaders.ExpectContinue = false;

        IProvedorToken provedorToken = new ProvedorToken(httpToken, tokenArmazenado, config, logger);

        var apiCliente = new ApiCliente(
            http: httpApi,
            provedorToken: provedorToken,
            log: logger,
            intervalo: TimeSpan.FromSeconds(16),
            urlBase: config.UrlTF,
            headers: config.HeadersTF,
            tamanhoJanelaBusca: TimeSpan.FromDays(3),
            margemVerificacao: TimeSpan.FromDays(3)
        );

        var limpador = new LimparDados(new OpcoesLimpeza
        {
            RemoverEspaços = true,
            RemoverStringsVazias = true,
            ParseNumerosString = true,
            ParseBooleanosString = true,
            ParseDatasString = true
        });

        var transformar = new TransformarDados(limpador, logger, Math.Max(1, Environment.ProcessorCount - 1), comando);

        var loteador = new Loteador(maximoLinhas: 5000, maximoBytes: 2 * 1024 * 1024);

        IUploader uploader = new Uploader(httpUpload, config, logger);

        var agendador = new AgendadorImediato(paralelos ?? 1);

        var executor = new CanalExecucao(
                provedorConfig,
                guardarDados,
                apiCliente,
                transformar,
                loteador,
                uploader,
                logger,
                agendador
            );

        return new CarregarScripts
        {
            Logger = logger,
            ConfigProvider = provedorConfig,
            Config = config,
            GuardarDados = guardarDados,
            ArmazenagemToken = tokenArmazenado,
            ProvedorToken = provedorToken,
            HttpApi = httpApi,
            HttpUpload = httpUpload,
            ApiCliente = apiCliente,
            Transformar = transformar,
            Loteador = loteador,
            Uploader = uploader,
            Agendador = agendador,
            CanalExecucao = executor
        };
    }
}