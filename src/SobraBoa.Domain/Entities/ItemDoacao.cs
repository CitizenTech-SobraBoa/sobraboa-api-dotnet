using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Domain.Entities;

public class ItemDoacao
{
    public int Id { get; private set; }
    public int DoacaoId { get; private set; }
    public string Descricao { get; private set; } = default!;
    public decimal Quantidade { get; private set; }
    public UnidadeMedida Unidade { get; private set; }
    public decimal PesoEstimadoKg { get; private set; }
    public DateOnly? DataValidade { get; private set; }

    private ItemDoacao()
    {
    }

    public ItemDoacao(string descricao, decimal quantidade, UnidadeMedida unidade, decimal pesoKg, DateOnly? validade)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("Descrição do item é obrigatória.");
        if (quantidade <= 0) throw new DomainException("Quantidade deve ser maior que zero.");
        if (pesoKg < 0) throw new DomainException("Peso não pode ser negativo.");
        Descricao = descricao;
        Quantidade = quantidade;
        Unidade = unidade;
        PesoEstimadoKg = pesoKg;
        DataValidade = validade;
    }
}