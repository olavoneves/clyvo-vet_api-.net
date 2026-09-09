using Clyvo.Insights.Application.Abstracoes;
using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Application.Projecoes;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Infrastructure.Mongo;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Clyvo.Insights.Tests.Integration.Infraestrutura;

/// <summary>
/// Escrita real no MongoDB, pelo mesmo caminho de DI que a API usa.
/// </summary>
/// <remarks>
/// Existe por um defeito concreto que nenhum dublê pegaria: o driver serializa
/// <c>decimal</c> como <b>string</b> por padrão, e sobre string "9.90" é maior
/// que "25.07". A série de snapshots existe justamente para ser comparada ao
/// longo do tempo, então o defeito só apareceria no dia em que alguém ordenasse
/// a série — e apareceria como número errado, não como erro.
/// </remarks>
[Collection(ColecaoDeInfraestruturaReal.Nome)]
public class EscritaNoMongoTests
{
    private const long ClinicaDeTeste = 999_002;

    private readonly InfraestruturaRealFixture _infra;

    public EscritaNoMongoTests(InfraestruturaRealFixture infra)
    {
        _infra = infra;
    }

    [RequerInfraestrutura]
    public async Task RegistrarSnapshotAsync_NoMongo_GravaDecimalComoDecimal128()
    {
        // Arrange
        await using var provedor = _infra.CriarProvedor();
        var colecao = provedor.GetRequiredService<IMongoCollection<DocumentoSnapshotCoorte>>();
        var brutos = colecao.Database.GetCollection<BsonDocument>(colecao.CollectionNamespace.CollectionName);

        var id = $"{ClinicaDeTeste}:{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}";
        var documento = new DocumentoSnapshotCoorte
        {
            Id = id,
            IdClinica = ClinicaDeTeste,
            DataReferencia = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            ApuradoEmUtc = DateTime.UtcNow,
            Analise = AnaliseDeTeste()
        };

        try
        {
            // Act
            await colecao.ReplaceOneAsync(
                d => d.Id == id, documento, new ReplaceOptions { IsUpsert = true });

            var bruto = await brutos.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(bruto);

            // camelCase: o campo tem a mesma grafia do JSON da API. Sem a
            // convenção registrada, nasceria "Analise" e quem consultasse o
            // Mongo precisaria saber que existem duas grafias do mesmo campo.
            var analise = bruto!["analise"].AsBsonDocument;

            Assert.Equal(BsonType.Decimal128, analise["deltaPontosPercentuais"].BsonType);
            Assert.Equal(BsonType.Decimal128, analise["receitaRecuperada"].BsonType);
            Assert.Equal(BsonType.Decimal128, analise["tratado"]["taxaCumprimentoPercentual"].BsonType);

            // Decimal128 de verdade, e não string que parece número. O
            // intervalo tem os dois lados de propósito: na ordenação de tipos do
            // MongoDB, string vem depois de número, então um campo textual
            // satisfaria o `$gt` por acidente e reprovaria no `$lt`. Só um
            // número cai dentro dos dois.
            var dentroDoIntervalo = await brutos.Find(Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("_id", id),
                    Builders<BsonDocument>.Filter.Gt("analise.deltaPontosPercentuais", 19),
                    Builders<BsonDocument>.Filter.Lt("analise.deltaPontosPercentuais", 21)))
                .AnyAsync();

            Assert.True(dentroDoIntervalo,
                "O delta gravado é 20,00 e a consulta por 19 < x < 21 não o encontrou — " +
                "sinal de que o valor foi para o banco como texto.");
        }
        finally
        {
            await brutos.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id));
        }
    }

    [RequerInfraestrutura]
    public async Task RegistrarConsultaAsync_NoMongo_AcrescentaLinhaAoLogDeConsulta()
    {
        // Arrange
        await using var provedor = _infra.CriarProvedor();
        var repositorio = provedor.GetRequiredService<IProjecaoRepository>();
        var colecao = provedor.GetRequiredService<IMongoCollection<DocumentoConsultaIndicador>>();

        var filtro = Builders<DocumentoConsultaIndicador>.Filter.Eq(d => d.IdClinica, ClinicaDeTeste);
        var correlacao = Guid.NewGuid().ToString("N");

        try
        {
            // Act
            await repositorio.RegistrarConsultaAsync(new RegistroDeConsulta(
                ClinicaDeTeste, "analise_coorte", DateTime.UtcNow, Disponivel: true, correlacao));

            var gravado = await colecao.Find(filtro).FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(gravado);
            Assert.Equal("analise_coorte", gravado!.Indicador);
            Assert.True(gravado.Disponivel);
            Assert.Equal(correlacao, gravado.TraceId);
            Assert.Equal(DateTimeKind.Utc, gravado.InstanteUtc.Kind);
        }
        finally
        {
            await colecao.DeleteManyAsync(filtro);
        }
    }

    /// <summary>
    /// Delta de 20 p.p. redondos: 60% contra 40%. Valor escolhido para o
    /// intervalo 19 &lt; x &lt; 21 ter resposta previsível.
    /// </summary>
    private static AnaliseCoorteDto AnaliseDeTeste()
    {
        var analise = AnaliseCoorte.Calcular(
            Coorte.Criar(GrupoCoorte.Tratado, 1000, 600, 100),
            Coorte.Criar(GrupoCoorte.Controle, 100, 40, 10),
            ticketMedio: 200m);

        return new AnaliseCoorteDto(
            ClinicaDeTeste,
            analise.Disponivel,
            analise.MotivoIndisponibilidade,
            analise.DeltaPontosPercentuais,
            analise.ConsultasAtribuiveis,
            analise.ReceitaRecuperada,
            analise.ReceitaEstimavel,
            analise.TicketMedio,
            new CoorteDto("TRATADO", 1000, 600, 60.00m, 100),
            new CoorteDto("CONTROLE", 100, 40, 40.00m, 10),
            Array.Empty<MetaAvaliadaDto>());
    }
}
