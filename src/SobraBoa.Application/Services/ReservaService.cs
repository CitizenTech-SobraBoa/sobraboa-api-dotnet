using SobraBoa.Application.Dtos;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Application.Services;

public class ReservaService(
    IReservaRepository reservas,
    IDoacaoRepository doacoes,
    IUsuarioRepository usuarios,
    IUnitOfWork uow,
    TimeProvider clock)
{
    public async Task<ReservaResponse> ReservarAsync(int receptorId, int doacaoId, CancellationToken ct = default)
    {
        var receptor = await usuarios.ObterPorIdAsync(receptorId, ct) ??
                       throw new NotFoundException("Usuário não encontrado.");
        if (receptor.Papel != Papel.Receptor) throw new ForbiddenException("Apenas receptores podem reservar.");
        if (!receptor.EstaAprovado) throw new ForbiddenException("Cadastro ainda não aprovado.");

        var doacao = await doacoes.ObterPorIdAsync(doacaoId, ct) ??
                     throw new NotFoundException("Doação não encontrada.");
        if (doacao.RequerRefrigeracao && !receptor.PossuiRefrigeracao)
            throw new DomainException("Esta doação exige refrigeração e o receptor não possui.");

        var agora = clock.GetUtcNow().UtcDateTime;
        var anterior = doacao.Status;
        doacao.Reservar(agora); // valida status e janela
        var reserva = Reserva.Criar(doacao, receptorId, agora);
        await reservas.AdicionarAsync(reserva, ct);
        await doacoes.AdicionarHistoricoAsync(
            new HistoricoStatus(doacao.Id, anterior, doacao.Status, receptorId, "Reservada pelo receptor", agora), ct);

        // Se dois receptores chegarem juntos, o índice único do banco (etapa 5.3) faz o 2º falhar.
        // O UnitOfWork traduz a violação em ConflictException -> HTTP 409.
        await uow.SaveChangesAsync(ct);
        return new(reserva.Id, doacao.Id, reserva.Status.ToString(), reserva.CodigoRetirada, reserva.ExpiraEm);
    }

    public async Task<ReservaResponse> ConfirmarRetiradaAsync(int doadorId, int reservaId, string codigo,
        CancellationToken ct = default)
    {
        var (reserva, doacao) = await CarregarAsync(reservaId, ct);
        if (doacao.DoadorId != doadorId)
            throw new ForbiddenException("Apenas o doador desta doação pode confirmar a retirada.");

        var agora = clock.GetUtcNow().UtcDateTime;
        reserva.ConfirmarRetirada(codigo, agora);
        var anterior = doacao.Status;
        doacao.MarcarRetirada();
        await doacoes.AdicionarHistoricoAsync(
            new HistoricoStatus(doacao.Id, anterior, doacao.Status, doadorId, "Retirada confirmada", agora), ct);
        await uow.SaveChangesAsync(ct);
        return new(reserva.Id, doacao.Id, reserva.Status.ToString(), null, reserva.ExpiraEm);
    }

    public async Task<IReadOnlyList<ReservaResponse>> MinhasAsync(int receptorId, CancellationToken ct = default)
    {
        var lista = await reservas.ListarDoReceptorAsync(receptorId, ct);
        return lista.Select(r => new ReservaResponse(
            r.Id, r.DoacaoId, r.Status.ToString(),
            r.Status == StatusReserva.Ativa ? r.CodigoRetirada : null, // código só enquanto está ativa
            r.ExpiraEm)).ToList();
    }
    
    public async Task<ComprovanteResponse> ReceberAsync(int receptorId, int reservaId, CancellationToken ct = default)
    {
        var (reserva, doacao) = await CarregarAsync(reservaId, ct);
        if (reserva.ReceptorId != receptorId) throw new ForbiddenException("Esta reserva pertence a outro receptor.");

        var agora = clock.GetUtcNow().UtcDateTime;
        reserva.Concluir();
        var anterior = doacao.Status;
        doacao.Concluir();
        var comp = new Comprovante(reserva.Id, doacao.Id, doacao.PesoTotalKg, agora);
        await reservas.AdicionarComprovanteAsync(comp, ct);
        await doacoes.AdicionarHistoricoAsync(
            new HistoricoStatus(doacao.Id, anterior, doacao.Status, receptorId, "Recebimento confirmado", agora), ct);
        await uow.SaveChangesAsync(ct);
        return new(comp.CodigoVerificacao, comp.PesoTotalKg, comp.EmitidoEm, doacao.Id, reserva.Id);
    }

    public async Task CancelarAsync(int receptorId, int reservaId, string? motivo, CancellationToken ct = default)
    {
        var (reserva, doacao) = await CarregarAsync(reservaId, ct);
        if (reserva.ReceptorId != receptorId) throw new ForbiddenException("Esta reserva pertence a outro receptor.");

        var agora = clock.GetUtcNow().UtcDateTime;
        reserva.Cancelar(motivo);
        var anterior = doacao.Status;
        doacao.LiberarReserva(agora); // volta a Disponivel se a janela ainda está aberta
        await doacoes.AdicionarHistoricoAsync(
            new HistoricoStatus(doacao.Id, anterior, doacao.Status, receptorId, $"Reserva cancelada: {motivo}", agora),
            ct);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<ComprovanteResponse> ObterComprovanteAsync(int usuarioId, bool ehAdmin, int reservaId,
        CancellationToken ct = default)
    {
        var (reserva, doacao) = await CarregarAsync(reservaId, ct);
        if (!ehAdmin && reserva.ReceptorId != usuarioId && doacao.DoadorId != usuarioId)
            throw new ForbiddenException("Sem acesso a este comprovante.");
        var c = await reservas.ObterComprovantePorReservaAsync(reservaId, ct) ??
                throw new NotFoundException("Comprovante ainda não emitido.");
        return new(c.CodigoVerificacao, c.PesoTotalKg, c.EmitidoEm, c.DoacaoId, c.ReservaId);
    }

    private async Task<(Reserva, Doacao)> CarregarAsync(int reservaId, CancellationToken ct)
    {
        var reserva = await reservas.ObterPorIdAsync(reservaId, ct) ??
                      throw new NotFoundException("Reserva não encontrada.");
        var doacao = await doacoes.ObterPorIdAsync(reserva.DoacaoId, ct) ??
                     throw new NotFoundException("Doação não encontrada.");
        return (reserva, doacao);
    }
}