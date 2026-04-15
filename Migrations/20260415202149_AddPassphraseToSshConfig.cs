using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_services_viewer.Migrations
{
    /// <inheritdoc />
    public partial class AddPassphraseToSshConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SshConfig_Passphrase",
                table: "Nodes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SshConfig_Passphrase",
                table: "Nodes");
        }
    }
}
