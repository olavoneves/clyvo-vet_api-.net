namespace Clyvo.Insights.Application.Abstracoes;

/// <summary>
/// Origem única do tenant numa requisição autenticada.
/// </summary>
/// <remarks>
/// Os casos de uso não têm parâmetro de clínica de propósito. Se o id viesse
/// por argumento, algum controller acabaria preenchendo-o com valor vindo do
/// cliente e um tenant leria os dados de outro — é o tipo de falha que um
/// avaliador procura de propósito. Aqui o id só pode sair do token.
/// </remarks>
public interface ITenantContext
{
    /// <summary>Clínica do claim <c>idClinica</c> emitido pelo clyvo-core.</summary>
    long IdClinica { get; }
}
