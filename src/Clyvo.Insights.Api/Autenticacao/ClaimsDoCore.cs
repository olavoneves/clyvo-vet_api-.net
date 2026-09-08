namespace Clyvo.Insights.Api.Autenticacao;

/// <summary>Claims que o clyvo-core coloca no access token.</summary>
public static class ClaimsDoCore
{
    /// <summary>
    /// Tenant. Única origem do id da clínica numa requisição autenticada —
    /// o mesmo nome que o core usa em <c>JwtService.CLAIM_ID_CLINICA</c>.
    /// </summary>
    public const string IdClinica = "idClinica";

    /// <summary>TUTOR, VETERINARIO ou COLABORADOR.</summary>
    public const string Tipo = "tipo";
}
