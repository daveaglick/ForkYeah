namespace ForkYeah.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Repositories",
                c => new
                    {
                        Owner = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 128),
                        DbAdded = c.DateTimeOffset(nullable: false, precision: 7),
                        OriginialStargazersCount = c.Int(nullable: false),
                        Description = c.String(nullable: false),
                        HtmlUrl = c.String(nullable: false),
                        Homepage = c.String(),
                        Language = c.String(),
                        StargazersCount = c.Int(nullable: false),
                        OpenIssuesCount = c.Int(nullable: false),
                        CreatedAt = c.DateTimeOffset(precision: 7),
                        PushedAt = c.DateTimeOffset(precision: 7),
                        UpdatedAt = c.DateTimeOffset(precision: 7),
                        DbUpdated = c.DateTimeOffset(nullable: false, precision: 7),
                        ReadmeHtml = c.String(),
                        Contributors = c.Int(nullable: false),
                        Commits = c.Int(nullable: false),
                        DbDetailsUpdated = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => new { t.Owner, t.Name });
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Repositories");
        }
    }
}
