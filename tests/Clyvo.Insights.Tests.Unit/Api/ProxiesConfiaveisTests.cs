using System.Net;
using Clyvo.Insights.Api.Hipermidia;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace Clyvo.Insights.Tests.Unit.Api;

public class ProxiesConfiaveisTests
{
    [Fact]
    public void Configurar_SemListaEmDesenvolvimento_ConfiaEmQualquerOrigem()
    {
        // Arrange
        var opcoes = new ForwardedHeadersOptions();

        // Act
        ProxiesConfiaveis.Configurar(opcoes, Array.Empty<string>(), ambienteDeDesenvolvimento: true);

        // Assert
        Assert.Empty(opcoes.KnownProxies);
        Assert.Empty(opcoes.KnownNetworks);
        Assert.True(opcoes.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedHost));
    }

    [Fact]
    public void Configurar_SemListaForaDeDesenvolvimento_MantemSoOLoopback()
    {
        // Arrange
        var opcoes = new ForwardedHeadersOptions();

        // Act
        ProxiesConfiaveis.Configurar(opcoes, Array.Empty<string>(), ambienteDeDesenvolvimento: false);

        // Assert — o padrão do ASP.NET: só 127.0.0.1/8 e ::1
        Assert.Contains(IPAddress.IPv6Loopback, opcoes.KnownProxies);
        Assert.Single(opcoes.KnownNetworks);
    }

    [Fact]
    public void Configurar_ComIpERede_ConfiaSoNelesEmQualquerAmbiente()
    {
        // Arrange
        var opcoes = new ForwardedHeadersOptions();

        // Act
        ProxiesConfiaveis.Configurar(opcoes, new[] { "10.0.0.5", " 172.18.0.0/16 " }, ambienteDeDesenvolvimento: false);

        // Assert — a lista substitui o loopback padrão
        Assert.Equal(new[] { IPAddress.Parse("10.0.0.5") }, opcoes.KnownProxies);
        var rede = Assert.Single(opcoes.KnownNetworks);
        Assert.Equal(IPAddress.Parse("172.18.0.0"), rede.Prefix);
        Assert.Equal(16, rede.PrefixLength);
    }

    [Theory]
    [InlineData("proxy.interno")]
    [InlineData("10.0.0.0/oito")]
    [InlineData("10.0.0.0/8/1")]
    public void Configurar_ComEntradaInvalida_FalhaDizendoQualChaveCorrigir(string entrada)
    {
        // Arrange
        var opcoes = new ForwardedHeadersOptions();

        // Act
        var excecao = Assert.Throws<InvalidOperationException>(
            () => ProxiesConfiaveis.Configurar(opcoes, new[] { entrada }, ambienteDeDesenvolvimento: false));

        // Assert
        Assert.Contains(ProxiesConfiaveis.Secao, excecao.Message);
    }
}
