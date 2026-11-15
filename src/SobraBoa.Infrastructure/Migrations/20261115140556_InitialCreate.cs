using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SobraBoa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SB_COMPROVANTES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ReservaId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DoacaoId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CodigoVerificacao = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    PesoTotalKg = table.Column<decimal>(type: "DECIMAL(10,2)", precision: 10, scale: 2, nullable: false),
                    EmitidoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SB_COMPROVANTES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SB_HISTORICO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    DoacaoId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    StatusAnterior = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    StatusNovo = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    AlteradoPor = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    AlteradoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Observacao = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SB_HISTORICO", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SB_IDEMPOTENCIA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Chave = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Escopo = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    StatusCode = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CorpoResposta = table.Column<string>(type: "CLOB", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SB_IDEMPOTENCIA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SB_USUARIOS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Nome = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "NVARCHAR2(180)", maxLength: 180, nullable: false),
                    SenhaHash = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    Papel = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Aprovacao = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    NomeOrganizacao = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    Cnpj = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Cep = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Latitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    Longitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    PossuiRefrigeracao = table.Column<bool>(type: "BOOLEAN", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SB_USUARIOS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SB_DOACOES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    DoadorId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Titulo = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    JanelaInicio = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    JanelaFim = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    RequerRefrigeracao = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    InstrucoesRetirada = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Latitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    Longitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SB_DOACOES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SB_DOACOES_SB_USUARIOS_DoadorId",
                        column: x => x.DoadorId,
                        principalTable: "SB_USUARIOS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SB_ITENS_DOACAO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    DoacaoId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Descricao = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Quantidade = table.Column<decimal>(type: "DECIMAL(10,2)", precision: 10, scale: 2, nullable: false),
                    Unidade = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: false),
                    PesoEstimadoKg = table.Column<decimal>(type: "DECIMAL(10,2)", precision: 10, scale: 2, nullable: false),
                    DataValidade = table.Column<string>(type: "NVARCHAR2(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SB_ITENS_DOACAO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SB_ITENS_DOACAO_SB_DOACOES_DoacaoId",
                        column: x => x.DoacaoId,
                        principalTable: "SB_DOACOES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SB_RESERVAS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    DoacaoId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ReceptorId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CodigoRetirada = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SB_RESERVAS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SB_RESERVAS_SB_DOACOES_DoacaoId",
                        column: x => x.DoacaoId,
                        principalTable: "SB_DOACOES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SB_RESERVAS_SB_USUARIOS_ReceptorId",
                        column: x => x.ReceptorId,
                        principalTable: "SB_USUARIOS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SB_COMPROVANTES_CodigoVerificacao",
                table: "SB_COMPROVANTES",
                column: "CodigoVerificacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SB_COMPROVANTES_ReservaId",
                table: "SB_COMPROVANTES",
                column: "ReservaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SB_DOACOES_DoadorId",
                table: "SB_DOACOES",
                column: "DoadorId");

            migrationBuilder.CreateIndex(
                name: "IX_SB_DOACOES_Status_JanelaFim",
                table: "SB_DOACOES",
                columns: new[] { "Status", "JanelaFim" });

            migrationBuilder.CreateIndex(
                name: "IX_SB_HISTORICO_DoacaoId",
                table: "SB_HISTORICO",
                column: "DoacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SB_IDEMPOTENCIA_Chave_Escopo",
                table: "SB_IDEMPOTENCIA",
                columns: new[] { "Chave", "Escopo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SB_ITENS_DOACAO_DoacaoId",
                table: "SB_ITENS_DOACAO",
                column: "DoacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SB_RESERVAS_DoacaoId",
                table: "SB_RESERVAS",
                column: "DoacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SB_RESERVAS_ReceptorId",
                table: "SB_RESERVAS",
                column: "ReceptorId");

            migrationBuilder.CreateIndex(
                name: "IX_SB_USUARIOS_Email",
                table: "SB_USUARIOS",
                column: "Email",
                unique: true);
            migrationBuilder.Sql(@"
  CREATE UNIQUE INDEX UQ_RESERVA_EM_ANDAMENTO
  ON SB_RESERVAS (CASE WHEN ""Status"" IN ('Ativa','Retirada') THEN ""DoacaoId"" END)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SB_COMPROVANTES");

            migrationBuilder.DropTable(
                name: "SB_HISTORICO");

            migrationBuilder.DropTable(
                name: "SB_IDEMPOTENCIA");

            migrationBuilder.DropTable(
                name: "SB_ITENS_DOACAO");

            migrationBuilder.DropTable(
                name: "SB_RESERVAS");

            migrationBuilder.DropTable(
                name: "SB_DOACOES");

            migrationBuilder.DropTable(
                name: "SB_USUARIOS");
            
            migrationBuilder.Sql("DROP INDEX UQ_RESERVA_EM_ANDAMENTO");
        }
    }
}
