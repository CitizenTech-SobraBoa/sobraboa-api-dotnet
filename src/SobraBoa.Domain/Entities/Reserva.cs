using System.Security.Cryptography;
using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Domain.Entities;

public class Reserva
{
    public int Id { get; private set; }
    public int DoacaoId { get; private set; }
    public int ReceptorId { get; private set; }
    public StatusReserva Status { get; private set; } = StatusReserva.Ativa;
    public string CodigoRetirada { get; private set; } = default!;
    public DateTime CriadaEm { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    private Reserva()
    {
    }

    public static Reserva Criar(Doacao doacao, int receptorId, DateTime agoraUtc, int minutosValidade = 120)
    {
        var expira = agoraUtc.AddMinutes(minutosValidade);
        if (expira > doacao.JanelaFim) expira = doacao.JanelaFim; 
        return new Reserva
        {
            DoacaoId = doacao.Id, ReceptorId = receptorId, CriadaEm = agoraUtc, ExpiraEm = expira,
            CodigoRetirada = GerarCodigo()
        };
    }

    private static string GerarCodigo()
    {
        const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return string.Create(6, alfabeto, (span, a) =>
        {
            for (var i = 0; i < span.Length; i++) span[i] = a[RandomNumberGenerator.GetInt32(a.Length)];
        });
    }

    public void ConfirmarRetirada(string codigo)
    {
        if (Status != StatusReserva.Ativa) throw new DomainException("Reserva não está ativa.");
        if (!string.Equals(codigo?.Trim(), CodigoRetirada, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Código de retirada inválido.");
        Status = StatusReserva.Retirada;
    }

    public void Concluir()
    {
        if (Status != StatusReserva.Retirada) throw new DomainException("A retirada ainda não foi confirmada.");
        Status = StatusReserva.Concluida;
    }

    public void Cancelar(string? motivo)
    {
        if (Status != StatusReserva.Ativa) throw new DomainException("Só reservas ativas podem ser canceladas.");
        Status = StatusReserva.Cancelada;
        MotivoCancelamento = motivo;
    }
}

public class Comprovante
{
    public int Id { get; private set; }
    public int ReservaId { get; private set; }
    public int DoacaoId { get; private set; }
    public string CodigoVerificacao { get; private set; } = default!;
    public decimal PesoTotalKg { get; private set; }
    public DateTime EmitidoEm { get; private set; }

    private Comprovante()
    {
    }

    public Comprovante(int reservaId, int doacaoId, decimal pesoKg, DateTime agoraUtc)
    {
        ReservaId = reservaId;
        DoacaoId = doacaoId;
        PesoTotalKg = pesoKg;
        EmitidoEm = agoraUtc;
        CodigoVerificacao = "SB-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
    }
}