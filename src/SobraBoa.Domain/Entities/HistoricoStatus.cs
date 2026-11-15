using SobraBoa.Domain.Enums;

namespace SobraBoa.Domain.Entities;

public class HistoricoStatus
{
    public int Id { get; private set; }
    public int DoacaoId { get; private set; }
    public StatusDoacao? StatusAnterior { get; private set; }
    public StatusDoacao StatusNovo { get; private set; }
    public int? AlteradoPor { get; private set; }
    public DateTime AlteradoEm { get; private set; }
    public string? Observacao { get; private set; }

    private HistoricoStatus()
    {
    }

    public HistoricoStatus(int doacaoId, StatusDoacao? anterior, StatusDoacao novo, int? usuarioId, string? obs,
        DateTime agora)
    {
        DoacaoId = doacaoId;
        StatusAnterior = anterior;
        StatusNovo = novo;
        AlteradoPor = usuarioId;
        Observacao = obs;
        AlteradoEm = agora;
    }
}