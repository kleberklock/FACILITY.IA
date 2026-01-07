using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PROJFACILITY.IA.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Messages");

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
    }
}
