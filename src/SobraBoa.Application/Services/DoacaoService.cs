using SobraBoa.Application.Dtos;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Application.Services;

public class DoacaoService(
    IDoacaoRepository doacoes,
    IReservaRepository reservas,
    IUsuarioRepository usuarios,
    IUnitOfWork uow,
    TimeProvider clock)
{
    public async Task<DoacaoResponse> CriarAsync(int doadorId, DoacaoRequest r, CancellationToken ct = default)
    {
        var doador = await ObterDoadorAprovadoAsync(doadorId, ct);
        var agora = clock.GetUtcNow().UtcDateTime;
        var itens = r.Itens.Select(i =>
            new ItemDoacao(i.Descricao, i.Quantidade, i.Unidade, i.PesoEstimadoKg, i.DataValidade));
        var d = new Doacao(doadorId, r.Titulo, r.Descricao, r.JanelaInicio.ToUniversalTime(),
            r.JanelaFim.ToUniversalTime(),
            r.RequerRefrigeracao, r.InstrucoesRetirada, doador.Latitude, doador.Longitude, itens, agora);
        await doacoes.AdicionarAsync(d, ct);
        await uow.SaveChangesAsync(ct); // gera o Id
        await doacoes.AdicionarHistoricoAsync(
            new HistoricoStatus(d.Id, null, d.Status, doadorId, "Rascunho criado", agora), ct);
        await uow.SaveChangesAsync(ct);
        return Mapear(d);
    }

    public async Task<DoacaoResponse> PublicarAsync(int doadorId, int doacaoId, CancellationToken ct = default)
    {
        await ObterDoadorAprovadoAsync(doadorId, ct);
        var d = await ObterDoDoadorAsync(doadorId, doacaoId, ct);
        var anterior = d.Status;
        d.Publicar();
        await doacoes.AdicionarHistoricoAsync(
            new HistoricoStatus(d.Id, anterior, d.Status, doadorId, "Doação publicada", clock.GetUtcNow().UtcDateTime),
            ct);
        await uow.SaveChangesAsync(ct);
        return Mapear(d);
    }

    public async Task CancelarAsync(int doadorId, int doacaoId, CancellationToken ct = default)
    {
        var d = await ObterDoDoadorAsync(doadorId, doacaoId, ct);
        var anterior = d.Status;
        d.Cancelar(); // lança DomainException se o estado não permitir; só depois mexemos na reserva

        // reserva em andamento (se houver) é encerrada junto, liberando o índice único e o código de retirada
        var reservaAtiva = await reservas.ObterAtivaPorDoacaoAsync(doacaoId, ct);
        reservaAtiva?.Cancelar("Doação cancelada pelo doador");
        await doacoes.AdicionarHistoricoAsync(
            new HistoricoStatus(d.Id, anterior, d.Status, doadorId, "Cancelada pelo doador",
                clock.GetUtcNow().UtcDateTime), ct);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<DoacaoResponse> AtualizarAsync(int doadorId, int doacaoId, DoacaoRequest r,
        CancellationToken ct = default)
    {
        await ObterDoadorAprovadoAsync(doadorId, ct);
        var d = await ObterDoDoadorAsync(doadorId, doacaoId, ct);
        d.AtualizarDados(r.Titulo, r.Descricao, r.JanelaInicio.ToUniversalTime(), r.JanelaFim.ToUniversalTime(),
            r.RequerRefrigeracao, r.InstrucoesRetirada, clock.GetUtcNow().UtcDateTime);
        await uow.SaveChangesAsync(ct);
        return Mapear(d);
    }

    public async Task<PagedResult<DoacaoResponse>> MinhasAsync(int doadorId, int page, int pageSize,
        CancellationToken ct = default)
    {
        (page, pageSize) = Normalizar(page, pageSize);
        var (itens, total) = await doacoes.ListarDoDoadorAsync(doadorId, page, pageSize, ct);
        return new(itens.Select(i => Mapear(i)).ToList(), page, pageSize, total);
    }

    public async Task<DoacaoResponse> ObterAsync(int id, int usuarioId, bool ehAdmin, CancellationToken ct = default)
    {
        var d = await doacoes.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Doação não encontrada.");

        // rascunho e cancelada só interessam ao dono (ou admin). 404 em vez de 403 para não revelar que existe.
        var restrita = d.Status is StatusDoacao.Rascunho or StatusDoacao.Cancelada;
        if (restrita && !ehAdmin && d.DoadorId != usuarioId)
            throw new NotFoundException("Doação não encontrada.");

        return Mapear(d);
    }

    public async Task<PagedResult<DoacaoResponse>> ProximasAsync(int receptorId, double lat, double lng, double raioKm,
        int page, int pageSize, CancellationToken ct = default)
    {
        await ObterReceptorAprovadoAsync(receptorId, ct);
        if (raioKm is <= 0 or > 50) throw new ArgumentException("raioKm deve estar entre 0 e 50.");
        (page, pageSize) = Normalizar(page, pageSize);

        // 1) o banco filtra por "caixa" (barato) + status + janela; 2) refinamos com Haversine em memória
        var dLat = raioKm / 111.0;
        var dLng = raioKm / (111.0 * Math.Cos(lat * Math.PI / 180));
        var candidatas = await doacoes.ListarDisponiveisAsync(clock.GetUtcNow().UtcDateTime, lat - dLat, lat + dLat,
            lng - dLng, lng + dLng, ct);

        var dentro = candidatas
            .Select(d => (d, km: Haversine(lat, lng, d.Latitude, d.Longitude)))
            .Where(x => x.km <= raioKm)
            .OrderBy(x => x.d.JanelaFim) // as que encerram primeiro
            .ToList();

        var pagina = dentro.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => Mapear(x.d, Math.Round(x.km, 2))).ToList();
        return new(pagina, page, pageSize, dentro.Count);
    }

    // ---------- helpers ----------
    private async Task<Usuario> ObterDoadorAprovadoAsync(int id, CancellationToken ct)
    {
        var u = await usuarios.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Usuário não encontrado.");
        if (u.Papel != Papel.Doador) throw new ForbiddenException("Apenas doadores podem realizar esta ação.");
        if (!u.EstaAprovado) throw new ForbiddenException("Cadastro ainda não aprovado pelo administrador.");
        return u;
    }

    private async Task ObterReceptorAprovadoAsync(int id, CancellationToken ct)
    {
        var u = await usuarios.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Usuário não encontrado.");
        if (u.Papel != Papel.Receptor) throw new ForbiddenException("Apenas receptores podem realizar esta ação.");
        if (!u.EstaAprovado) throw new ForbiddenException("Cadastro ainda não aprovado pelo administrador.");
    }

    private async Task<Doacao> ObterDoDoadorAsync(int doadorId, int doacaoId, CancellationToken ct)
    {
        var d = await doacoes.ObterPorIdAsync(doacaoId, ct) ?? throw new NotFoundException("Doação não encontrada.");
        if (d.DoadorId != doadorId) throw new ForbiddenException("Esta doação pertence a outro doador.");
        return d;
    }

    private static (int, int) Normalizar(int page, int size) => (Math.Max(1, page), Math.Clamp(size, 1, 50));

    public static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double Rad(double g) => g * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public static DoacaoResponse Mapear(Doacao d, double? km = null) => new(d.Id, d.Titulo, d.Descricao,
        d.Status.ToString(),
        d.JanelaInicio, d.JanelaFim, d.RequerRefrigeracao, d.InstrucoesRetirada, d.PesoTotalKg, km,
        d.Itens.Select(i =>
                new ItemResponse(i.Id, i.Descricao, i.Quantidade, i.Unidade.ToString(), i.PesoEstimadoKg,
                    i.DataValidade))
            .ToList());
}