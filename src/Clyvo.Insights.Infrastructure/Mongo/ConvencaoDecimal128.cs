using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Clyvo.Insights.Infrastructure.Mongo;

/// <summary>
/// Grava <c>decimal</c> como Decimal128, e não como texto.
/// </summary>
/// <remarks>
/// <para>
/// O padrão do driver serializa decimal como string para não perder precisão.
/// Funciona para guardar, mas a série de snapshots existe para ser comparada ao
/// longo do tempo, e sobre string "9.90" é maior que "25.07". Decimal128 mantém
/// a precisão exata e continua sendo número para <c>$gt</c>, <c>$sort</c> e
/// agregação.
/// </para>
/// <para>
/// Feito como convenção de membro, e não com
/// <c>BsonSerializer.RegisterSerializer</c>: o registro global só funciona antes
/// da primeira consulta ao serializador de decimal, e uma consulta acidental
/// mais cedo no processo o faz estourar com "there is already a different
/// serializer registered". A convenção é aplicada quando cada classe é mapeada,
/// e não depende de ordem de inicialização.
/// </para>
/// </remarks>
internal sealed class ConvencaoDecimal128 : ConventionBase, IMemberMapConvention
{
    private static readonly DecimalSerializer Decimal128 = new(BsonType.Decimal128);

    public void Apply(BsonMemberMap memberMap)
    {
        if (memberMap.MemberType == typeof(decimal))
        {
            memberMap.SetSerializer(Decimal128);
        }
        else if (memberMap.MemberType == typeof(decimal?))
        {
            memberMap.SetSerializer(new NullableSerializer<decimal>(Decimal128));
        }
    }
}
