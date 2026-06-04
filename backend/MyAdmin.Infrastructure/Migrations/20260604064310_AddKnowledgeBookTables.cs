using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAdmin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBookTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SysBook",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Isbn = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysBook", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysBorrowLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    BookTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BorrowTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ReturnTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualReturnTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LogStatus = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysBorrowLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysBorrowLog_SysBook_BookId",
                        column: x => x.BookId,
                        principalTable: "SysBook",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SysBorrowLog_SysUser_UserId",
                        column: x => x.UserId,
                        principalTable: "SysUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SysBook_Isbn",
                table: "SysBook",
                column: "Isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysBorrowLog_BookId",
                table: "SysBorrowLog",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_SysBorrowLog_LogStatus",
                table: "SysBorrowLog",
                column: "LogStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SysBorrowLog_UserId",
                table: "SysBorrowLog",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SysBorrowLog");

            migrationBuilder.DropTable(
                name: "SysBook");
        }
    }
}
