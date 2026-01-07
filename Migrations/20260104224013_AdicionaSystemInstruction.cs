using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJFACILITY.IA.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaSystemInstruction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SystemInstruction",
                table: "Agents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemInstruction",
                table: "Agents");
        }
    }
}
