using System.Globalization;
using Clyvo.Insights.Application.Abstracoes;

namespace Clyvo.Insights.Api.Autenticacao;

/// <summary>
/// Resolve o tenant a partir do token, e só dele.
/// </summary>
/// <remarks>
/// Não há caminho para o id da clínica entrar por rota, query string ou corpo:
/// nem os casos de uso nem os controllers aceitam esse parâmetro. Tenant que
/// pode ser escolhido pelo cliente é vazamento entre clínicas.
/// </remarks>
internal sealed class TenantContextHttp : ITenantContext
{
    private readonly IHttpContextAccessor _acessor;

    public TenantContextHttp(IHttpContextAccessor acessor)
    {
        _acessor = acessor;
    }

    public long IdClinica
    {
        get
        {
            var claim = _acessor.HttpContext?.User.FindFirst(ClaimsDoCore.IdClinica)?.Value;

            // A policy de autorização já exige o claim, então chegar aqui sem
            // ele significa que a policy foi desligada por engano.
            if (!long.TryParse(claim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idClinica))
            {
                throw new InvalidOperationException(
                    $"Requisição autenticada sem o claim '{ClaimsDoCore.IdClinica}' utilizável (valor: '{claim}').");
            }

            return idClinica;
        }
    }
}
