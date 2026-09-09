using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace Clyvo.Insights.Api.Observabilidade;

/// <summary>
/// Agrega o histograma de duração de requisição que o próprio ASP.NET Core
/// emite, para expô-lo em <c>/metrics</c>.
/// </summary>
/// <remarks>
/// <para>
/// A medição não é refeita aqui. O runtime já publica
/// <c>http.server.request.duration</c> no meter
/// <c>Microsoft.AspNetCore.Hosting</c>, com rota, método e status como tags, e é
/// esse instrumento que o <see cref="MeterListener"/> escuta. Medir de novo num
/// middleware próprio daria dois números para a mesma coisa — os dois
/// plausíveis, divergindo no primeiro caso de borda.
/// </para>
/// <para>
/// Guarda contagem, soma e máximo por rota, e não a distribuição inteira: para
/// responder "qual rota está lenta" e "quanto estamos errando", média e pico
/// bastam. Percentil exigiria reter as amostras, e reter amostras num processo
/// que não tem coletor na frente é vazamento de memória com outro nome.
/// </para>
/// </remarks>
public sealed class ColetorDeMetricas : IDisposable
{
    private const string MeterDoHosting = "Microsoft.AspNetCore.Hosting";
    private const string InstrumentoDeDuracao = "http.server.request.duration";

    private readonly ConcurrentDictionary<string, AcumuladoDaRota> _porRota = new();
    private readonly ConcurrentDictionary<string, long> _porFaixaDeStatus = new();
    private readonly MeterListener _escuta;
    private readonly DateTime _inicioUtc = DateTime.UtcNow;

    public ColetorDeMetricas()
    {
        _escuta = new MeterListener();

        _escuta.InstrumentPublished = (instrumento, escuta) =>
        {
            if (instrumento.Meter.Name == MeterDoHosting && instrumento.Name == InstrumentoDeDuracao)
            {
                escuta.EnableMeasurementEvents(instrumento);
            }
        };

        _escuta.SetMeasurementEventCallback<double>(Registrar);
        _escuta.Start();
    }

    /// <summary>
    /// Chamado pelo runtime na thread que atendeu a requisição — daí as
    /// estruturas concorrentes e o acumulador com trava própria.
    /// </summary>
    private void Registrar(
        Instrument instrumento,
        double duracaoEmSegundos,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? estado)
    {
        string rota = "(sem rota)";
        string metodo = "-";
        int status = 0;

        foreach (var tag in tags)
        {
            switch (tag.Key)
            {
                // Rota, e não caminho: /api/insights/metas/7 e /api/insights/metas/9
                // são a mesma rota, e agrupar por caminho criaria uma série por id.
                case "http.route":
                    rota = tag.Value?.ToString() ?? rota;
                    break;
                case "http.request.method":
                    metodo = tag.Value?.ToString() ?? metodo;
                    break;
                case "http.response.status_code":
                    status = Convert.ToInt32(tag.Value, CultureInfo.InvariantCulture);
                    break;
            }
        }

        _porRota
            .GetOrAdd($"{metodo} {rota}", chave => new AcumuladoDaRota(chave))
            .Registrar(duracaoEmSegundos * 1000);

        _porFaixaDeStatus.AddOrUpdate(Faixa(status), 1, (_, atual) => atual + 1);
    }

    /// <summary>Foto do estado atual, para o endpoint.</summary>
    public object Ler()
    {
        var faixas = _porFaixaDeStatus.ToArray();
        var total = faixas.Sum(f => f.Value);
        var comErro = faixas.Where(f => f.Key is "4xx" or "5xx").Sum(f => f.Value);

        return new
        {
            servico = "clyvo-insights",
            coletadoEmUtc = DateTime.UtcNow,
            desdeUtc = _inicioUtc,
            requisicoes = total,
            taxaDeErro = total == 0 ? 0d : Math.Round((double)comErro / total, 4),
            respostasPorFaixa = faixas
                .OrderBy(f => f.Key, StringComparer.Ordinal)
                .ToDictionary(f => f.Key, f => f.Value),
            duracaoPorRota = _porRota.Values
                .Select(a => a.Ler())
                .OrderByDescending(r => r.duracaoMediaMs)
                .ToArray()
        };
    }

    /// <summary>
    /// Faixa, e não status exato. O que se quer saber é "quanto estamos
    /// errando"; a distinção entre 401 e 403 já está no log, com a rota e a
    /// correlação junto.
    /// </summary>
    private static string Faixa(int status) => status switch
    {
        >= 200 and < 300 => "2xx",
        >= 300 and < 400 => "3xx",
        >= 400 and < 500 => "4xx",
        >= 500 => "5xx",
        _ => "outros"
    };

    public void Dispose() => _escuta.Dispose();

    private sealed class AcumuladoDaRota
    {
        private readonly object _trava = new();
        private readonly string _rota;

        private long _quantidade;
        private double _somaMs;
        private double _maximoMs;

        public AcumuladoDaRota(string rota) => _rota = rota;

        public void Registrar(double duracaoMs)
        {
            lock (_trava)
            {
                _quantidade++;
                _somaMs += duracaoMs;

                if (duracaoMs > _maximoMs)
                {
                    _maximoMs = duracaoMs;
                }
            }
        }

        public MetricaDeRota Ler()
        {
            lock (_trava)
            {
                return new MetricaDeRota(
                    _rota,
                    _quantidade,
                    Math.Round(_quantidade == 0 ? 0 : _somaMs / _quantidade, 2),
                    Math.Round(_maximoMs, 2));
            }
        }
    }

    /// <param name="rota">Método e template da rota.</param>
    /// <param name="requisicoes">Quantas passaram por ela.</param>
    /// <param name="duracaoMediaMs">Duração média em milissegundos.</param>
    /// <param name="duracaoMaximaMs">Pior caso observado.</param>
    public sealed record MetricaDeRota(
        string rota,
        long requisicoes,
        double duracaoMediaMs,
        double duracaoMaximaMs);
}
