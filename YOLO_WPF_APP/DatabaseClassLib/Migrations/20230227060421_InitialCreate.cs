using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseClassLib.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Datasets",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hashcode = table.Column<string>(type: "TEXT", nullable: false),
                    LabelsIndices = table.Column<byte[]>(type: "BLOB", nullable: false),
                    X1 = table.Column<byte[]>(type: "BLOB", nullable: false),
                    X2 = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Y1 = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Y2 = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datasets", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Imgs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hashcode = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imgs", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Datasets");

            migrationBuilder.DropTable(
                name: "Imgs");
        }
    }
}
