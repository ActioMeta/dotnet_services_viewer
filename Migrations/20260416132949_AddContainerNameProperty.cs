using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_services_viewer.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerNameProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContainerName",
                table: "Containers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainerName",
                table: "Containers");
        }
    }
}
