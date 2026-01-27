using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AugmentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedDate", "Email", "UpdatedDate" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000100"), new DateTime(2026, 1, 27, 8, 32, 54, 0, DateTimeKind.Utc), "akashnagar47@outlook.com", null });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "CreatedDate", "RoleId", "UpdatedDate", "UserId" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000200"), new DateTime(2026, 1, 27, 8, 32, 54, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000003"), null, new Guid("00000000-0000-0000-0000-000000000100") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000200"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: new Guid("00000000-0000-0000-0000-000000000100"));
        }
    }
}
