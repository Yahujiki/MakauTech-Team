using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MakauTech.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWismaSanyan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO Categories (Name, Icon, CreatedAt)
SELECT 'City', '🏙️', CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Name = 'City');
");

            migrationBuilder.Sql(@"
INSERT INTO Places (Name, Location, Description, ImageUrl, Rating, VisitCount, CategoryId, CreatedAt)
SELECT
  'Wisma Sanyan',
  'Sibu',
  'A landmark riverside complex in Sibu with offices, shops and views near the Rajang River — a classic city stop for photos, errands and exploring the town centre.',
  '/images/wisma-sanyan.svg',
  0.0,
  0,
  (SELECT Id FROM Categories WHERE Name = 'City' LIMIT 1),
  CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM Places WHERE Name = 'Wisma Sanyan' AND Location = 'Sibu');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Places WHERE Name = 'Wisma Sanyan' AND Location = 'Sibu';");
        }
    }
}
