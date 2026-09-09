using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Domain.Metas;
using Moq;

namespace Clyvo.Insights.Tests.Unit.Aplicacao.Metas;

/// <summary>
/// Arranjo comum aos quatro casos de uso de meta: repositório e tenant
/// dublados com Moq.
/// </summary>
/// <remarks>
/// <para>
/// Classe base, e não fixture: os dublês precisam nascer limpos a cada teste,
/// e o xUnit já constrói uma instância nova da classe de teste por método. Uma
/// <c>IClassFixture</c> aqui faria o oposto do que se quer — compartilharia o
/// mock entre testes e deixaria a verificação de chamadas depender da ordem de
/// execução.
/// </para>
/// <para>
/// O que estes testes verificam é orquestração: o recorte por tenant, a ordem
/// das chamadas e o que chega ao DTO. A regra em si já é coberta nos testes de
/// domínio, e recalculá-la aqui seria uma segunda implementação dentro da
/// própria suíte.
/// </para>
/// </remarks>
public abstract class TesteDeCasoDeUsoDeMeta
{
    protected const long IdClinicaDoToken = 23;
    protected const long IdClinicaVizinha = 24;

    protected readonly Mock<IMetaIndicadorRepository> Repositorio = new();
    protected readonly Mock<ITenantContext> Tenant = new();

    protected TesteDeCasoDeUsoDeMeta()
    {
        Tenant.SetupGet(t => t.IdClinica).Returns(IdClinicaDoToken);
    }

    protected static MetaIndicador Meta(long idClinica, IndicadorMonitorado indicador, decimal limiar) =>
        MetaIndicador.Criar(idClinica, indicador, limiar, DateTime.UtcNow);
}
