using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CadastroClientesAPI.Migrations
{
    public partial class AlterarLogotipoParaByteArray : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                 name: "Logotipo",
                 table: "Clientes");

            migrationBuilder.AddColumn<byte[]>(
                name: "Logotipo",
                table: "Clientes",
                type: "varbinary(max)",
                nullable: true);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AlterColumn<string>(
            //    name: "Logotipo",
            //    table: "Clientes",
            //    type: "nvarchar(max)",
            //    nullable: true,
            //    oldClrType: typeof(byte[]),
            //    oldType: "varbinary(max)",
            //    oldNullable: true);
        }
    }
}
