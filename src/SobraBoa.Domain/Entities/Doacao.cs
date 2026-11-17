using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Domain.Entities;

public class Doacao
{
    public int Id { get; private set; }
    public int DoadorId { get; private set; }
    public string Titulo { get; private set; } = default!;
    public string? Descricao { get; private set; }
    public DateTime JanelaInicio { get; private set; } 
    public DateTime JanelaFim { get; private set; } 
    public bool RequerRefrigeracao { get; private set; }
    public string? InstrucoesRetirada { get; private set; }
    public double Latitude { get; private set; } 
    public double Longitude { get; private set; }
    public StatusDoacao Status { get; private set; } = StatusDoacao.Rascunho;
    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;
    public List<ItemDoacao> Itens { get; private set; } = [];

    public decimal PesoTotalKg => Itens.Sum(i => i.PesoEstimadoKg);

    private Doacao()
    {
    }

    public Doacao(int doadorId, string titulo, string? descricao, DateTime janelaInicio, DateTime janelaFim,
        bool requerRefrigeracao, string? instrucoes, double lat, double lng,
        IEnumerable<ItemDoacao> itens, DateTime agoraUtc)
    {
        if (string.IsNullOrWhiteSpace(titulo)) throw new DomainException("Título é obrigatório.");
        if (janelaFim <= janelaInicio)
            throw new DomainException("A janela de retirada deve terminar depois de começar.");
        if (janelaFim <= agoraUtc) throw new DomainException("A janela de retirada deve terminar no futuro.");
        var lista = itens.ToList();
        if (lista.Count == 0) throw new DomainException("Informe ao menos um item.");

        DoadorId = doadorId;
        Titulo = titulo.Trim();
        Descricao = descricao;
        JanelaInicio = janelaInicio;
        JanelaFim = janelaFim;
        RequerRefrigeracao = requerRefrigeracao;
        InstrucoesRetirada = instrucoes;
        Latitude = lat;
        Longitude = lng;
        Itens = lista;
    }
    
    
    public void Expirar(DateTime agoraUtc)
    {
        if (Status != StatusDoacao.Disponivel) throw new DomainException("Só doações disponíveis podem expirar.");
        if (agoraUtc < JanelaFim) throw new DomainException("A janela de retirada ainda está aberta.");
        Status = StatusDoacao.Expirada;
    }
    
    public void Publicar()
    {
        if (Status != StatusDoacao.Rascunho) throw new DomainException("Apenas rascunhos podem ser publicados.");
        Status = StatusDoacao.Disponivel;
    }

    public void Reservar(DateTime agoraUtc)
    {
        if (Status != StatusDoacao.Disponivel) throw new ConflictException("Doação já reservada ou indisponível.");
        if (agoraUtc < JanelaInicio || agoraUtc > JanelaFim) throw new DomainException("Fora da janela de retirada.");
        Status = StatusDoacao.Reservada;
    }

    public void LiberarReserva(DateTime agoraUtc) 
    {
        if (Status != StatusDoacao.Reservada) throw new DomainException("Doação não está reservada.");
        Status = agoraUtc < JanelaFim ? StatusDoacao.Disponivel : StatusDoacao.Expirada;
    }

    public void MarcarRetirada()
    {
        if (Status != StatusDoacao.Reservada) throw new DomainException("Doação não está reservada.");
        Status = StatusDoacao.Retirada;
    }

    public void Concluir()
    {
        if (Status != StatusDoacao.Retirada) throw new DomainException("A retirada ainda não foi confirmada.");
        Status = StatusDoacao.Concluida;
    }

    public void Cancelar()
    {
        if (Status is StatusDoacao.Retirada or StatusDoacao.Concluida or StatusDoacao.Expirada
            or StatusDoacao.Cancelada)
            throw new DomainException($"Doação em '{Status}' não pode ser cancelada.");
        Status = StatusDoacao.Cancelada;
    }

    public void AtualizarDados(string titulo, string? descricao, DateTime ini, DateTime fim, bool refrig,
        string? instr, DateTime agoraUtc)
    {
        if (Status is not (StatusDoacao.Rascunho or StatusDoacao.Disponivel))
            throw new DomainException("Só é possível editar rascunho ou doação disponível.");
        if (string.IsNullOrWhiteSpace(titulo)) throw new DomainException("Título é obrigatório.");
        if (fim <= ini) throw new DomainException("A janela de retirada é inválida.");
        if (fim <= agoraUtc) throw new DomainException("A janela de retirada deve terminar no futuro.");
        Titulo = titulo.Trim();
        Descricao = descricao;
        JanelaInicio = ini;
        JanelaFim = fim;
        RequerRefrigeracao = refrig;
        InstrucoesRetirada = instr;
    }
}