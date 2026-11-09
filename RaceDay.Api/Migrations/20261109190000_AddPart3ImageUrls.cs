using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RaceDay.Api.Data;

#nullable disable

namespace RaceDay.Api.Migrations;

[DbContext(typeof(RaceDayContext))]
[Migration("20261109190000_AddPart3ImageUrls")]
public partial class AddPart3ImageUrls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BannerImageUrl", table: "Events", type: "varchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ProfilePictureUrl", table: "Users", type: "varchar(1000)", maxLength: 1000, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BannerImageUrl", table: "Events");
        migrationBuilder.DropColumn(name: "ProfilePictureUrl", table: "Users");
    }
}
