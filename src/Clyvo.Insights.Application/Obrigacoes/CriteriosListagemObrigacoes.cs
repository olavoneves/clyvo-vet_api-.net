using System.Globalization;
using Clyvo.Insights.Application.Excecoes;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Obrigacoes;

namespace Clyvo.Insights.Application.Obrigacoes;

/// <summary>Campos pelos quais a listagem de obrigações pode ser ordenada.</summary>
public enum CampoOrdenacaoObrigacoes
{
    DataPrevista = 1,

    /// <summary>Pela ordem do funil, não pela ordem alfabética do código.</summary>
    Status = 2,

    /// <summary>Pelo nome do pet.</summary>
    Pet = 3,

    ValorEstimado = 4
}

/// <summary>Direção da ordenação.</summary>
public enum DirecaoOrdenacao
{
    Asc = 1,
    Desc = 2
}

/// <summary>
/// A consulta de obrigações já validada e interpretada.
/// </summary>
/// <remarks>
/// <para>
/// Só nasce por <see cref="APartirDe"/>, e só nasce válida: quem recebe um
/// critério — o repositório, o gerador de links — não precisa conferir de novo
/// se a página é positiva ou se o campo de ordenação existe.
/// </para>
/// <para>
/// A ordenação é por lista branca. Aceitar qualquer nome de coluna faria do
/// parâmetro um jeito de ordenar por coluna que não é contrato, e um texto
/// livre virando <c>ORDER BY</c> é o tipo de atalho que acaba em injeção.
/// </para>
/// </remarks>
public sealed class CriteriosListagemObrigacoes
{
    public const int PaginaPadrao = 1;
    public const int TamanhoPadrao = 20;
    public const int TamanhoMaximo = 100;

    /// <summary><c>DS_CATEGORIA</c> é VARCHAR2(20) no core.</summary>
    private const int TamanhoMaximoCategoria = 20;

    private const string GrupoTratamento = "tratamento";
    private const string GrupoControle = "controle";

    private static readonly IReadOnlyDictionary<string, CampoOrdenacaoObrigacoes> CamposDeOrdenacao =
        new Dictionary<string, CampoOrdenacaoObrigacoes>(StringComparer.OrdinalIgnoreCase)
        {
            ["dataPrevista"] = CampoOrdenacaoObrigacoes.DataPrevista,
            ["status"] = CampoOrdenacaoObrigacoes.Status,
            ["pet"] = CampoOrdenacaoObrigacoes.Pet,
            ["valorEstimado"] = CampoOrdenacaoObrigacoes.ValorEstimado
        };

    private CriteriosListagemObrigacoes()
    {
    }

    /// <summary>Página pedida, a partir de 1.</summary>
    public int Pagina { get; private init; }

    /// <summary>Itens por página, de 1 a <see cref="TamanhoMaximo"/>.</summary>
    public int Tamanho { get; private init; }

    public StatusObrigacao? Status { get; private init; }

    public GrupoCoorte? Grupo { get; private init; }

    /// <summary>Já sem espaços e em maiúsculas, como o core grava.</summary>
    public string? Categoria { get; private init; }

    /// <summary>Data prevista a partir de, inclusive.</summary>
    public DateOnly? De { get; private init; }

    /// <summary>Data prevista até, inclusive.</summary>
    public DateOnly? Ate { get; private init; }

    public long? IdPet { get; private init; }

    public CampoOrdenacaoObrigacoes OrdenarPor { get; private init; }

    public DirecaoOrdenacao Direcao { get; private init; }

    /// <summary>Quantos itens pular para chegar à página pedida.</summary>
    public int Deslocamento => (Pagina - 1) * Tamanho;

    /// <summary>
    /// Valida e interpreta os parâmetros. Parâmetro ausente assume o padrão;
    /// parâmetro presente e inválido é erro, nunca padrão silencioso.
    /// </summary>
    /// <exception cref="ConsultaInvalidaException">
    /// Com todos os parâmetros inválidos de uma vez, por nome.
    /// </exception>
    public static CriteriosListagemObrigacoes APartirDe(ConsultaObrigacoesRequest consulta)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        var erros = new Dictionary<string, string[]>();

        var pagina = consulta.Pagina ?? PaginaPadrao;
        if (pagina < 1)
        {
            erros["pagina"] = new[] { $"A página começa em 1 (valor {pagina})." };
        }

        var tamanho = consulta.Tamanho ?? TamanhoPadrao;
        if (tamanho is < 1 or > TamanhoMaximo)
        {
            erros["tamanho"] = new[] { $"O tamanho da página vai de 1 a {TamanhoMaximo} (valor {tamanho})." };
        }
        else if (pagina >= 1 && (long)(pagina - 1) * tamanho > int.MaxValue)
        {
            // O deslocamento vira OFFSET no banco e precisa caber num int.
            erros["pagina"] = new[] { $"A página {pagina} passa do maior deslocamento suportado." };
        }

        StatusObrigacao? status = null;
        if (consulta.Status is not null)
        {
            if (CodigoDeStatus.TentarInterpretar(consulta.Status, out var interpretado))
            {
                status = interpretado;
            }
            else
            {
                erros["status"] = new[]
                {
                    $"Status '{consulta.Status}' desconhecido. Esperado um de: " +
                    string.Join(", ", Enum.GetValues<StatusObrigacao>().Select(s => s.Codigo())) + "."
                };
            }
        }

        GrupoCoorte? grupo = null;
        if (consulta.Grupo is not null)
        {
            grupo = InterpretarGrupo(consulta.Grupo);
            if (grupo is null)
            {
                erros["grupo"] = new[]
                {
                    $"Grupo '{consulta.Grupo}' desconhecido. Esperado '{GrupoTratamento}' ou '{GrupoControle}'."
                };
            }
        }

        string? categoria = null;
        if (consulta.Categoria is not null)
        {
            categoria = consulta.Categoria.Trim().ToUpperInvariant();
            if (categoria.Length is 0 or > TamanhoMaximoCategoria)
            {
                erros["categoria"] = new[]
                {
                    $"A categoria precisa ter de 1 a {TamanhoMaximoCategoria} caracteres."
                };
            }
        }

        if (consulta.De is { } de && consulta.Ate is { } ate && de > ate)
        {
            erros["de"] = new[] { $"O início do período ({de:yyyy-MM-dd}) é posterior ao fim ({ate:yyyy-MM-dd})." };
        }

        if (consulta.Pet is <= 0)
        {
            erros["pet"] = new[] { $"Id de pet inválido (valor {consulta.Pet})." };
        }

        var ordenarPor = CampoOrdenacaoObrigacoes.DataPrevista;
        if (consulta.OrdenarPor is not null
            && !CamposDeOrdenacao.TryGetValue(consulta.OrdenarPor.Trim(), out ordenarPor))
        {
            erros["ordenarPor"] = new[]
            {
                $"Não é possível ordenar por '{consulta.OrdenarPor}'. Campos aceitos: " +
                string.Join(", ", CamposDeOrdenacao.Keys) + "."
            };
        }

        var direcao = DirecaoOrdenacao.Asc;
        if (consulta.Direcao is not null)
        {
            switch (consulta.Direcao.Trim().ToLowerInvariant())
            {
                case "asc":
                    direcao = DirecaoOrdenacao.Asc;
                    break;
                case "desc":
                    direcao = DirecaoOrdenacao.Desc;
                    break;
                default:
                    erros["direcao"] = new[] { $"Direção '{consulta.Direcao}' desconhecida. Esperado 'asc' ou 'desc'." };
                    break;
            }
        }

        if (erros.Count > 0)
        {
            throw new ConsultaInvalidaException(erros);
        }

        return new CriteriosListagemObrigacoes
        {
            Pagina = pagina,
            Tamanho = tamanho,
            Status = status,
            Grupo = grupo,
            Categoria = categoria,
            De = consulta.De,
            Ate = consulta.Ate,
            IdPet = consulta.Pet,
            OrdenarPor = ordenarPor,
            Direcao = direcao
        };
    }

    /// <summary>Os mesmos critérios, em outra página. É o que os links de navegação usam.</summary>
    public CriteriosListagemObrigacoes NaPagina(int pagina)
    {
        if (pagina < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pagina), pagina, "A página começa em 1.");
        }

        return new CriteriosListagemObrigacoes
        {
            Pagina = pagina,
            Tamanho = Tamanho,
            Status = Status,
            Grupo = Grupo,
            Categoria = Categoria,
            De = De,
            Ate = Ate,
            IdPet = IdPet,
            OrdenarPor = OrdenarPor,
            Direcao = Direcao
        };
    }

    /// <summary>
    /// Os critérios de volta como parâmetros de consulta, na forma canônica.
    /// </summary>
    /// <remarks>
    /// Só os filtros presentes entram; paginação e ordenação entram sempre, para
    /// que o link seja explícito sobre a ordem em que a página foi cortada. Ler
    /// estes parâmetros de volta com <see cref="APartirDe"/> reproduz os mesmos
    /// critérios — é essa ida e volta que faz o link preservar a consulta.
    /// </remarks>
    public IReadOnlyDictionary<string, string> ParaParametrosDeConsulta()
    {
        var parametros = new Dictionary<string, string>
        {
            ["pagina"] = Pagina.ToString(CultureInfo.InvariantCulture),
            ["tamanho"] = Tamanho.ToString(CultureInfo.InvariantCulture)
        };

        if (Status is { } status)
        {
            parametros["status"] = status.Codigo();
        }

        if (Grupo is { } grupo)
        {
            parametros["grupo"] = TextoDoGrupo(grupo);
        }

        if (Categoria is not null)
        {
            parametros["categoria"] = Categoria;
        }

        if (De is { } de)
        {
            parametros["de"] = de.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (Ate is { } ate)
        {
            parametros["ate"] = ate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (IdPet is { } idPet)
        {
            parametros["pet"] = idPet.ToString(CultureInfo.InvariantCulture);
        }

        parametros["ordenarPor"] = CamposDeOrdenacao.First(c => c.Value == OrdenarPor).Key;
        parametros["direcao"] = Direcao == DirecaoOrdenacao.Desc ? "desc" : "asc";

        return parametros;
    }

    /// <summary>
    /// O valor do parâmetro <c>grupo</c> que filtra pelo braço dado. Exposto
    /// para quem monta link para a listagem a partir de outro recurso.
    /// </summary>
    public static string TextoDoGrupo(GrupoCoorte grupo) => grupo switch
    {
        GrupoCoorte.Tratado => GrupoTratamento,
        GrupoCoorte.Controle => GrupoControle,
        _ => throw new ArgumentOutOfRangeException(nameof(grupo), grupo, "Grupo sem texto de consulta.")
    };

    private static GrupoCoorte? InterpretarGrupo(string texto) => texto.Trim().ToLowerInvariant() switch
    {
        GrupoTratamento => GrupoCoorte.Tratado,
        GrupoControle => GrupoCoorte.Controle,
        _ => null
    };
}
