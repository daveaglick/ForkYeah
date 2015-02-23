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
                        OwnerHtmlUrl = c.String(nullable: false),
                        Description = c.String(nullable: false),
                        HtmlUrl = c.String(nullable: false),
                        StargazersCount = c.Int(nullable: false),
                        StargazersIncrease = c.Int(nullable: false),
                        Added = c.DateTime(nullable: false),
                        Updated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.Owner, t.Name });
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Repositories");
        }
    }
}
