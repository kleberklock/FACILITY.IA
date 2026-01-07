using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PROJFACILITY.IA.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialtyToAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Area",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "SystemPrompt",
                table: "Agents",
                newName: "Specialty");

            migrationBuilder.InsertData(
                table: "Agents",
                columns: new[] { "Id", "Name", "Specialty" },
                values: new object[,]
                {
                    { 1, "Advogado Civil", "Direito Civil" },
                    { 2, "Médico Clínico", "Saúde" },
                    { 3, "Dev. FullStack", "Tecnologia" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.RenameColumn(
                name: "Specialty",
                table: "Agents",
                newName: "SystemPrompt");

            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "Agents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
